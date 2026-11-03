namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record CrearUsuarioSolicitud(string Nombre, string Apellido, string Correo, int IdRol);
