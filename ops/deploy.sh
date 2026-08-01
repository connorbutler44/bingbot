#!/usr/bin/env bash

set -euo pipefail

# load env
set -a
source .env
set +a

SERVER="${DEPLOY_USER}@${DEPLOY_HOST}"

echo "Deploying to ${DEPLOY_HOST}..."

ssh "$SERVER" << EOF

set -e

cd "$DEPLOY_APP_DIR"

git pull

docker build -f Dockerfile.runtime -t bingbot/media-runtime-base .

docker compose up --build -d

docker image prune -f
EOF

echo "Deployment complete!"