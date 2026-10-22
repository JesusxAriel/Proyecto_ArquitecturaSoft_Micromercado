namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class ErrorEnvioCorreoException(string mensaje, Exception innerException)
    : Exception(mensaje, innerException);
