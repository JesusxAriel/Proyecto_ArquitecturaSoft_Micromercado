namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public interface IUnidadDeTrabajo
{
    Task IniciarAsync(CancellationToken cancellationToken = default);

    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    Task RevertirAsync(CancellationToken cancellationToken = default);
}
