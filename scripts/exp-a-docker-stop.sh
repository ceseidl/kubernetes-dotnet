#!/usr/bin/env bash
# Experimento (a): docker stop com uma requisicao em voo.
# Uso: scripts/exp-a-docker-stop.sh <imagem> <timeout-do-stop>
IMG=${1:-pedidos:1.0}; T=${2:-30}
SED='s/.*mp":"([^"]*)".*ge":"([^"]*)".*/\1 \2/'
docker rm -f pedidos-a >/dev/null 2>&1
docker run -d --name pedidos-a -p 8081:8080 "$IMG" >/dev/null
until curl -sf localhost:8081/health/startup >/dev/null; do
  sleep 1
done
curl -s -m 60 -w ' http=%{http_code} em %{time_total}s\n' \
  'localhost:8081/lento?segundos=20' &
sleep 2
INI=$(date +%s%N)
docker stop -t "$T" pedidos-a >/dev/null
FIM=$(date +%s%N)
wait
echo "docker stop levou $(( (FIM - INI) / 1000000 )) ms"
CODE=$(docker inspect -f '{{.State.ExitCode}}' pedidos-a)
echo "exit code: $CODE"
docker logs pedidos-a 2>&1 \
  | grep -E 'Stopping|Drenando|Fim da|Stopped|lento .* 200' \
  | sed -E "$SED" | cut -c1-60
docker rm -f pedidos-a >/dev/null
