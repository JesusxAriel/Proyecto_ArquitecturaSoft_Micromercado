namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public sealed class ErrorDePersistenciaException : Exception
{
    private const string MensajePorDefecto =
        "Falló la comunicación con el almacenamiento de datos.";

    public ErrorDePersistenciaException(Exception innerException)
        : base(MensajePorDefecto, innerException)
    {
    }

    public ErrorDePersistenciaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
