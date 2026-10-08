#!/usr/bin/env bash
# Experimento (d): a aplicacao trava -> liveness falha 3x ->
# o kubelet reinicia o container (restartCount sobe).
NS=pedidos-dev
POD=$(kubectl -n $NS get pod -l app=pedidos \
  -o jsonpath='{.items[0].metadata.name}')
kubectl -n $NS port-forward "pod/$POD" 18080:8080 >/dev/null &
PF=$!; sleep 2
echo "== travando $POD"
curl -s -XPOST localhost:18080/admin/travar; echo
for i in $(seq 1 9); do
  sleep 4
  kubectl -n $NS get pod "$POD" --no-headers -o custom-columns=\
POD:.metadata.name,\
READY:.status.containerStatuses[0].ready,\
RESTARTS:.status.containerStatuses[0].restartCount
done
kill $PF
kubectl -n $NS describe pod "$POD" | grep -E \
  'Liveness|Last State|Reason|Exit Code|Restart Count'
kubectl -n $NS get events --sort-by=.lastTimestamp \
  | grep "$POD" | grep -E 'Unhealthy|Killing|Started' | tail -5
