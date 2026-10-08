using System.Collections.Concurrent;

namespace Pedidos;

public sealed record Pedido(
    Guid Id, string Cliente, decimal Total);
public sealed record NovoPedido(
    string Cliente, decimal Total);

public static class Endpoints
{
    public static void MapPedidos(this WebApplication app)
    {
        var db = new ConcurrentDictionary<Guid, Pedido>();

        app.MapGet("/pedidos", () => db.Values);
        app.MapGet("/pedidos/{id:guid}", (Guid id) =>
            db.TryGetValue(id, out var p)
                ? Results.Ok(p) : Results.NotFound());
        app.MapPost("/pedidos", (NovoPedido n) =>
        {
            var p = new Pedido(
                Guid.NewGuid(), n.Cliente, n.Total);
            db[p.Id] = p;
            return Results.Created($"/pedidos/{p.Id}", p);
        });

        // Requisicao longa: prova o shutdown graceful.
        app.MapGet("/lento", async (int segundos) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(segundos));
            return new
            {
                concluido = true,
                pod = Environment.MachineName
            };
        });
    }

    public static void MapInfo(this WebApplication app) =>
        app.MapGet("/info", () => new
        {
            pod = Environment.MachineName,
            cpus = Environment.ProcessorCount,
            memoriaMb = GC.GetGCMemoryInfo()
                .TotalAvailableMemoryBytes >> 20,
            gcServidor = System.Runtime.GCSettings.IsServerGC,
            threadsMinimas = MinThreads()
        });

    static int MinThreads()
    {
        ThreadPool.GetMinThreads(out var w, out _);
        return w;
    }

    public static void MapAdmin(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Admin:Habilitado"))
            return;
        var adm = app.MapGroup("/admin");
        var e = app.Services.GetRequiredService<EstadoApp>();

        adm.MapPost("/dependencia/{ligada:bool}", (bool ligada) =>
            e.DependenciaLigada = ligada);
        adm.MapPost("/latencia/{ms:int}", (int ms) =>
            e.LatenciaDependenciaMs = ms);
        adm.MapPost("/travar", () => e.Travado = true);
    }
}
