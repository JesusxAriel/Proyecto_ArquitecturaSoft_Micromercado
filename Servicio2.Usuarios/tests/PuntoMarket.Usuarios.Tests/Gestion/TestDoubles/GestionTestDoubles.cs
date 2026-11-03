using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;

namespace PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;

internal sealed class RelojFijo : IReloj
{
    public DateTimeOffset UtcNow { get; } = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
}

internal sealed record UsuarioActualStub(int Id = 7, string Rol = "Administrador") : IUsuarioActual;

internal sealed class HasheadorPasswordSpy : IHasheadorPassword
{
    public List<string> Contrasenas { get; } = [];
    public Action? AlHashear { get; set; }
    public string Hash(string contrasena)
    {
        Contrasenas.Add(contrasena);
        AlHashear?.Invoke();
        return "hash-password-prueba";
    }

    public bool Verify(string contrasena, string hash) => throw new NotSupportedException();
}

internal sealed class GeneradorTokenSpy : IGeneradorTokenActivacion
{
    public List<(DateTimeOffset Ahora, TimeSpan Vigencia)> Solicitudes { get; } = [];
    public List<string> TokensHasheados { get; } = [];
    public List<TokenActivacion> Tokens { get; } = [];

    public TokenActivacion Generar(DateTimeOffset ahoraUtc, TimeSpan vigencia)
    {
        Solicitudes.Add((ahoraUtc, vigencia));
        var token = new TokenActivacion($"raw-nuevo-{Solicitudes.Count}",
            $"hash-token-nuevo-{Solicitudes.Count}", ahoraUtc.Add(vigencia));
        Tokens.Add(token);
        return token;
    }

    public string CalcularHash(string token)
    {
        TokensHasheados.Add(token);
        return $"hash-de:{token}";
    }
}

internal sealed class EnviadorCorreoSpy : IEnviadorCorreo
{
    public List<(string Correo, string Token, DateTimeOffset Expira)> Envios { get; } = [];
    public Action? AlEnviar { get; set; }

    public Task EnviarActivacionAsync(string correo, string token, DateTimeOffset expira,
        CancellationToken cancellationToken = default)
    {
        AlEnviar?.Invoke();
        Envios.Add((correo, token, expira));
        return Task.CompletedTask;
    }
}

internal sealed class UsuarioGestionRepositorioStub : IUsuarioGestionRepositorio
{
    public bool ExisteCorreo { get; set; }
    public bool RolActivo { get; set; } = true;
    public HashSet<string> UsernamesExistentes { get; } = [];
    public List<string> UsernamesConsultados { get; } = [];
    public List<UsuarioAltaInterna> Altas { get; } = [];
    public int IdCreado { get; set; } = 42;
    public bool FallarCreacion { get; set; }
    public IReadOnlyList<UsuarioListadoDto> Usuarios { get; set; } = [];
    public UsuarioActivacionDto? UsuarioParaActivacion { get; set; }
    public string? CorreoRenovacion { get; set; } = "ana@example.com";
    public bool ResultadoDesactivacion { get; set; } = true;
    public bool ResultadoTokenValido { get; set; } = true;
    public Dictionary<string, DateTime>? TokensVigentes { get; set; }
    public bool ResultadoActivacion { get; set; } = true;
    public Action? AlActivar { get; set; }
    public List<string> Llamadas { get; } = [];
    public List<(int UsuarioId, string Hash, DateTime Expira)> Renovaciones { get; } = [];
    public List<(int UsuarioId, int IdUsuarioEliminacion)> Bajas { get; } = [];
    public List<(string Hash, DateTime Ahora)> Validaciones { get; } = [];
    public List<(string TokenHash, string PasswordHash, DateTime Ahora)> Activaciones { get; } = [];

    public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ListarAsync));
        return Task.FromResult(Usuarios);
    }

    public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ExisteUsernameAsync));
        UsernamesConsultados.Add(username);
        return Task.FromResult(UsernamesExistentes.Contains(username));
    }

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ExisteCorreoAsync));
        return Task.FromResult(ExisteCorreo);
    }

    public Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(EsRolActivoAsync));
        return Task.FromResult(RolActivo);
    }

    public Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(CrearAsync));
        Altas.Add(usuario);
        return FallarCreacion
            ? Task.FromException<int>(new InvalidOperationException("Fallo de persistencia simulado."))
            : Task.FromResult(IdCreado);
    }

    public Task<UsuarioActivacionDto?> ObtenerParaActivacionAsync(int usuarioId,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(ObtenerParaActivacionAsync));
        return Task.FromResult(UsuarioParaActivacion);
    }

    public Task<string?> RenovarTokenActivacionAsync(int usuarioId, string tokenHash, DateTime tokenExpiraUtc,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(RenovarTokenActivacionAsync));
        Renovaciones.Add((usuarioId, tokenHash, tokenExpiraUtc));
        return Task.FromResult(CorreoRenovacion);
    }

    public Task<bool> DesactivarAsync(int usuarioId, int idUsuarioEliminacion,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(DesactivarAsync));
        Bajas.Add((usuarioId, idUsuarioEliminacion));
        return Task.FromResult(ResultadoDesactivacion);
    }

    public Task<bool> TokenValidoAsync(string tokenHash, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        Llamadas.Add(nameof(TokenValidoAsync));
        Validaciones.Add((tokenHash, ahoraUtc));
        return Task.FromResult(TokensVigentes is null ? ResultadoTokenValido :
            TokensVigentes.TryGetValue(tokenHash, out var expira) && expira > ahoraUtc);
    }

    public Task<bool> ActivarAsync(string tokenHash, string passwordHash, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        AlActivar?.Invoke();
        Llamadas.Add(nameof(ActivarAsync));
        Activaciones.Add((tokenHash, passwordHash, ahoraUtc));
        return Task.FromResult(ResultadoActivacion);
    }
}
