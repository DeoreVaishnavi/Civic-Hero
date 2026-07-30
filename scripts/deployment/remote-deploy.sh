#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-deployment/.env.production.local}"
COMPOSE_FILE="deployment/docker-compose.prod.yml"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing production environment file: $ENV_FILE" >&2
  exit 1
fi

: "${BACKEND_IMAGE:?BACKEND_IMAGE must be supplied by the release pipeline}"
: "${FRONTEND_IMAGE:?FRONTEND_IMAGE must be supplied by the release pipeline}"

export BACKEND_IMAGE FRONTEND_IMAGE
previous_file="deployment/.previous-release"
current_backend="$(grep '^BACKEND_IMAGE=' "$ENV_FILE" | cut -d= -f2- || true)"
current_frontend="$(grep '^FRONTEND_IMAGE=' "$ENV_FILE" | cut -d= -f2- || true)"
printf 'BACKEND_IMAGE=%s\nFRONTEND_IMAGE=%s\n' "$current_backend" "$current_frontend" > "$previous_file"

sed -i.bak "s|^BACKEND_IMAGE=.*|BACKEND_IMAGE=$BACKEND_IMAGE|" "$ENV_FILE"
sed -i.bak "s|^FRONTEND_IMAGE=.*|FRONTEND_IMAGE=$FRONTEND_IMAGE|" "$ENV_FILE"
rm -f "$ENV_FILE.bak"

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans

for attempt in {1..24}; do
  if curl --fail --silent --show-error --max-time 10 "https://$(grep '^APP_HOST=' "$ENV_FILE" | cut -d= -f2-)/health/ready" >/dev/null; then
    echo "CivicHero deployment is ready."
    docker image prune -f >/dev/null
    exit 0
  fi
  sleep 5
done

echo "Deployment health verification failed. Run scripts/deployment/rollback.sh." >&2
exit 1
