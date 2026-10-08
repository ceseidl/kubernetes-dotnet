namespace Pedidos;

/// Dependencia externa (banco, fila...). Simulada no exemplo.
public interface IDependencia
{
    Task<bool> PingAsync(CancellationToken ct);
}

public sealed class DependenciaSimulada(EstadoApp estado)
    : IDependencia
{
    public async Task<bool> PingAsync(CancellationToken ct)
    {
        await Task.Delay(estado.LatenciaDependenciaMs, ct);
        return estado.DependenciaLigada;
    }
}
