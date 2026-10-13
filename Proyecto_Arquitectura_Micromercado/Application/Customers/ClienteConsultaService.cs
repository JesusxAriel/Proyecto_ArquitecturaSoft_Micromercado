namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public sealed class ClienteConsultaService(
    IClienteConsultaRepository repositorio) : IClienteConsultaService
{
    public Task<bool> ExisteNitCiAsync(string nitCi, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(nitCi))
        {
            return Task.FromResult(false);
        }

        return repositorio.ExisteNitCiAsync(nitCi.Trim(), cancellationToken);
    }

    public Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(termino))
        {
            return Task.FromResult<IReadOnlyList<ClienteConsultaDto>>(
                Array.Empty<ClienteConsultaDto>());
        }

        return repositorio.BuscarAsync(termino.Trim(), cancellationToken);
    }
}
