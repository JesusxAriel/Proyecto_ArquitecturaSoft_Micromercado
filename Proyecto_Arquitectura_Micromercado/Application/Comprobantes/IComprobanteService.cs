namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

public interface IComprobanteService
{
    Task<ComprobanteDto?> ObtenerAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
