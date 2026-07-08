#!/usr/bin/env bash
set -euo pipefail

has_value() {
  local value="${1:-}"
  value="$(printf '%s' "$value" | tr -d '[:space:]')"
  [ -n "$value" ]
}

fail() {
  echo "[release-secrets] $1" >&2
  exit 1
}

has_value "${NUGET_API_KEY:-}" || fail "NUGET_API_KEY is required before publishing NuGet packages."

if [ -x ./.hexalith/release/publish-containers.sh ] || has_value "${HEXALITH_CONTAINER_PROJECTS:-}"; then
  has_value "${HEXALITH_ZOT_USERNAME:-}" || fail "HEXALITH_ZOT_USERNAME is required before publishing containers."
  has_value "${HEXALITH_ZOT_API_KEY:-}" || fail "HEXALITH_ZOT_API_KEY is required before publishing containers."
fi
