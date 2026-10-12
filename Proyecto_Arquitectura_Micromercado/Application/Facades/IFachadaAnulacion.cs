namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public interface IFachadaAnulacion
{
    Task<bool> AnularVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
