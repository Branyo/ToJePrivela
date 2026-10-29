#!/bin/sh
# Puts the latest main online: backs up the database, pulls, rebuilds the images (with fresh base images) and restarts.
# The database migrates itself when the API starts; the backup is the way back. Run as root on the server.

# All in one function, read whole before it runs: git pull may rewrite this very file mid-run.
main() {
  set -e
  REPO_DIR=$(cd "$(dirname "$0")/.." && pwd)
  cd "$REPO_DIR"

  sh deploy/backup-db.sh
  git pull --ff-only
  docker compose build --pull
  docker compose up -d
  docker image prune -f
  docker compose ps
}

main "$@"
exit
