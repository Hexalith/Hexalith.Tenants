#!/usr/bin/env bash
#
# Hexalith.Tenants "Aha Moment" demo automation.
#
# Prerequisites:
#   dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
#   TOKEN=<quickstart-keycloak-token>
#
# Usage:
#   TOKEN=<redacted> ./scripts/demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235 --tenants-url https://localhost:7236
#   ./scripts/demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235 --token <redacted>
#   ./scripts/demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235 --hmac-dev-token

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
GRAY='\033[0;37m'
NC='\033[0m'

BASE_URL="${COMMANDAPI_URL:-}"
SAMPLE_URL="${SAMPLE_URL:-}"
TENANTS_URL="${TENANTS_URL:-}"
TOKEN="${TOKEN:-}"
USE_HMAC_DEV_TOKEN=false
TIMEOUT_SECONDS=30

show_help() {
    echo "Usage: $0 --base-url <eventstore-url> --sample-url <sample-url> [options]"
    echo ""
    echo "Required:"
    echo "  --base-url       EventStore command gateway base URL"
    echo "  --sample-url     Sample service base URL"
    echo ""
    echo "Options:"
    echo "  --tenants-url    Tenants query API base URL for current-state/audit evidence"
    echo "  --token          JWT token from the quickstart Keycloak flow"
    echo "  --hmac-dev-token Generate an HMAC token for the explicit EnableKeycloak=false fallback"
    echo "  --timeout        Projection wait timeout in seconds (default: 30)"
    echo "  -h, --help       Show this help message"
    echo ""
    echo "Environment alternatives: COMMANDAPI_URL, SAMPLE_URL, TENANTS_URL, TOKEN"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --base-url)
            BASE_URL="${2:-}"
            shift 2
            ;;
        --sample-url)
            SAMPLE_URL="${2:-}"
            shift 2
            ;;
        --tenants-url)
            TENANTS_URL="${2:-}"
            shift 2
            ;;
        --token)
            TOKEN="${2:-}"
            shift 2
            ;;
        --hmac-dev-token)
            USE_HMAC_DEV_TOKEN=true
            shift
            ;;
        --timeout)
            TIMEOUT_SECONDS="${2:-30}"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}ERROR: Unknown argument: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

if [[ -z "$BASE_URL" || -z "$SAMPLE_URL" ]]; then
    echo -e "${RED}ERROR: --base-url and --sample-url are required.${NC}"
    echo -e "${YELLOW}Find dynamic endpoints in the Aspire dashboard resources: eventstore and sample.${NC}"
    exit 1
fi

if [[ -z "$TOKEN" && "$USE_HMAC_DEV_TOKEN" != true ]]; then
    echo -e "${RED}ERROR: provide TOKEN/--token from Keycloak, or pass --hmac-dev-token only when EnableKeycloak=false.${NC}"
    exit 1
fi

for cmd in curl openssl; do
    if ! command -v "$cmd" >/dev/null 2>&1; then
        echo -e "${RED}ERROR: '$cmd' is required but was not found.${NC}"
        exit 1
    fi
done

HAS_JQ=false
if command -v jq >/dev/null 2>&1; then
    HAS_JQ=true
fi

BASE_URL="${BASE_URL%/}"
SAMPLE_URL="${SAMPLE_URL%/}"
TENANTS_URL="${TENANTS_URL%/}"
COMMAND_ENDPOINT="$BASE_URL/api/v1/commands"
STATUS_ENDPOINT="$BASE_URL/api/v1/commands/status"

json_field() {
    local json="$1"
    local field="$2"
    local fallback="${3:-unknown}"

    if [[ -z "$json" ]]; then
        echo "$fallback"
        return
    fi

    if $HAS_JQ; then
        echo "$json" | jq -r ".$field // \"$fallback\"" 2>/dev/null || echo "$fallback"
    else
        echo "$json" | sed -nE "s/.*\"$field\"[[:space:]]*:[[:space:]]*\"?([^\",}]+)\"?.*/\\1/p" | head -n 1
    fi
}

