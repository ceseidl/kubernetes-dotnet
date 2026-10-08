using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Pedidos;

public static class Saude
{
    /// Cada rota executa so os checks com a sua tag.
    public static HealthCheckOptions Rota(string tag) => new()
    {
        Predicate = r => r.Tags.Contains(tag),
        ResponseWriter = (http, rel) =>
            http.Response.WriteAsJsonAsync(new
            {
                status = rel.Status.ToString(),
                checks = rel.Entries.Select(e => new
                {
                    nome = e.Key,
                    status = e.Value.Status.ToString(),
                    detalhe = e.Value.Description
                })
            })
    };
}
