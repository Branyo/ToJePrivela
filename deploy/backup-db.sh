#!/bin/sh
# Copies the game's database into the data volume's backups folder and keeps the last 14 days of copies.
# Safe while the game runs: SQLite's .backup makes a consistent copy. Run as root on the server; cron runs it nightly.
set -e

REPO_DIR=$(cd "$(dirname "$0")/.." && pwd)
# Docker Compose names the volume after the project, which is the repository folder unless COMPOSE_PROJECT_NAME says
# otherwise.
PROJECT=${COMPOSE_PROJECT_NAME:-$(basename "$REPO_DIR" | tr '[:upper:]' '[:lower:]')}
VOL=$(docker volume inspect "${PROJECT}_data" --format '{{ .Mountpoint }}')
# The API image runs as this user; files it cannot write would stop the game.
APP_UID=1654

BACKUP="$VOL/backups/ToJePrivela-$(date +%F-%H%M).db"

mkdir -p "$VOL/backups"
chown "$APP_UID:$APP_UID" "$VOL/backups"

# As the app's user, so the -wal/-shm files SQLite may create stay writable for the app.
setpriv --reuid="$APP_UID" --regid="$APP_UID" --clear-groups \
  sqlite3 "$VOL/ToJePrivela.db" ".backup '$BACKUP'"

find "$VOL/backups" -name 'ToJePrivela-*.db' -mtime +14 -delete
echo "backup done: $BACKUP"
