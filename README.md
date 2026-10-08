English | [Português](README.pt-BR.md)

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

# .NET 10 + Kubernetes: from the Dockerfile to resilience probes

A small, real **orders API** (.NET 10 Minimal API) used to walk the whole path to Kubernetes: a multi-stage Dockerfile with a chiseled, non-root image, three health endpoints (`live`, `ready`, `startup`), graceful shutdown, manifests with Kustomize (Deployment, Service, ConfigMap, Secret, HPA, PodDisruptionBudget, NetworkPolicy, Ingress) and a set of experiments run on a local [kind](https://kind.sigs.k8s.io/) cluster.

> **Quick start**

```bash
dotnet test Pedidos.slnx
docker build -t pedidos:1.0 .
docker run --rm -p 8080:8080 pedidos:1.0
```

## Requirements

Windows is the main target; Linux and macOS use the same tools (see the links). Nothing here needs a cloud account.

| Tool | Install (Windows) | Check |
|---|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | `winget install Microsoft.DotNet.SDK.10` | `dotnet --version` |
| WSL 2 (Docker backend) | `wsl --install` (admin, reboot) | `wsl --version` |
| [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) | `winget install Docker.DockerDesktop` | `docker version` |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) (includes Kustomize) | `winget install Kubernetes.kubectl` | `kubectl version --client` |
| [kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) | `winget install Kubernetes.kind` | `kind version` |

Order: .NET SDK is independent; WSL 2, then Docker Desktop, then kubectl and kind (both need Docker running).

Expected output (the versions used to validate this repo):

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

Common problems:

- **Virtualization or WSL 2 disabled**: enable virtualization in the BIOS/UEFI and run `wsl --install`, then reboot.
- **Docker Desktop does not start**: open it once, accept the terms, wait for the "Engine running" status and retry `docker version`.
- **Port already in use** (30080 or 5001): change `hostPort` in `k8s/kind-config.yaml` or stop the process that holds it.
- **kubectl points to the wrong context**: `kubectl config get-contexts`, then `kubectl config use-context kind-artigo-k8s`.

Linux/macOS: use your package manager or the official download pages linked above instead of `winget`.

## Running

```bash
# 1. tests (no network, no cluster)
dotnet test Pedidos.slnx

# 2. image: naive vs optimized (compare with `docker images`)
docker build -t pedidos:ingenua -f Dockerfile.ingenua .
docker build -t pedidos:1.0 .

# 3. local registry (optional) and kind cluster
docker run -d -p 127.0.0.1:5001:5000 --name registro registry:2
docker tag pedidos:1.0 localhost:5001/pedidos:1.0
docker push localhost:5001/pedidos:1.0
kind create cluster --name artigo-k8s --config k8s/kind-config.yaml
kind load docker-image pedidos:1.0 --name artigo-k8s

# 4. deploy (dev overlay: 3 replicas, NodePort 30080)
kubectl apply -k k8s/overlays/dev
kubectl -n pedidos-dev rollout status deploy/pedidos
curl localhost:30080/health/ready

# 5. cleanup
kind delete cluster --name artigo-k8s
docker rm -f registro
```

Alternative to the Dockerfile: `dotnet publish src/Pedidos -t:PublishContainer -p:ContainerFamily=noble-chiseled`.

## Experiments (`scripts/`)

| Script | What it proves |
|---|---|
| `exp-a-docker-stop.sh` | `docker stop` with an in-flight request, with and without SIGTERM handling |
| `exp-c-readiness.sh` | dependency down: readiness fails, pod leaves the Service, no restart |
| `exp-d-liveness.sh` | frozen app: liveness fails, container restarts |
| `exp-e-startup.sh` | slow start with and without `startupProbe` |
| `exp-f-rolling.sh` | rolling update under continuous load, with and without `preStop` |
| `exp-g-delete-pod.sh` | `kubectl delete pod` with a slow request in flight |
| `exp-h-eviction.sh` | PodDisruptionBudget blocking the second eviction |

## Structure

```text
src/Pedidos/            Minimal API, health checks, drain
tests/Pedidos.Tests/    xUnit + WebApplicationFactory (fakes)
tools/Carga/            continuous load client (counts errors)
k8s/base/               Deployment, Service, ConfigMap, Secret...
k8s/overlays/{dev,prod} Kustomize overlays
k8s/experimentos/       patches used by the experiments
scripts/                experiment scripts
Dockerfile              multi-stage, chiseled, non-root
Dockerfile.ingenua      the naive version, for comparison
```

## Notes

- The Secret in `k8s/base/secret.yaml` holds a **fake** value. Never commit real secrets; use a vault in production.
- `/admin/*` endpoints exist only when `Admin__Habilitado=true` (dev overlay). Keep them off in production.
- Not executed here: HPA scaling (kind has no metrics-server by default), the `Ingress` (needs a controller), the NetworkPolicy enforcement and pushing to a real registry such as ACR.

License: [MIT](LICENSE).
