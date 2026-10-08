// Carga continua: conta respostas por status e por pod.
// Uso: dotnet run --project tools/Carga -- URL SEGUNDOS
using System.Collections.Concurrent;
using System.Diagnostics;

var url = args.Length > 0
    ? args[0] : "http://localhost:30080/info";
var segundos = args.Length > 1 ? int.Parse(args[1]) : 30;
var status = new ConcurrentDictionary<string, int>();
var pods = new ConcurrentDictionary<string, int>();
var fim = Stopwatch.StartNew();
var inicio = DateTime.Now;

async Task Worker()
{
    using var http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    while (fim.Elapsed.TotalSeconds < segundos)
    {
        try
        {
            var r = await http.GetAsync(url);
            var cod = ((int)r.StatusCode).ToString();
            status.AddOrUpdate(cod, 1, (_, n) => n + 1);
            if (r.Headers.TryGetValues("X-Pod", out var p))
                pods.AddOrUpdate(p.First(), 1, (_, n) => n + 1);
        }
        catch (Exception e)
        {
            var nome = e.GetType().Name;
            status.AddOrUpdate(nome, 1, (_, n) => n + 1);
            Console.WriteLine(
                $"{(DateTime.Now - inicio).TotalSeconds:F1}s " +
                $"erro: {e.GetType().Name}");
        }
        await Task.Delay(20);
    }
}

await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Worker()));

Console.WriteLine("Status:");
foreach (var (k, v) in status.OrderBy(x => x.Key))
    Console.WriteLine($"  {k}: {v}");
Console.WriteLine("Pods:");
foreach (var (k, v) in pods.OrderBy(x => x.Key))
    Console.WriteLine($"  {k}: {v}");
