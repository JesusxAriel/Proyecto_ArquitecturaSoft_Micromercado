namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class ErrorEnvioActivacionException(int usuarioId, Exception innerException)
    : Exception("El usuario fue creado, pero no se pudo enviar el correo de activación.", innerException)
{
    public int UsuarioId { get; } = usuarioId;
}
