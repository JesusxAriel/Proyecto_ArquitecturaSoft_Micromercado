namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;

public sealed record CrearUsuarioEntrada(string Nombre, string Apellido, string Correo, int IdRol);
