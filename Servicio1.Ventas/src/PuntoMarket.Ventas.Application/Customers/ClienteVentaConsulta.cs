namespace PuntoMarket.Ventas.Application.Customers;

public sealed record ClienteConsultaDto(int Id, string NitCi, string RazonSocial);
public interface IClienteVentaConsultaRepository
{
    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string termino, int limite, CancellationToken cancellationToken = default);
}
public interface IClienteVentaConsultaService
{
    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string? termino, CancellationToken cancellationToken = default);
}
public sealed class ClienteVentaConsultaService(IClienteVentaConsultaRepository repositorio) : IClienteVentaConsultaService
{
    public Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string? termino, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizado = termino?.Trim();
        if (normalizado is { Length: > 150 }) throw new ArgumentException("El término admite como máximo 150 caracteres.", nameof(termino));
        return string.IsNullOrEmpty(normalizado)
            ? Task.FromResult<IReadOnlyList<ClienteConsultaDto>>([])
            : repositorio.BuscarAsync(normalizado, 20, cancellationToken);
    }
}
