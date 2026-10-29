#!/bin/sh
# Puts a backup from backup-db.sh back as the game's database: deploy/restore-db.sh ToJePrivela-2026-10-24-0330.db
# Without an argument it lists the backups. The database it replaces is kept as a backup first. Run as root on the server.
set -e

REPO_DIR=$(cd "$(dirname "$0")/.." && pwd)
PROJECT=${COMPOSE_PROJECT_NAME:-$(basename "$REPO_DIR" | tr '[:upper:]' '[:lower:]')}
VOL=$(docker volume inspect "${PROJECT}_data" --format '{{ .Mountpoint }}')
APP_UID=1654

if [ -z "$1" ]; then
  echo "usage: $0 <backup file>; the backups are:"
  ls -t "$VOL/backups"
  exit 1
fi

BACKUP="$VOL/backups/$(basename "$1")"
[ -f "$BACKUP" ] || { echo "no such backup: $BACKUP"; exit 1; }

cd "$REPO_DIR"
sh deploy/backup-db.sh
docker compose stop api
cp "$BACKUP" "$VOL/ToJePrivela.db"
# They belong to the replaced database; next to the restored one they would corrupt it.
rm -f "$VOL/ToJePrivela.db-wal" "$VOL/ToJePrivela.db-shm"
chown "$APP_UID:$APP_UID" "$VOL/ToJePrivela.db"
docker compose start api
echo "restored $BACKUP"
