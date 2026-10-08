#!/usr/bin/env bash
# Experimento (c): dependencia cai -> readiness falha ->
# o pod sai do Service, sem reiniciar.
NS=pedidos-dev
POD=$(kubectl -n $NS get pod -l app=pedidos \
  -o jsonpath='{.items[0].metadata.name}')
kubectl -n $NS port-forward "pod/$POD" 18080:8080 >/dev/null &
PF=$!; sleep 2
echo "== antes"; kubectl -n $NS get endpoints pedidos
curl -s -XPOST localhost:18080/admin/dependencia/false; echo
sleep 8
echo "== depois (pod alvo: $POD)"
kubectl -n $NS get pods -l app=pedidos
kubectl -n $NS get endpoints pedidos
curl -s localhost:18080/health/ready; echo
curl -s -o /dev/null -w 'live=%{http_code}\n' \
  localhost:18080/health/live
kubectl -n $NS get events --field-selector reason=Unhealthy \
  | tail -3
curl -s -XPOST localhost:18080/admin/dependencia/true; echo
sleep 6
echo "== recuperado"; kubectl -n $NS get endpoints pedidos
kill $PF
