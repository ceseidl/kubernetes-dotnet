#!/usr/bin/env bash
# Experimento (e): arranque lento (45 s) com e sem startupProbe.
# Uso: scripts/exp-e-startup.sh com|sem
NS=pedidos-dev
P=k8s/experimentos/sem-startup-probe.yaml
kubectl -n $NS apply -k k8s/overlays/dev >/dev/null
kubectl -n $NS set env deploy/pedidos \
  Aquecimento__Segundos=45 Aquecimento__Bloqueante=true >/dev/null
if [ "$1" = sem ]; then
  kubectl -n $NS patch deploy/pedidos --patch-file $P >/dev/null
fi
for i in $(seq 1 14); do
  sleep 5
  echo "t=$((i * 5))s"
  kubectl -n $NS get pods -l app=pedidos --no-headers \
    | awk '{print "  " $1, $2, $3, "restarts=" $4}'
done
kubectl -n $NS get events --sort-by=.lastTimestamp \
  | grep -E 'Unhealthy|Killing' | tail -4 | cut -c1-120
