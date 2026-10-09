using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Adaptador en memoria del puerto IUnidadDeTrabajo.
//
// No hay nada que confirmar ni revertir: los repositorios en memoria escriben en listas
// y no existe atomicidad que gestionar. Igual implementa el puerto para que el motor en
// memoria sea completo, y asi un caso de uso que orqueste una transaccion (la venta de
// la Parte 3, o la fachada IVentaFacade) se pueda probar sin base de datos.
//
// Lleva la cuenta del estado para que el contrato se comporte igual que en MySQL:
// iniciar dos veces sin cerrar es un error de orquestacion en los dos motores, y
// confirmar o revertir sin transaccion abierta no hace nada en ninguno. Respetar esas
// mismas reglas es lo que mantiene el LSP entre los dos adaptadores.
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
                "Ya hay una transaccion en curso en esta unidad de trabajo.");
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
