namespace Pedidos;

/// Simula um arranque lento (cache, migracoes, JIT).
public sealed class Aquecimento(
    EstadoApp estado,
    IConfiguration config,
    ILogger<Aquecimento> log) : BackgroundService
{
    public override async Task StartAsync(CancellationToken ct)
    {
        // Bloqueante: o Kestrel so abre a porta apos aquecer,
        // como uma app que migra o banco antes de escutar.
        if (config.GetValue<bool>("Aquecimento:Bloqueante"))
            await AquecerAsync(ct);
        await base.StartAsync(ct);
    }

    protected override Task ExecuteAsync(CancellationToken ct) =>
        AquecerAsync(ct);

    async Task AquecerAsync(CancellationToken ct)
    {
        if (estado.CacheAquecido) return;
        var s = config.GetValue("Aquecimento:Segundos", 3);
        log.LogInformation("Aquecendo cache por {S}s", s);
        await Task.Delay(TimeSpan.FromSeconds(s), ct);
        estado.CacheAquecido = true;
        log.LogInformation("Cache aquecido");
    }
}
