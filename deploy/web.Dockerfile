# The web image: Caddy serves the built Angular app and passes /api on to the API container (see Caddyfile).

FROM node:24-alpine AS build
WORKDIR /frontend

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM caddy:2-alpine
COPY deploy/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /frontend/dist/to-je-privela/browser /srv
