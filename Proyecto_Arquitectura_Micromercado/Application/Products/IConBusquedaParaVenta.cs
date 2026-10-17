namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConBusquedaParaVenta
{
    Task<ResultadoBusquedaParaVenta> BuscarParaVentaAsync(
        string termino,
        int limite,
        CancellationToken cancellationToken = default);
}
