namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public sealed class TokenDeAccesoDelUsuario(
    IHttpContextAccessor accesor,
    IConfiguration configuracion,
    IHostEnvironment entorno) : ITokenDeAcceso
{
    public const string ClaimDeTokenDeAcceso = "access_token";
    public const string ClaveDeTokenDePrueba = "Reportes:TokenDePrueba";

    public string Obtener() =>
        TokenDelUsuario()
        ?? TokenDePruebaEnDesarrollo()
        ?? throw new TokenDeAccesoNoDisponibleException();

    private string? TokenDelUsuario() =>
        ConValor(accesor.HttpContext?.User.FindFirst(ClaimDeTokenDeAcceso)?.Value);

    private string? TokenDePruebaEnDesarrollo() =>
        entorno.IsDevelopment() ? ConValor(configuracion[ClaveDeTokenDePrueba]) : null;

    private static string? ConValor(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto;
}
