namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public interface IFachadaAnulacion
{
    Task AnularVentaAsync(
        int idVenta,
        string motivo,
        CancellationToken cancellationToken = default);
}
