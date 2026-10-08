#!/usr/bin/env bash
# Experimento (g): kubectl delete pod com requisicao lenta em voo.
NS=pedidos-dev
kubectl -n $NS apply -k k8s/overlays/dev >/dev/null
kubectl -n $NS set env deploy/pedidos \
  Shutdown__DrenagemSegundos- >/dev/null
kubectl -n $NS rollout status deploy/pedidos >/dev/null
curl -s -m 60 -w ' http=%{http_code} em %{time_total}s\n' \
  'localhost:30080/lento?segundos=25' &
sleep 3
for P in $(kubectl -n $NS get pods -l app=pedidos -o name); do
  if kubectl -n $NS logs "$P" | grep -q 'GET /lento'; then
    ALVO=${P#pod/}
  fi
done
echo "pod com a requisicao em voo: $ALVO"
kubectl -n $NS logs -f "$ALVO" > /tmp/pod-g.log 2>&1 &
sleep 1
INI=$(date +%s)
kubectl -n $NS delete pod "$ALVO" --wait=false
wait %1
echo "requisicao terminou $(( $(date +%s) - INI )) s apos delete"
sleep 4
grep -E 'Stopping|Drenando|Fim da|Stopped' /tmp/pod-g.log \
  | sed -E 's/.*mp":"([^"]*)".*ge":"([^"]*)".*/\1 \2/'
kubectl -n $NS get pods -l app=pedidos
