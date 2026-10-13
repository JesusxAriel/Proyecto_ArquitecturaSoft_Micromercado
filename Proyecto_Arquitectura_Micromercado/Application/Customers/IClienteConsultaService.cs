namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteConsultaService
{
    Task<bool> ExisteNitCiAsync(string nitCi, CancellationToken cancellationToken = default);

    // Busca coincidencias parciales entre clientes activos; devuelve hasta 20 resultados.
    // Un termino vacio devuelve una lista vacia.
    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default);
}
