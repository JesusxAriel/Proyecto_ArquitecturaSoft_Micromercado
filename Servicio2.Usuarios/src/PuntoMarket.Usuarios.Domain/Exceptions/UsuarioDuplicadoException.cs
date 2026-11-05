namespace PuntoMarket.Usuarios.Domain.Exceptions;

public sealed class UsuarioDuplicadoException() : Exception("El usuario ya está registrado.");
