namespace PuntoMarket.Ventas.Application.Customers;

/// <summary>Verifica la vigencia del cliente sin exponer operaciones de administración.</summary>
public interface IClienteActivoConsulta
{
    Task<bool> ExisteActivoAsync(int idCliente, CancellationToken cancellationToken = default);
}
