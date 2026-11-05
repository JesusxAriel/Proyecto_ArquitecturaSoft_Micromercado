namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaCategorias
{
    Task<IReadOnlyList<CategoriaDisponible>> ListarAsync(CancellationToken cancelacion);
}

public sealed record CategoriaDisponible(int Id, string Nombre);
