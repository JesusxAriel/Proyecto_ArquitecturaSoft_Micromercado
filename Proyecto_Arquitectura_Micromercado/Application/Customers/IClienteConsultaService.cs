namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteConsultaService
{
    // Busca coincidencias parciales entre clientes activos; devuelve hasta 20 resultados.
    // Un termino vacio devuelve una lista vacia.
    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default);
}
