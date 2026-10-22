namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class NoPuedeDesactivarSuPropiaCuentaException()
    : InvalidOperationException("No puedes desactivar tu propia cuenta.");
