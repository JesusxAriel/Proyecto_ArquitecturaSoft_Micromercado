using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;

public sealed class IniciarSesionCasoDeUso(
    IUsuarioAutenticacionRepositorio repositorio,
    IHasheadorPassword hasheador,
    IEmisorToken emisorToken)
{
    private const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos.";

    public async Task<IniciarSesionSalida> EjecutarAsync(
        IniciarSesionEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(entrada.Correo))
            throw new ArgumentException("El correo es obligatorio.", nameof(entrada));
        if (string.IsNullOrWhiteSpace(entrada.Contrasena))
            throw new ArgumentException("La contraseña es obligatoria.", nameof(entrada));

        var usuario = await repositorio.ObtenerPorCorreoAsync(
            entrada.Correo.Trim(),
            cancellationToken);

        if (usuario is null ||
            !hasheador.Verify(entrada.Contrasena, usuario.PasswordHash) ||
            usuario.Estado != EstadoUsuario.ACTIVO ||
            !usuario.RolActivo)
        {
            throw new UnauthorizedAccessException(MensajeCredencialesInvalidas);
        }

        var token = emisorToken.Emitir(
            usuario.Id,
            usuario.Username,
            usuario.NombreCompleto,
            usuario.Rol,
            usuario.DebeCambiarPassword);

        return new IniciarSesionSalida(
            token,
            usuario.Id,
            usuario.Username,
            usuario.NombreCompleto,
            usuario.Rol,
            usuario.DebeCambiarPassword);
    }
}
