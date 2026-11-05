using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.DarDeBajaUsuario;

public sealed class DarDeBajaUsuarioCasoDeUso(
    IUsuarioGestionRepositorio repositorio,
    IUsuarioActual usuarioActual)
{
    public async Task<DarDeBajaUsuarioSalida> EjecutarAsync(
        DarDeBajaUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var idUsuarioEliminacion = usuarioActual.Id;
        if (usuarioActual.Rol != "Administrador" || idUsuarioEliminacion <= 0)
            throw new UnauthorizedAccessException("La administración de usuarios requiere el rol Administrador.");

        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        if (entrada.UsuarioId <= 0)
            throw new ArgumentException("El ID del usuario debe ser mayor a cero.");
        if (entrada.UsuarioId == idUsuarioEliminacion)
            throw new InvalidOperationException("No puede dar de baja su propia cuenta.");

        if (!await repositorio.DesactivarAsync(entrada.UsuarioId, idUsuarioEliminacion, cancellationToken))
            throw new InvalidOperationException("No se pudo realizar la baja del usuario.");

        return new DarDeBajaUsuarioSalida(true);
    }
}
