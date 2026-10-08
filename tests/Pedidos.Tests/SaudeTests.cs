using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Pedidos.Tests;

public class SaudeTests : IClassFixture<AppFactory>
{
    const HttpStatusCode Ok = HttpStatusCode.OK;
    const HttpStatusCode Fora = HttpStatusCode.ServiceUnavailable;

    readonly AppFactory _app;
    readonly HttpClient _http;

    public SaudeTests(AppFactory app)
    {
        _app = app;
        _http = app.CreateClient();
    }

    async Task<HttpStatusCode> Status(string rota) =>
        (await _http.GetAsync(rota)).StatusCode;

    [Fact]
    public async Task Live_nao_depende_da_dependencia()
    {
        _app.Dep.Ligada = false;
        Assert.Equal(Ok, await Status("/health/live"));
        _app.Dep.Ligada = true;
    }

    [Fact]
    public async Task Ready_acompanha_a_dependencia()
    {
        _app.Dep.Ligada = true;
        Assert.Equal(Ok, await Status("/health/ready"));

        _app.Dep.Ligada = false;
        Assert.Equal(Fora, await Status("/health/ready"));

        _app.Dep.Ligada = true;
        Assert.Equal(Ok, await Status("/health/ready"));
    }

    [Fact]
    public async Task Ready_tem_timeout_se_a_dependencia_trava()
    {
        _app.Dep.Atraso = TimeSpan.FromSeconds(30);
        var r = await _http.GetAsync("/health/ready");
        _app.Dep.Atraso = TimeSpan.Zero;

        Assert.Equal(Fora, r.StatusCode);
        var corpo = await r.Content.ReadAsStringAsync();
        Assert.Contains("lenta", corpo);
    }

    [Fact]
    public async Task Admin_derruba_a_dependencia()
    {
        using var app = new AppFactory { UsaFake = false };
        var http = app.CreateClient();

        await http.PostAsync("/admin/dependencia/false", null);
        var r = await http.GetAsync("/health/ready");
        Assert.Equal(Fora, r.StatusCode);

        await http.PostAsync("/admin/dependencia/true", null);
        r = await http.GetAsync("/health/ready");
        Assert.Equal(Ok, r.StatusCode);
    }

    [Fact]
    public async Task Shutdown_drena_a_readiness()
    {
        using var app = new AppFactory();
        var http = app.CreateClient();
        Assert.Equal(Ok, (await http.GetAsync("/health/ready"))
            .StatusCode);

        var drenagem = app.Services.GetServices<IHostedService>()
            .OfType<Drenagem>().Single();
        await drenagem.StoppingAsync(CancellationToken.None);

        Assert.Equal(Fora, (await http.GetAsync("/health/ready"))
            .StatusCode);
        Assert.Equal(Ok, (await http.GetAsync("/health/live"))
            .StatusCode);
    }

    [Fact]
    public async Task Startup_503_enquanto_o_cache_aquece()
    {
        using var app = new AppFactory { AquecimentoSegundos = 30 };
        var http = app.CreateClient();
        var r = await http.GetAsync("/health/startup");
        Assert.Equal(Fora, r.StatusCode);
    }

    [Fact]
    public async Task Startup_fica_ok_depois_de_aquecer()
    {
        for (var i = 0; i < 50; i++)
        {
            if (await Status("/health/startup") == Ok) return;
            await Task.Delay(100);
        }
        Assert.Fail("cache nao aqueceu em 5 s");
    }

    [Fact]
    public async Task Cria_e_le_pedido()
    {
        var r = await _http.PostAsJsonAsync("/pedidos",
            new NovoPedido("Ana", 42.5m));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var criado = await r.Content.ReadFromJsonAsync<Pedido>();
        var lido = await _http.GetFromJsonAsync<Pedido>(
            $"/pedidos/{criado!.Id}");
        Assert.Equal("Ana", lido!.Cliente);
    }
}
