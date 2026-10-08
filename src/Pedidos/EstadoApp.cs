namespace Pedidos;

/// Estado compartilhado que os health checks consultam.
public sealed class EstadoApp
{
    public volatile bool CacheAquecido;
    public volatile bool Drenando;
    public volatile bool Travado;
    public volatile bool DependenciaLigada = true;
    public int LatenciaDependenciaMs;
}
