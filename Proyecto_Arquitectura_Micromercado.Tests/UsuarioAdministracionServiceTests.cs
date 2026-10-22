using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Users;
using System.Security.Cryptography;
using System.Text;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UsuarioAdministracionServiceTests
{
    private readonly Repositorio repositorio = new();
    private readonly Hasher hasher = new();
    private readonly TimeProvider reloj = new RelojFijo();
    private readonly EmailActivacion email = new();
    private static readonly UsuarioCreacionDto Valido = new("Ariana", "Pérez Quispe", " Ariana@ejemplo.com ", 2);
    private UsuarioAdministracionService Servicio(string rol = Roles.Administrador) =>
        new(repositorio, new UsuarioActual(73, rol), hasher, reloj, email);

    [Fact]
    public async Task Genera_username_normaliza_correo_y_obtiene_auditoria_del_usuario_actual()
    {
        var resultado = await Servicio().CrearAsync(Valido);
        var alta = Assert.IsType<UsuarioAltaInterna>(repositorio.Alta);
        Assert.Equal(10, resultado.Id);
        Assert.Equal("aperez", alta.Username);
        Assert.Equal("ariana@ejemplo.com", alta.Correo);
        Assert.Equal(73, alta.IdUsuarioCreacion);
        Assert.Equal("hash-generado", alta.HashContrasena);
        Assert.Equal(64, Assert.Single(hasher.Valores).Length);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Token!))),
            alta.TokenActivacionHash);
        Assert.DoesNotContain(email.Token!, alta.TokenActivacionHash, StringComparison.Ordinal);
        Assert.Equal(reloj.GetUtcNow().AddMinutes(10), email.Expira);
        Assert.Equal(email.Expira.UtcDateTime, alta.TokenActivacionExpira);
        Assert.Equal("ariana@ejemplo.com", email.Correo);
    }

    [Fact]
    public async Task Usa_primer_sufijo_disponible()
    {
        repositorio.Usernames.UnionWith(["aperez", "aperez1", "aperez2"]);
        await Servicio().CrearAsync(Valido);
        Assert.Equal("aperez3", repositorio.Alta!.Username);
    }

    [Fact]
    public async Task Reintenta_si_username_se_ocupa_entre_consulta_e_insert()
    {
        repositorio.Colision = true;
        await Servicio().CrearAsync(Valido);
        Assert.Equal("aperez1", repositorio.Alta!.Username);
    }

    [Fact]
    public async Task Rechaza_correo_duplicado_antes_de_insertar()
    {
        repositorio.CorreoDuplicado = true;
        await Assert.ThrowsAsync<CorreoUsuarioDuplicadoException>(() => Servicio().CrearAsync(Valido));
        Assert.Null(repositorio.Alta);
        Assert.Empty(hasher.Valores);
    }

    [Fact]
    public async Task Propaga_duplicado_de_correo_detectado_por_unique()
    {
        repositorio.ColisionCorreo = true;
        await Assert.ThrowsAsync<CorreoUsuarioDuplicadoException>(() => Servicio().CrearAsync(Valido));
    }

    [Fact]
    public async Task Rechaza_rol_inexistente_o_inactivo()
    {
        repositorio.RolActivo = false;
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().CrearAsync(Valido));
        Assert.Null(repositorio.Alta);
    }

    [Theory]
    [InlineData("", "Pérez", "a@ejemplo.com", 2)]
    [InlineData("Ariana", " ", "a@ejemplo.com", 2)]
    [InlineData("Ariana", "Pérez", "", 2)]
    [InlineData("Ariana", "Pérez", "correo-invalido", 2)]
    [InlineData("Ariana", "Pérez", "a@ejemplo.com", 0)]
    [InlineData("123", "Pérez", "a@ejemplo.com", 2)]
    public async Task Valida_campos_en_aplicacion(string nombre, string apellido, string correo, int rol)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().CrearAsync(new(nombre, apellido, correo, rol)));
        Assert.Null(repositorio.Alta);
    }

    [Fact]
    public async Task Limita_username_a_50_caracteres_incluyendo_sufijo()
    {
        var apellido = new string('p', 60);
        repositorio.Usernames.Add("a" + new string('p', 49));
        await Servicio().CrearAsync(Valido with { Apellido = apellido });
        Assert.Equal(50, repositorio.Alta!.Username.Length);
        Assert.EndsWith("1", repositorio.Alta.Username);
    }

    [Fact]
    public async Task Cada_alta_genera_material_aleatorio_distinto()
    {
        await Servicio().CrearAsync(Valido);
        await Servicio().CrearAsync(Valido with { Correo = "otra@ejemplo.com" });
        Assert.Equal(2, hasher.Valores.Distinct().Count());
    }

    [Fact]
    public async Task Informa_si_el_alta_se_guardo_pero_fallo_el_envio_del_correo()
    {
        email.Error = new ErrorEnvioCorreoException(
            "SMTP no disponible", new InvalidOperationException("SMTP no disponible"));

        var error = await Assert.ThrowsAsync<ErrorEnvioActivacionException>(
            () => Servicio().CrearAsync(Valido));

        Assert.Equal(10, error.UsuarioId);
        Assert.NotNull(repositorio.Alta);
        Assert.Same(email.Error, error.InnerException);
    }

    [Fact]
    public async Task Reenvia_token_nuevo_a_usuario_pendiente()
    {
        repositorio.CorreoParaActivacion = "ana@ejemplo.com";

        Assert.True(await Servicio().ReenviarActivacionAsync(10));

        Assert.NotNull(repositorio.TokenActivacionHashRenovado);
        Assert.Equal(64, repositorio.TokenActivacionHashRenovado!.Length);
        Assert.Equal("ana@ejemplo.com", email.Correo);
        Assert.NotNull(email.Token);
        Assert.Equal(reloj.GetUtcNow().AddMinutes(10), email.Expira);
    }

    [Fact]
    public async Task No_envia_correo_si_el_usuario_ya_no_esta_pendiente()
    {
        repositorio.CorreoParaActivacion = null;

        Assert.False(await Servicio().ReenviarActivacionAsync(10));

        Assert.Null(email.Correo);
    }

    [Fact]
    public async Task Si_falla_el_reenvio_informa_el_usuario_y_permite_otro_intento()
    {
        repositorio.CorreoParaActivacion = "ana@ejemplo.com";
        email.Error = new ErrorEnvioCorreoException(
            "SMTP no disponible", new InvalidOperationException("SMTP no disponible"));

        var error = await Assert.ThrowsAsync<ErrorEnvioActivacionException>(
            () => Servicio().ReenviarActivacionAsync(10));

        Assert.Equal(10, error.UsuarioId);
        Assert.Equal("ana@ejemplo.com", email.Correo);
    }

    [Fact]
    public async Task No_permite_desactivar_la_cuenta_del_administrador_actual()
    {
        await Assert.ThrowsAsync<NoPuedeDesactivarSuPropiaCuentaException>(() => Servicio().DesactivarAsync(73));
        Assert.Equal(0, repositorio.Bajas);
    }

    [Fact]
    public async Task Registra_baja_logica_con_el_administrador_actual()
    {
        repositorio.BajaResultado = true;

        Assert.True(await Servicio().DesactivarAsync(10));

        Assert.Equal((10, 73), repositorio.UltimaBaja);
    }

    [Fact]
    public async Task Listados_solo_exponen_contratos_seguros()
    {
        Assert.Same(repositorio.Listado, await Servicio().ListarAsync());
        Assert.Equal(["Id", "Nombre", "Apellido", "Correo", "Username", "Rol", "Estado", "FechaCreacion"],
            typeof(UsuarioListadoDto).GetProperties().Select(p => p.Name).ToArray());
        Assert.Equal(["Nombre", "Apellido", "Correo", "IdRol"],
            typeof(UsuarioCreacionDto).GetProperties().Select(p => p.Name).ToArray());
        Assert.Single(await Servicio().ListarRolesActivosAsync());
    }

    [Fact]
    public async Task Solo_administrador_puede_crear_y_consultar()
    {
        var servicio = Servicio(Roles.Cajero);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.CrearAsync(Valido));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ListarAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ListarRolesActivosAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.ReenviarActivacionAsync(10));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.DesactivarAsync(10));
        Assert.Null(repositorio.Alta);
    }

    private sealed record UsuarioActual(int Id, string Rol) : IUsuarioActual
    {
        public string NombreUsuario => "admin-prueba";
    }

    private sealed class RelojFijo : TimeProvider
    {
        private static readonly DateTimeOffset Ahora =
            new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private sealed class Hasher : IPasswordHasher
    {
        public List<string> Valores { get; } = [];
        public string Hash(string valor) { Valores.Add(valor); return "hash-generado"; }
        public bool Verify(string valor, string hash) => throw new NotSupportedException();
    }

    private sealed class EmailActivacion : IEmailActivacionUsuario
    {
        public string? Correo { get; private set; }
        public string? Token { get; private set; }
        public DateTimeOffset Expira { get; private set; }
        public Exception? Error { get; set; }

        public Task EnviarEnlaceAsync(
            string correo,
            string token,
            DateTimeOffset expira,
            CancellationToken cancellationToken = default)
        {
            Correo = correo;
            Token = token;
            Expira = expira;
            return Error is null ? Task.CompletedTask : Task.FromException(Error);
        }
    }

    private sealed class Repositorio : IUsuarioAdministracionRepository
    {
        public HashSet<string> Usernames { get; } = [];
        public UsuarioAltaInterna? Alta { get; private set; }
        public bool CorreoDuplicado { get; set; }
        public bool RolActivo { get; set; } = true;
        public bool Colision { get; set; }
        public bool ColisionCorreo { get; set; }
        public string? CorreoParaActivacion { get; set; }
        public string? TokenActivacionHashRenovado { get; private set; }
        public bool BajaResultado { get; set; }
        public int Bajas { get; private set; }
        public (int Usuario, int Administrador)? UltimaBaja { get; private set; }
        public IReadOnlyList<UsuarioListadoDto> Listado { get; } =
            [new(1, "Ariana", "Pérez", "a@ejemplo.com", "aperez", "Cajero", "PENDIENTE", DateTime.Today)];
        public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Listado);
        public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RolActivoDto>>([new(2, "Cajero")]);
        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) => Task.FromResult(CorreoDuplicado);
        public Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(Usernames.Contains(username));
        public Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default) => Task.FromResult(RolActivo);
        public Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default)
        {
            if (ColisionCorreo) throw new CorreoUsuarioDuplicadoException();
            if (Colision) { Colision = false; throw new UsernameUsuarioDuplicadoException(); }
            Alta = usuario;
            Usernames.Add(usuario.Username);
            return Task.FromResult(10);
        }

        public Task<string?> RenovarTokenActivacionAsync(
            int usuarioId,
            string tokenHash,
            DateTime tokenExpiraUtc,
            CancellationToken cancellationToken = default)
        {
            TokenActivacionHashRenovado = tokenHash;
            return Task.FromResult(CorreoParaActivacion);
        }

        public Task<bool> DesactivarAsync(
            int usuarioId,
            int idUsuarioEliminacion,
            CancellationToken cancellationToken = default)
        {
            Bajas++;
            UltimaBaja = (usuarioId, idUsuarioEliminacion);
            return Task.FromResult(BajaResultado);
        }
    }
}
