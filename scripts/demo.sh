#!/usr/bin/env bash
#
# Hexalith.Tenants "Aha Moment" Demo — automated reactive access revocation demo.
#
# Prerequisites: The AppHost must be running before executing this script.
# Start it with: dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
#
# Usage:
#   ./scripts/demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235
#
# Environment variables (alternative to flags):
#   COMMANDAPI_URL=https://localhost:7234  SAMPLE_URL=https://localhost:7235  ./scripts/demo.sh

set -euo pipefail

# --- Colors ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

BASE_URL="${COMMANDAPI_URL:-}"
SAMPLE_URL="${SAMPLE_URL:-}"

# --- Parse arguments ---
while [[ $# -gt 0 ]]; do
    case $1 in
        --base-url)
            if [[ $# -lt 2 || "$2" == --* ]]; then
                echo -e "${RED}ERROR: --base-url requires a value.${NC}"
                exit 1
            fi
            BASE_URL="$2"
            shift 2
            ;;
        --sample-url)
            if [[ $# -lt 2 || "$2" == --* ]]; then
                echo -e "${RED}ERROR: --sample-url requires a value.${NC}"
                exit 1
            fi
            SAMPLE_URL="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 --base-url <URL> --sample-url <URL>"
            echo ""
            echo "Options:"
            echo "  --base-url    CommandApi base URL (e.g., https://localhost:7234)"
            echo "  --sample-url  Sample service base URL (e.g., https://localhost:7235)"
            echo "  -h, --help    Show this help message"
            echo ""
            echo "Environment variables (alternative to flags):"
            echo "  COMMANDAPI_URL  CommandApi base URL"
            echo "  SAMPLE_URL      Sample service base URL"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown argument: $1${NC}"
            exit 1
            ;;
    esac
done

# --- Validate required parameters ---
if [[ -z "$BASE_URL" || -z "$SAMPLE_URL" ]]; then
    echo -e "${RED}ERROR: --base-url and --sample-url are required.${NC}"
    echo -e "${YELLOW}Find your service URLs in the Aspire dashboard (typically https://localhost:17225).${NC}"
    echo ""
    echo -e "${YELLOW}Example:${NC}"
    echo -e "${YELLOW}  ./scripts/demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235${NC}"
    echo ""
    echo -e "${YELLOW}Or set environment variables:${NC}"
    echo -e "${YELLOW}  export COMMANDAPI_URL=https://localhost:7234${NC}"
    echo -e "${YELLOW}  export SAMPLE_URL=https://localhost:7235${NC}"
    exit 1
fi

# --- Validate required tools ---
for cmd in curl openssl; do
    if ! command -v "$cmd" &> /dev/null; then
        echo -e "${RED}ERROR: '$cmd' is required but not found. Please install it and retry.${NC}"
        exit 1
    fi
done

BASE_URL="${BASE_URL%/}"
SAMPLE_URL="${SAMPLE_URL%/}"
COMMAND_ENDPOINT="$BASE_URL/api/v1/commands"

# --- Create temp file for curl responses ---
TMPFILE=$(mktemp /tmp/hexalith_demo_XXXXXX.json)
trap 'rm -f "$TMPFILE"' EXIT

# --- Generate unique IDs to avoid conflicts on re-run ---
TIMESTAMP=$(date +%Y%m%d%H%M%S)
TENANT_ID="acme-demo-$TIMESTAMP"
USER_ID="jane-doe-$TIMESTAMP"

# --- Generate JWT token ---
echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Hexalith.Tenants - Aha Moment Demo${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "${GRAY}CommandApi: $BASE_URL${NC}"
echo -e "${GRAY}Sample:     $SAMPLE_URL${NC}"
echo -e "${GRAY}Tenant ID:  $TENANT_ID${NC}"
echo -e "${GRAY}User ID:    $USER_ID${NC}"
echo ""

# Dev signing key — must match the key configured in docs/quickstart.md and AppHost dev settings.
# If you get 401 Unauthorized, verify the key has not changed in docs/quickstart.md.
echo -e "${YELLOW}[Setup] Generating JWT token...${NC}"
HEADER=$(echo -n '{"alg":"HS256","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
EXP=$(($(date +%s) + 28800))
PAYLOAD=$(echo -n "{\"sub\":\"admin-user\",\"iss\":\"hexalith-dev\",\"aud\":\"hexalith-tenants\",\"tenants\":[\"system\"],\"exp\":$EXP}" | openssl base64 -A | tr '+/' '-_' | tr -d '=')
SIG=$(echo -n "$HEADER.$PAYLOAD" | openssl dgst -sha256 -hmac "this-is-a-development-signing-key-minimum-32-chars" -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
TOKEN="$HEADER.$PAYLOAD.$SIG"
echo -e "${GREEN}[Setup] JWT token generated.${NC}"
echo ""

# --- Prerequisite check ---
echo -e "${YELLOW}[Setup] Checking CommandApi is reachable...${NC}"
if curl -sfk --max-time 5 "$BASE_URL/health" > /dev/null 2>&1; then
    echo -e "${GREEN}[Setup] CommandApi is healthy.${NC}"
else
    echo -e "${RED}ERROR: CommandApi is not reachable at $BASE_URL/health${NC}"
    echo -e "${YELLOW}Ensure the AppHost is running:${NC}"
    echo -e "${YELLOW}  dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj${NC}"
    exit 1
fi

echo -e "${YELLOW}[Setup] Checking Sample service is reachable...${NC}"
if curl -sfk --max-time 5 "$SAMPLE_URL/health" > /dev/null 2>&1; then
    echo -e "${GREEN}[Setup] Sample service is healthy.${NC}"
else
    echo -e "${RED}ERROR: Sample service is not reachable at $SAMPLE_URL/health${NC}"
    exit 1
fi

COMMANDS_SENT=0
ACCESS_VERIFIED=false

# --- Helper: check for jq ---
HAS_JQ=false
if command -v jq &> /dev/null; then
    HAS_JQ=true
fi

format_json() {
    if $HAS_JQ; then
        echo "$1" | jq . 2>/dev/null || echo "$1"
    else
        echo "$1"
    fi
}

safe_jq() {
    local json="$1"
    local query="$2"
    local fallback="${3:-unknown}"
    if $HAS_JQ && [[ -n "$json" ]]; then
        echo "$json" | jq -r "$query" 2>/dev/null || echo "$fallback"
    else
        echo "$fallback"
    fi
}

send_command() {
    local step_name="$1"
    local body="$2"

    echo ""
    echo -e "${CYAN}--- $step_name ---${NC}"
    echo -e "${GRAY}POST $COMMAND_ENDPOINT${NC}"

    HTTP_CODE=$(curl -sk -o "$TMPFILE" -w "%{http_code}" \
        -X POST "$COMMAND_ENDPOINT" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$body" \
        --max-time 30)

    RESPONSE=$(cat "$TMPFILE" 2>/dev/null || echo "")

    if [[ "$HTTP_CODE" == "202" ]]; then
        COMMANDS_SENT=$((COMMANDS_SENT + 1))
        CORR_ID=$(safe_jq "$RESPONSE" '.correlationId // "unknown"')
        echo -e "${GREEN}  202 Accepted — correlationId: $CORR_ID${NC}"
    elif [[ "$HTTP_CODE" == "422" ]]; then
        echo -e "${YELLOW}  $HTTP_CODE — Command rejected (business rule). This may be expected on re-runs.${NC}"
    else
        echo -e "${RED}  $HTTP_CODE — Error${NC}"
        if [[ -n "$RESPONSE" ]]; then
            format_json "$RESPONSE"
        fi
    fi
}

# --- Step 1: Bootstrap Global Admin ---
send_command "Step 1: Bootstrap Global Admin" \
    "{\"messageId\":\"demo-$TIMESTAMP-01-bootstrap\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"global-administrators\",\"commandType\":\"BootstrapGlobalAdmin\",\"payload\":{\"UserId\":\"admin-user\"}}"

sleep 2

# --- Step 2: Create a Tenant ---
send_command "Step 2: Create Tenant '$TENANT_ID'" \
    "{\"messageId\":\"demo-$TIMESTAMP-02-create-tenant\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"CreateTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"Name\":\"Acme Demo Corp\",\"Description\":\"Demo tenant for aha moment\"}}"

sleep 2

# --- Step 3: Add a User with TenantContributor Role ---
send_command "Step 3: Add User '$USER_ID' with TenantContributor Role" \
    "{\"messageId\":\"demo-$TIMESTAMP-03-add-user\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"AddUserToTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"UserId\":\"$USER_ID\",\"Role\":1}}"

sleep 2

# --- Step 4: Verify Access Granted ---
echo ""
echo -e "${CYAN}--- Step 4: Verify Access Granted ---${NC}"
echo -e "${GRAY}GET $SAMPLE_URL/access/$TENANT_ID/$USER_ID${NC}"

ACCESS_RESPONSE=$(curl -sk --max-time 10 "$SAMPLE_URL/access/$TENANT_ID/$USER_ID" 2>/dev/null || echo "")
if [[ -z "$ACCESS_RESPONSE" ]]; then
    echo -e "${YELLOW}  Event may not have propagated yet. Retrying in 3 seconds...${NC}"
    sleep 3
    ACCESS_RESPONSE=$(curl -sk --max-time 10 "$SAMPLE_URL/access/$TENANT_ID/$USER_ID" 2>/dev/null || echo "")
fi

ACCESS=$(safe_jq "$ACCESS_RESPONSE" '.access // "unknown"')
ROLE=$(safe_jq "$ACCESS_RESPONSE" '.role // "unknown"')
echo -e "${GREEN}  Access: $ACCESS | Role: $ROLE${NC}"

sleep 2

# --- Step 5: Remove the User — THE AHA MOMENT ---
send_command "Step 5: Remove User '$USER_ID' — THE AHA MOMENT" \
    "{\"messageId\":\"demo-$TIMESTAMP-05-remove-user\",\"tenant\":\"system\",\"domain\":\"tenants\",\"aggregateId\":\"$TENANT_ID\",\"commandType\":\"RemoveUserFromTenant\",\"payload\":{\"TenantId\":\"$TENANT_ID\",\"UserId\":\"$USER_ID\"}}"

sleep 2

# --- Step 6: Verify Access Denied ---
echo ""
echo -e "${CYAN}--- Step 6: Verify Access DENIED ---${NC}"
echo -e "${GRAY}GET $SAMPLE_URL/access/$TENANT_ID/$USER_ID${NC}"

ACCESS_RESPONSE=$(curl -sk --max-time 10 "$SAMPLE_URL/access/$TENANT_ID/$USER_ID" 2>/dev/null || echo "")
if [[ -z "$ACCESS_RESPONSE" ]]; then
    echo -e "${YELLOW}  Event may not have propagated yet. Retrying in 3 seconds...${NC}"
    sleep 3
    ACCESS_RESPONSE=$(curl -sk --max-time 10 "$SAMPLE_URL/access/$TENANT_ID/$USER_ID" 2>/dev/null || echo "")
fi

ACCESS=$(safe_jq "$ACCESS_RESPONSE" '.access // "unknown"')
REASON=$(safe_jq "$ACCESS_RESPONSE" '.reason // "unknown"')
echo -e "${MAGENTA}  Access: $ACCESS | Reason: $REASON${NC}"
if [[ "$ACCESS" == "denied" ]]; then ACCESS_VERIFIED=true; fi

# --- Summary ---
echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Demo Complete${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "  Commands sent:        $COMMANDS_SENT"
if $ACCESS_VERIFIED; then
    echo -e "${GREEN}  Access transitions:   granted -> denied (VERIFIED)${NC}"
else
    echo -e "${YELLOW}  Access transitions:   UNVERIFIED — check logs${NC}"
fi
echo -e "${GREEN}  Demo cycle:           COMPLETED${NC}"
echo ""
echo -e "${YELLOW}  The consuming service automatically revoked access${NC}"
echo -e "${YELLOW}  via DAPR pub/sub — no custom integration needed.${NC}"
echo ""
