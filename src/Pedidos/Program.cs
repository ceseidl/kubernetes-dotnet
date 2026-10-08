using Pedidos;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(o =>
    o.TimestampFormat = "HH:mm:ss.fff ");

builder.Services.AddSingleton<EstadoApp>();
builder.Services
    .AddSingleton<IDependencia, DependenciaSimulada>();
builder.Services.AddHostedService<Aquecimento>();
builder.Services.AddHostedService<Drenagem>();
builder.Services.AddHealthChecks()
    .AddCheck<VivoCheck>("vivo", tags: ["live"])
    .AddCheck<ProntoCheck>("dependencia", tags: ["ready"])
    .AddCheck<CacheCheck>("cache", tags: ["startup"]);

var app = builder.Build();

app.Use(async (http, next) =>
{
    http.Response.Headers["X-Pod"] = Environment.MachineName;
    await next();
});

app.MapHealthChecks("/health/live", Saude.Rota("live"));
app.MapHealthChecks("/health/ready", Saude.Rota("ready"));
app.MapHealthChecks("/health/startup", Saude.Rota("startup"));

app.MapPedidos();
app.MapInfo();
app.MapAdmin();

app.Run();

public partial class Program;
