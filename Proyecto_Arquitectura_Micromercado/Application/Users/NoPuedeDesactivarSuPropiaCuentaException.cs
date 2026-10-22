namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class NoPuedeDesactivarSuPropiaCuentaException()
    : InvalidOperationException("No puede desactivar su propia cuenta.");
