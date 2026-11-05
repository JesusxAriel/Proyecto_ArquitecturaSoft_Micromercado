namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class CorreoUsuarioDuplicadoException() : Exception("El correo ya está registrado.");

public sealed class UsernameUsuarioDuplicadoException() : Exception("El nombre de usuario ya está registrado.");
