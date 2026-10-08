[English](README.md) | Português

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

# .NET 10 + Kubernetes: do Dockerfile aos probes de resiliência

Uma **API de pedidos** pequena e real (Minimal API do .NET 10) para percorrer todo o caminho até o Kubernetes: Dockerfile multi-stage com imagem chiseled e usuário não-root, três endpoints de saúde (`live`, `ready`, `startup`), graceful shutdown, manifests com Kustomize (Deployment, Service, ConfigMap, Secret, HPA, PodDisruptionBudget, NetworkPolicy, Ingress) e uma série de experimentos em um cluster local [kind](https://kind.sigs.k8s.io/).

> **Início rápido**

```bash
dotnet test Pedidos.slnx
docker build -t pedidos:1.0 .
docker run --rm -p 8080:8080 pedidos:1.0
```

## Requisitos

O Windows é o alvo principal; Linux e macOS usam as mesmas ferramentas (veja os links). Nada aqui exige conta em nuvem.

| Ferramenta | Instalação (Windows) | Verificação |
|---|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | `winget install Microsoft.DotNet.SDK.10` | `dotnet --version` |
| WSL 2 (backend do Docker) | `wsl --install` (admin, reiniciar) | `wsl --version` |
| [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) | `winget install Docker.DockerDesktop` | `docker version` |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) (inclui o Kustomize) | `winget install Kubernetes.kubectl` | `kubectl version --client` |
| [kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) | `winget install Kubernetes.kind` | `kind version` |

Ordem: o SDK do .NET é independente; WSL 2, depois Docker Desktop, depois kubectl e kind (os dois precisam do Docker rodando).

Saída esperada (versões usadas para validar este repositório):

```text
$ dotnet --version
10.0.401
$ docker version --format '{{.Server.Version}}'
29.8.1
$ kubectl version --client
Client Version: v1.36.1
Kustomize Version: v5.8.1
$ kind version
kind v0.33.0 go1.26.7 windows/amd64
```

Problemas comuns:

- **Virtualização ou WSL 2 desabilitados**: habilite a virtualização na BIOS/UEFI e rode `wsl --install`; reinicie.
- **Docker Desktop não inicia**: abra-o uma vez, aceite os termos, espere o status "Engine running" e repita `docker version`.
- **Porta ocupada** (30080 ou 5001): troque o `hostPort` em `k8s/kind-config.yaml` ou encerre o processo que a usa.
- **kubectl no contexto errado**: `kubectl config get-contexts` e depois `kubectl config use-context kind-artigo-k8s`.

Linux/macOS: use o gerenciador de pacotes ou as páginas oficiais de download linkadas acima no lugar do `winget`.

## Como rodar

```bash
# 1. testes (sem rede, sem cluster)
dotnet test Pedidos.slnx

# 2. imagem: ingênua vs otimizada (compare com `docker images`)
docker build -t pedidos:ingenua -f Dockerfile.ingenua .
docker build -t pedidos:1.0 .

# 3. registro local (opcional) e cluster kind
docker run -d -p 127.0.0.1:5001:5000 --name registro registry:2
docker tag pedidos:1.0 localhost:5001/pedidos:1.0
docker push localhost:5001/pedidos:1.0
kind create cluster --name artigo-k8s --config k8s/kind-config.yaml
kind load docker-image pedidos:1.0 --name artigo-k8s

# 4. deploy (overlay dev: 3 réplicas, NodePort 30080)
kubectl apply -k k8s/overlays/dev
kubectl -n pedidos-dev rollout status deploy/pedidos
curl localhost:30080/health/ready

# 5. limpeza
kind delete cluster --name artigo-k8s
docker rm -f registro
```

Alternativa ao Dockerfile: `dotnet publish src/Pedidos -t:PublishContainer -p:ContainerFamily=noble-chiseled`.

## Experimentos (`scripts/`)

| Script | O que prova |
|---|---|
| `exp-a-docker-stop.sh` | `docker stop` com requisição em voo, com e sem tratamento de SIGTERM |
| `exp-c-readiness.sh` | dependência cai: readiness falha, o pod sai do Service, sem reiniciar |
| `exp-d-liveness.sh` | app travada: liveness falha e o contêiner reinicia |
| `exp-e-startup.sh` | arranque lento com e sem `startupProbe` |
| `exp-f-rolling.sh` | rolling update sob carga contínua, com e sem `preStop` |
| `exp-g-delete-pod.sh` | `kubectl delete pod` com requisição lenta em voo |
| `exp-h-eviction.sh` | PodDisruptionBudget barrando a segunda eviction |

## Estrutura

```text
src/Pedidos/            Minimal API, health checks, drenagem
tests/Pedidos.Tests/    xUnit + WebApplicationFactory (fakes)
tools/Carga/            cliente de carga contínua (conta erros)
k8s/base/               Deployment, Service, ConfigMap, Secret...
k8s/overlays/{dev,prod} overlays do Kustomize
k8s/experimentos/       patches usados nos experimentos
scripts/                scripts dos experimentos
Dockerfile              multi-stage, chiseled, não-root
Dockerfile.ingenua      a versão ingênua, para comparação
```

## Notas

- O Secret de `k8s/base/secret.yaml` tem um valor **falso**. Nunca versione segredos reais; em produção use um cofre.
- Os endpoints `/admin/*` só existem com `Admin__Habilitado=true` (overlay dev). Mantenha desligados em produção.
- Não executados aqui: escala do HPA (o kind não traz metrics-server), o `Ingress` (precisa de controller), a aplicação efetiva da NetworkPolicy e o push para um registro real como o ACR.

Licença: [MIT](LICENSE).
