#!/usr/bin/env bash
# Experimento (h): PodDisruptionBudget (minAvailable: 2) barra a
# segunda eviction seguida. kubectl drain equivale a evictions.
export MSYS_NO_PATHCONV=1   # Git Bash nao reescreve /api/...
NS=pedidos-dev
URL=/api/v1/namespaces/$NS/pods
kubectl -n $NS apply -k k8s/overlays/dev >/dev/null
kubectl -n $NS rollout status deploy/pedidos >/dev/null
kubectl -n $NS get pdb pedidos
ATIVOS=$(kubectl -n $NS get pods -l app=pedidos --no-headers \
  | grep -v Terminating | awk '{print $1}' | head -2)
for N in $ATIVOS; do
  BODY='{"apiVersion":"policy/v1","kind":"Eviction",'
  BODY="$BODY\"metadata\":{\"name\":\"$N\"}}"
  echo "== evict $N"
  echo "$BODY" | kubectl create --raw "$URL/$N/eviction" -f - \
    2>&1 | cut -c1-110
  kubectl -n $NS get pdb pedidos --no-headers
done
