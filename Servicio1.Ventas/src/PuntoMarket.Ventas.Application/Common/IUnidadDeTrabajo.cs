namespace PuntoMarket.Ventas.Application.Common;

/// <summary>
/// Delimita una transacción compartida por los puertos de ventas y lotes de un caso de uso.
/// El adaptador debe confirmar todos sus cambios juntos o revertirlos juntos.
/// Application solo coordina este contrato; no proporciona una implementación transaccional.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task IniciarAsync(CancellationToken cancellationToken = default);

    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    Task RevertirAsync(CancellationToken cancellationToken = default);
}
