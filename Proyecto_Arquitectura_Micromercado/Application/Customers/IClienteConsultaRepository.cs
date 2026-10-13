namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public interface IClienteConsultaRepository
{
    Task<bool> ExisteNitCiAsync(string nitCi, CancellationToken cancellationToken = default);

    // Busca coincidencias parciales literales en CI/NIT o razon social de clientes activos.
    // Devuelve hasta 20 resultados ordenados por razon social e id; vacio si no hay termino.
    Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default);
}
