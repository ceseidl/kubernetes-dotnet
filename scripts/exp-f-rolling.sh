#!/usr/bin/env bash
# Experimento (f): rolling update sob carga continua.
# Uso: scripts/exp-f-rolling.sh com|sem  (preStop + drenagem)
NS=pedidos-dev
kubectl -n $NS apply -k k8s/overlays/dev >/dev/null
kubectl -n $NS set env deploy/pedidos \
  Shutdown__DrenagemSegundos- >/dev/null
kubectl -n $NS set env deploy/pedidos \
  Aquecimento__Segundos=3 Aquecimento__Bloqueante=false >/dev/null
if [ "$1" = sem ]; then
  kubectl -n $NS patch deploy/pedidos \
    --patch-file k8s/experimentos/sem-prestop.yaml >/dev/null
fi
kubectl -n $NS rollout status deploy/pedidos >/dev/null

dotnet run -c Release --project tools/Carga -- \
  http://localhost:30080/info 45 &
sleep 8
echo "== rollout restart (t=8s)"
kubectl -n $NS rollout restart deploy/pedidos >/dev/null
kubectl -n $NS rollout status deploy/pedidos | tail -1
wait
