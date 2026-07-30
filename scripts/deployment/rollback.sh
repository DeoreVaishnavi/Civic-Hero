#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-deployment/.env.production.local}"
COMPOSE_FILE="deployment/docker-compose.prod.yml"
PREVIOUS_FILE="deployment/.previous-release"

if [[ ! -f "$PREVIOUS_FILE" ]]; then
  echo "No previous release record exists: $PREVIOUS_FILE" >&2
  exit 1
fi

# shellcheck disable=SC1090
source "$PREVIOUS_FILE"
: "${BACKEND_IMAGE:?Previous backend image is missing}"
: "${FRONTEND_IMAGE:?Previous frontend image is missing}"

sed -i.bak "s|^BACKEND_IMAGE=.*|BACKEND_IMAGE=$BACKEND_IMAGE|" "$ENV_FILE"
sed -i.bak "s|^FRONTEND_IMAGE=.*|FRONTEND_IMAGE=$FRONTEND_IMAGE|" "$ENV_FILE"
rm -f "$ENV_FILE.bak"

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans

echo "Rollback started using:"
echo "  $BACKEND_IMAGE"
echo "  $FRONTEND_IMAGE"
