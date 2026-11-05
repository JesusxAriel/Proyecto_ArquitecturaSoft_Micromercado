namespace PuntoMarket.Usuarios.Application.Ports.Autenticacion;

public interface IEmisorToken
{
    string Emitir(
        int usuarioId,
        string username,
        string nombreCompleto,
        string rol,
        bool debeCambiarPassword);
}
