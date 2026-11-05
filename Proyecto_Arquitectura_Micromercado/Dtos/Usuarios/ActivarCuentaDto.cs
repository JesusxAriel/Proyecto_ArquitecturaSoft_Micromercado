namespace Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;

public sealed record ActivarCuentaDto(string Token, string NuevaContrasena, string ConfirmarContrasena);

public sealed record ActivarCuentaRespuesta(bool Activada);
