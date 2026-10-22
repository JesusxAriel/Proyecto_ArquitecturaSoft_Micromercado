using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

public sealed class UnidadDeTrabajoEnMemoria : IUnidadDeTrabajo
{
    public bool HayTransaccionActiva { get; private set; }

    public int ConfirmacionesRealizadas { get; private set; }

    public int ReversionesRealizadas { get; private set; }

    public Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        if (HayTransaccionActiva)
        {
            throw new InvalidOperationException(
                "Ya hay una transacción en curso en esta unidad de trabajo.");
        }

        HayTransaccionActiva = true;

        return Task.CompletedTask;
    }

    public Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        if (HayTransaccionActiva)
        {
            HayTransaccionActiva = false;
            ConfirmacionesRealizadas++;
        }

        return Task.CompletedTask;
    }

    public Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        if (HayTransaccionActiva)
        {
            HayTransaccionActiva = false;
            ReversionesRealizadas++;
        }

        return Task.CompletedTask;
    }
}
