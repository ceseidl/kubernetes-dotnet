namespace Pedidos;

/// Ao receber SIGTERM, avisa o balanceador (readiness 503)
/// e espera ANTES de o Kestrel parar de aceitar conexoes.
public sealed class Drenagem(
    EstadoApp estado,
    IConfiguration config,
    IHostApplicationLifetime vida,
    ILogger<Drenagem> log) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken ct)
    {
        vida.ApplicationStopping.Register(() =>
            log.LogInformation("1. Stopping (SIGTERM)"));
        vida.ApplicationStopped.Register(() =>
            log.LogInformation("4. Stopped"));
        return Task.CompletedTask;
    }

    // Roda antes do StopAsync de TODOS os servicos (inclusive
    // o Kestrel), por isso a drenagem fica aqui.
    public async Task StoppingAsync(CancellationToken ct)
    {
        var s = config.GetValue("Shutdown:DrenagemSegundos", 3);
        estado.Drenando = true;
        log.LogInformation("2. Drenando (readiness 503) {S}s", s);
        await Task.Delay(TimeSpan.FromSeconds(s), ct);
        log.LogInformation("3. Fim da drenagem, fechando");
    }

    static Task Nada => Task.CompletedTask;

    public Task StartAsync(CancellationToken ct) => Nada;
    public Task StartedAsync(CancellationToken ct) => Nada;
    public Task StopAsync(CancellationToken ct) => Nada;
    public Task StoppedAsync(CancellationToken ct) => Nada;
}
