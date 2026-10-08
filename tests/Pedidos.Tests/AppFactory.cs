using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pedidos;

namespace Pedidos.Tests;

/// Dependencia falsa: o teste decide se ela esta no ar.
public sealed class FakeDependencia : IDependencia
{
    public volatile bool Ligada = true;
    public TimeSpan Atraso = TimeSpan.Zero;

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        await Task.Delay(Atraso, ct);
        return Ligada;
    }
}

public sealed class AppFactory : WebApplicationFactory<Program>
{
    public FakeDependencia Dep { get; } = new();
    public int AquecimentoSegundos { get; init; }
    public bool UsaFake { get; init; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseSetting("Admin:Habilitado", "true");
        b.UseSetting("Shutdown:DrenagemSegundos", "0");
        b.UseSetting("Aquecimento:Segundos",
            AquecimentoSegundos.ToString());
        if (!UsaFake) return;
        b.ConfigureServices(s =>
        {
            s.RemoveAll<IDependencia>();
            s.AddSingleton<IDependencia>(Dep);
        });
    }
}
