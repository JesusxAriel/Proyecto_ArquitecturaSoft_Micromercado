#!/bin/bash
set -eu

: "${JWT_KEY:?Falta JWT_KEY}"

umask 077
carpeta="${HOME}/puntomarket-demo-secrets"
mkdir -p "${carpeta}"
printf '%s' "${JWT_KEY}" > "${carpeta}/jwt-demo-key.txt"
unset JWT_KEY

socat TCP-LISTEN:5002,bind=127.0.0.1,fork,reuseaddr "TCP:${VENTAS_DESTINO:-ventas:8080}" &
socat TCP-LISTEN:5012,bind=127.0.0.1,fork,reuseaddr "TCP:${REPORTES_DESTINO:-reportes:8080}" &

exec dotnet PuntoMarket.Ventas.Frontend.dll
