namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public sealed class ClienteConsultaService(
    IClienteConsultaRepository repositorio) : IClienteConsultaService
{
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
