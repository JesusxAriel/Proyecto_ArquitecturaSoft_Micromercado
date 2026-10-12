namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteCreacionService
{
    Task<int> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default);
}
