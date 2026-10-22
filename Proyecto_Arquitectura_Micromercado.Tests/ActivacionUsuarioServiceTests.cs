using System.Security.Cryptography;
using System.Text;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ActivacionUsuarioServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);
    private readonly Repositorio repositorio = new();
    private readonly Hasher hasher = new();
    private readonly ActivacionUsuarioService servicio =
        null!;

    public ActivacionUsuarioServiceTests()
    {
        servicio = new ActivacionUsuarioService(repositorio, hasher, new RelojFijo());
    }

    [Fact]
    public async Task Activar_guarda_hash_de_contrasena_y_hash_del_token()
    {
        const string token = "token-seguro";

        Assert.True(await servicio.ActivarAsync(token, "NuevaClave1!"));

        Assert.Equal(Hash(token), repositorio.TokenHash);
        Assert.Equal(Hash("NuevaClave1!"), repositorio.PasswordHash);
        Assert.Equal(Ahora.UtcDateTime, repositorio.AhoraUtc);
        Assert.Equal(1, repositorio.Activaciones);
        Assert.Single(hasher.Valores);
        Assert.DoesNotContain(token, repositorio.TokenHash, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta1!")]
    [InlineData("nominuscula1!")]
    [InlineData("SINNUMERO!")]
    [InlineData("SinEspecial1")]
    public async Task Rechaza_contrasenas_que_no_cumplen_la_politica(string contrasena)
    {
        Assert.False(await servicio.ActivarAsync("token-seguro", contrasena));

        Assert.Equal(0, repositorio.Activaciones);
        Assert.Empty(hasher.Valores);
    }

    [Fact]
    public async Task Token_invalido_no_actualiza_la_cuenta()
    {
        repositorio.TokenValido = false;

        Assert.False(await servicio.ActivarAsync("token-vencido", "NuevaClave1!"));

        Assert.Equal(0, repositorio.Activaciones);
        Assert.Empty(hasher.Valores);
    }

    [Fact]
    public async Task Valida_token_usando_su_hash_y_hora_utc_actual()
    {
        Assert.True(await servicio.TokenValidoAsync("token-seguro"));

        Assert.Equal(Hash("token-seguro"), repositorio.TokenHash);
        Assert.Equal(Ahora.UtcDateTime, repositorio.AhoraUtc);
    }

    private static string Hash(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Hasher : IPasswordHasher
    {
        public List<string> Valores { get; } = [];
        public string Hash(string contrasena)
        {
            Valores.Add(contrasena);
            return ActivacionUsuarioServiceTests.Hash(contrasena);
        }

        public bool Verify(string contrasena, string hash) =>
            ActivacionUsuarioServiceTests.Hash(contrasena) == hash;
    }

    private sealed class Repositorio : IActivacionUsuarioRepository
    {
        public string? TokenHash { get; private set; }
        public string? PasswordHash { get; private set; }
        public DateTime AhoraUtc { get; private set; }
        public int Activaciones { get; private set; }
        public bool TokenValido { get; set; } = true;

        public Task<bool> TokenValidoAsync(
            string tokenHash,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            TokenHash = tokenHash;
            AhoraUtc = ahoraUtc;
            return Task.FromResult(TokenValido);
        }

        public Task<bool> ActivarAsync(
            string tokenHash,
            string passwordHash,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            TokenHash = tokenHash;
            PasswordHash = passwordHash;
            AhoraUtc = ahoraUtc;
            Activaciones++;
            return Task.FromResult(TokenValido);
        }
    }
}
