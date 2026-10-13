namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteConsultaRepository
{
    Task<bool> ExisteNitCiAsync(string nitCi, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default);
}
