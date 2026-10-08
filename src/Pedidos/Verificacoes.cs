using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Pedidos;

using Ctx = HealthCheckContext;
using Res = HealthCheckResult;

/// Liveness: so olha para o proprio processo. Nada de banco.
public sealed class VivoCheck(EstadoApp estado) : IHealthCheck
{
    public async Task<Res> CheckHealthAsync(
        Ctx ctx, CancellationToken ct = default)
    {
        // Simula um deadlock: o processo vive, mas nao responde.
        if (estado.Travado)
            await Task.Delay(Timeout.Infinite, ct);
        return Res.Healthy("processo responde");
    }
}

/// Readiness: pronto para receber trafego agora?
public sealed class ProntoCheck(
    EstadoApp estado, IDependencia dependencia) : IHealthCheck
{
    public async Task<Res> CheckHealthAsync(
        Ctx ctx, CancellationToken ct = default)
    {
        if (estado.Drenando)
            return Res.Unhealthy("drenando (SIGTERM recebido)");

        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            return await dependencia.PingAsync(cts.Token)
                ? Res.Healthy("dependencia ok")
                : Res.Unhealthy("dependencia fora do ar");
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested)
        {
            return Res.Unhealthy("dependencia lenta (> 2 s)");
        }
    }
}

/// Startup: o cache de arranque ja foi aquecido?
public sealed class CacheCheck(EstadoApp estado) : IHealthCheck
{
    public Task<Res> CheckHealthAsync(
        Ctx ctx, CancellationToken ct = default) =>
        Task.FromResult(estado.CacheAquecido
            ? Res.Healthy("cache aquecido")
            : Res.Unhealthy("cache aquecendo"));
}
