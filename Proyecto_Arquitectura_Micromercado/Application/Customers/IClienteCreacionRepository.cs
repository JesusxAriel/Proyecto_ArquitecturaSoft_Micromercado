namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteCreacionRepository
{
    Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion,
        CancellationToken cancellationToken = default);
}