generate_ulid() {
    local alphabet="0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    local value
    value=$(($(date +%s%3N)))
    local time_part=""
    for _ in {1..10}; do
        local index=$((value % 32))
        time_part="${alphabet:index:1}$time_part"
        value=$((value / 32))
    done

    local random_hex
    random_hex=$(openssl rand -hex 16)
    local random_part=""
    for i in $(seq 1 16); do
        local byte_hex="${random_hex:$(((i - 1) * 2)):2}"
        local index=$((16#$byte_hex % 32))
        random_part="${random_part}${alphabet:index:1}"
    done

    echo "${time_part}${random_part}"
}

generate_hmac_token() {
    local header payload signature exp
    header=$(printf '{"alg":"HS256","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
    exp=$(($(date +%s) + 28800))
    payload=$(printf '{"sub":"admin-user","iss":"hexalith-dev","aud":"hexalith-eventstore","tenants":["system"],"exp":%s}' "$exp" \
        | openssl base64 -A | tr '+/' '-_' | tr -d '=')
    signature=$(printf '%s.%s' "$header" "$payload" \
        | openssl dgst -sha256 -hmac "DevOnlySigningKey-AtLeast32Chars!" -binary \
        | openssl base64 -A | tr '+/' '-_' | tr -d '=')
    printf '%s.%s.%s' "$header" "$payload" "$signature"
}

if [[ "$USE_HMAC_DEV_TOKEN" == true ]]; then
    TOKEN="$(generate_hmac_token)"
fi

TMPFILE=$(mktemp /tmp/hexalith_demo_XXXXXX.json)
trap 'rm -f "$TMPFILE"' EXIT

TIMESTAMP=$(date +%Y%m%d%H%M%S)
TENANT_ID="acme-demo-$TIMESTAMP"
USER_ID="jane-doe-$TIMESTAMP"

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Hexalith.Tenants - Aha Moment Demo${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "${GRAY}EventStore: $BASE_URL${NC}"
echo -e "${GRAY}Sample:     $SAMPLE_URL${NC}"
if [[ -n "$TENANTS_URL" ]]; then
    echo -e "${GRAY}Tenants:    $TENANTS_URL${NC}"
fi
echo -e "${GRAY}Tenant ID:  $TENANT_ID${NC}"
echo -e "${GRAY}User ID:    $USER_ID${NC}"
echo ""

check_health() {
    local name="$1"
    local url="$2"
    echo -e "${YELLOW}[Setup] Checking $name health...${NC}"
    if curl -sfk --max-time 5 "$url/health" >/dev/null 2>&1 || curl -sfk --max-time 5 "$url/alive" >/dev/null 2>&1; then
        echo -e "${GREEN}[Setup] $name is reachable.${NC}"
    else
        echo -e "${RED}ERROR: $name is not reachable at $url/health or $url/alive.${NC}"
        exit 1
    fi
}

check_health "EventStore" "$BASE_URL"
check_health "Sample" "$SAMPLE_URL"

COMMANDS_ACCEPTED=0
STATUS_SUMMARY=()

poll_status() {
    local correlation_id="$1"
    local label="$2"
    local deadline=$((SECONDS + TIMEOUT_SECONDS))
    local response status rejection

    while (( SECONDS <= deadline )); do
        response=$(curl -sk --max-time 10 \
            -H "Authorization: Bearer $TOKEN" \
            "$STATUS_ENDPOINT/$correlation_id" 2>/dev/null || true)
        status=$(json_field "$response" "status" "")

        case "$status" in
            Completed)
                echo -e "${GREEN}  Status: Completed ($label)${NC}"
                STATUS_SUMMARY+=("$label:Completed:$correlation_id")
                return 0
                ;;
            Rejected)
                rejection=$(json_field "$response" "rejectionEventType" "Rejected")
                echo -e "${YELLOW}  Status: Rejected ($label) - $rejection${NC}"
                STATUS_SUMMARY+=("$label:Rejected:$correlation_id")
                return 0
                ;;
            PublishFailed|TimedOut)
                echo -e "${RED}  Status: $status ($label)${NC}"
                STATUS_SUMMARY+=("$label:$status:$correlation_id")
                return 1
                ;;
        esac

        sleep 1
    done

    echo -e "${RED}  Timed out waiting for command status: $label ($correlation_id)${NC}"
    return 1
}

send_command() {
    local label="$1"
    local body="$2"
    local response http_code correlation_id

    echo ""
    echo -e "${CYAN}--- $label ---${NC}"
    http_code=$(curl -sk -o "$TMPFILE" -w "%{http_code}" \
        -X POST "$COMMAND_ENDPOINT" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$body" \
        --max-time 30)
    response=$(cat "$TMPFILE" 2>/dev/null || true)

    if [[ "$http_code" != "202" ]]; then
        echo -e "${RED}  HTTP $http_code from command gateway.${NC}"
        return 1
    fi

    COMMANDS_ACCEPTED=$((COMMANDS_ACCEPTED + 1))
    correlation_id=$(json_field "$response" "correlationId" "")
    if [[ -z "$correlation_id" ]]; then
        echo -e "${RED}  Command accepted but correlationId was not found in the response.${NC}"
        return 1
    fi

    echo -e "${GREEN}  202 Accepted - status: $STATUS_ENDPOINT/$correlation_id${NC}"
    poll_status "$correlation_id" "$label"
}

wait_for_access() {
    local expected="$1"
    local label="$2"
    local deadline=$((SECONDS + TIMEOUT_SECONDS))
    local response access role reason

    echo ""
    echo -e "${CYAN}--- $label ---${NC}"
    echo -e "${GRAY}GET $SAMPLE_URL/access/$TENANT_ID/$USER_ID${NC}"

    while (( SECONDS <= deadline )); do
        response=$(curl -sk --max-time 10 "$SAMPLE_URL/access/$TENANT_ID/$USER_ID" 2>/dev/null || true)
        access=$(json_field "$response" "access" "")

        if [[ "$access" == "$expected" ]]; then
            role=$(json_field "$response" "role" "")
            reason=$(json_field "$response" "reason" "")
            if [[ -n "$role" && "$role" != "unknown" ]]; then
                echo -e "${GREEN}  Access: $access | Role: $role${NC}"
            else
                echo -e "${MAGENTA}  Access: $access | Reason: $reason${NC}"
            fi
            return 0
        fi

        sleep 1
    done

    echo -e "${RED}  Timed out waiting for access '$expected'. Last response access='$access'.${NC}"
    return 1
}

bootstrap_message_id=$(generate_ulid)
create_message_id=$(generate_ulid)
add_message_id=$(generate_ulid)
remove_message_id=$(generate_ulid)

send_command "Bootstrap Global Admin" \
    "{\"messageId\":\"$bootstrap_message_id\",\"tenant\":\"system\",\"domain\":\"global-administrators\",\"aggregateId\":\"global-administrators\",\"commandType\":\"BootstrapGlobalAdmin\",\"payload\":{\"UserId\":\"admin-user\"}}"

send_command "Create Tenant" \
    "{\"messageId\":\"$create_message_id\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"CreateTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"Name\":\"Acme Demo Corp\",\"Description\":\"Demo tenant for aha moment\"}}"

send_command "Add User" \
    "{\"messageId\":\"$add_message_id\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"AddUserToTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"UserId\":\"$USER_ID\",\"Role\":\"TenantContributor\"}}"

wait_for_access "granted" "Verify Access Granted"

send_command "Remove User" \
    "{\"messageId\":\"$remove_message_id\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"RemoveUserFromTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"UserId\":\"$USER_ID\"}}"

wait_for_access "denied" "Verify Access Denied"

QUERY_EVIDENCE="not requested"
if [[ -n "$TENANTS_URL" ]]; then
    tenant_http=$(curl -sk -o /dev/null -w "%{http_code}" \
        -H "Authorization: Bearer $TOKEN" \
        "$TENANTS_URL/api/tenants/$TENANT_ID" 2>/dev/null || true)
    audit_http=$(curl -sk -o /dev/null -w "%{http_code}" \
        -H "Authorization: Bearer $TOKEN" \
        "$TENANTS_URL/api/tenants/$TENANT_ID/audit" 2>/dev/null || true)
    QUERY_EVIDENCE="tenant=$tenant_http audit=$audit_http"
fi

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Demo Complete${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "  Commands accepted:    $COMMANDS_ACCEPTED"
for item in "${STATUS_SUMMARY[@]}"; do
    IFS=":" read -r label status correlation <<< "$item"
    echo -e "  Command status:       $label = $status ($STATUS_ENDPOINT/$correlation)"
done
echo -e "${GREEN}  Access transition:    granted -> denied (verified)${NC}"
echo -e "  Query evidence:       $QUERY_EVIDENCE"
echo ""
echo -e "${YELLOW}  The sample subscribing service revoked local access via tenants.events.${NC}"
echo -e "${YELLOW}  No Tenants/EventStore polling is used by the access endpoint.${NC}"
