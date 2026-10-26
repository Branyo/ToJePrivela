# Deploying the game

The game runs as two containers from `docker-compose.yml`: **web** (Caddy: serves the Angular app, passes `/api` on to
the API, gets and renews the HTTPS certificate) and **api** (the .NET API; the SQLite database and logs live in the
`data` volume). Only web is reachable from outside.

```
browser ──► web (Caddy, 80/443) ──/api/*──► api (.NET, 8080) ──► /data/ToJePrivela.db
```

## Running it locally

```powershell
Copy-Item .env.example .env   # then fill in .env
docker compose up -d --build  # http://localhost (or the HTTP_PORT set in .env)
```

## Setting up a server

Tested on a DigitalOcean Droplet with Ubuntu 24.04 (1 GB RAM is enough to run the game, not to build it, hence the
swap). Allow inbound TCP 22, 80, 443 and UDP 443 with the cloud provider's firewall: Docker's published ports bypass
`ufw`.

```bash
apt update && apt upgrade -y
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
curl -fsSL https://get.docker.com | sh
apt install -y sqlite3

git clone https://github.com/Branyo/ToJePrivela.git /opt/tojeprivela
cd /opt/tojeprivela
cp .env.example .env && nano .env && chmod 600 .env   # a new JWT_SIGNING_KEY: openssl rand -base64 48
docker compose up -d --build

# Nightly database backup at 03:30 (server time).
echo '30 3 * * * root sh /opt/tojeprivela/deploy/backup-db.sh >> /var/log/tojeprivela-backup.log 2>&1' \
  > /etc/cron.d/tojeprivela-backup
```

For HTTPS, point a domain at the server (an `A` record, or a free name such as `<name>.duckdns.org`), set
`SITE_ADDRESS=<domain>` in `.env` and run `docker compose up -d`. Caddy logs `certificate obtained successfully`.

## Day to day (on the server, as root)

| Command | What it does |
|---|---|
| `sh deploy/update.sh` | backs up the database, then puts the latest `main` online |
| `sh deploy/backup-db.sh` | backs up the database now (into the volume's `backups/`, kept 14 days) |
| `sh deploy/restore-db.sh` | lists the backups; with a file name, puts that one back (the current database is backed up first) |
| `docker compose ps` / `docker compose logs -f api` | is it running / the API's log |

The database migrates itself when the API starts. Older code cannot read a database a newer version has migrated, so
going back to an older version means restoring a backup taken before the update as well.

Copy backups off the server now and then:
`scp "root@<server>:/var/lib/docker/volumes/tojeprivela_data/_data/backups/*.db" .`
