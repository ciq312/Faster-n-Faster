#!/usr/bin/env bash
set -eu
cd "$(dirname "$0")"
root=$(pwd -P)
project="fnf-e2e-$(node -p "crypto.randomUUID()")"
compose=(docker compose --env-file "$root/.env.example" -p "$project"
    -f "$root/docker-compose.yml" -f "$root/docker-compose.e2e.yml")
export POSTGRES_USER=e2e POSTGRES_PASSWORD=local-e2e POSTGRES_DB=e2e

cleanup() {
    local status=$?
    if (( status != 0 )); then
        "${compose[@]}" logs --no-color >&2 || true
    fi
    "${compose[@]}" down --volumes --rmi local || exit 1
    exit "$status"
}

docker info > /dev/null
trap cleanup EXIT
echo "Starting $project"

"${compose[@]}" up --build --wait --wait-timeout 90 api

api_address=$("${compose[@]}" port api 8080)
export E2E_API_URL="http://$api_address"
export VITE_API_URL=""

hasher_project="$root/MicrosoftPasswordHash/PasswordHasher/PasswordHasher.csproj"
dotnet build "$hasher_project" --verbosity quiet

nick=Alice
login=e2e-alice
password=E2e-Racer-2026!
password_hash=$(dotnet run --no-build --project "$hasher_project" -- --nick "$nick" --login "$login" --password "$password")

user_id=$(
    "${compose[@]}" exec -T postgres \
        psql -qAt -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
        -v ON_ERROR_STOP=1 -v password_hash="$password_hash" <<'SQL'
INSERT INTO "Users" ("Id", "Email", "Nick", "Login", "Password", "IsEmailVerified")
VALUES (gen_random_uuid(), 'alice@example.test', 'Alice', 'e2e-alice', :'password_hash', true)
RETURNING "Id";
SQL
)

printf 'Seeded player ID: %s\n' "$user_id"

export E2E_LOGIN="$login"
export E2E_PASSWORD="$password"

cd fasternfasterapp

npx playwright test "$@"
