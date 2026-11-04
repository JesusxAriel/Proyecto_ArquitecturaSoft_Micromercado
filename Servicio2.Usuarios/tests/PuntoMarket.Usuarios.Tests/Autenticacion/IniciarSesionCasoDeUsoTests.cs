using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;
using PuntoMarket.Usuarios.Domain.Enums;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class IniciarSesionCasoDeUsoTests
{
    private const string Contrasena = "Segura1!";
    private const string Hash = "hash-de-prueba";
    private const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos.";

    private readonly UsuarioAutenticacionRepositorioStub repositorio = new();
    private readonly HasheadorPasswordStub hasheador = new();
    private readonly EmisorTokenStub emisorToken = new();

    private IniciarSesionCasoDeUso CrearCaso() =>
        new(repositorio, hasheador, emisorToken);

    [Fact]
    public async Task Inicia_sesion_y_emite_token_para_usuario_activo_con_rol_activo()
    {
        repositorio.Usuario = UsuarioValido();
        hasheador.ResultadoVerificacion = true;

        var salida = await CrearCaso().EjecutarAsync(new(" ana@example.com ", Contrasena));

        Assert.Equal("jwt-de-prueba", salida.Token);
        Assert.Equal(42, salida.UsuarioId);
        Assert.Equal("ana", salida.Username);
        Assert.Equal("Ana Pérez", salida.NombreCompleto);
        Assert.Equal("Administrador", salida.Rol);
        Assert.True(salida.DebeCambiarPassword);
        Assert.Equal("ana@example.com", repositorio.CorreoConsultado);
        Assert.Equal((Contrasena, Hash), hasheador.ArgumentosVerificados);
        Assert.Equal((42, "ana", "Ana Pérez", "Administrador", true), emisorToken.Argumentos);
    }

    [Fact]
    public async Task Rechaza_correo_inexistente_con_mensaje_generico_sin_verificar_ni_emitir_token()
    {
        repositorio.Usuario = null;

        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso().EjecutarAsync(new("ana@example.com", Contrasena)));

        Assert.Equal(MensajeCredencialesInvalidas, excepcion.Message);
        Assert.Null(hasheador.ArgumentosVerificados);
        Assert.Null(emisorToken.Argumentos);
    }

    [Fact]
    public async Task Rechaza_contrasena_incorrecta_con_mensaje_generico_sin_emitir_token()
    {
        repositorio.Usuario = UsuarioValido();
        hasheador.ResultadoVerificacion = false;

        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso().EjecutarAsync(new("ana@example.com", Contrasena)));

        Assert.Equal(MensajeCredencialesInvalidas, excepcion.Message);
        Assert.Null(emisorToken.Argumentos);
    }

    [Fact]
    public async Task Rechaza_usuario_inactivo_con_mensaje_generico_sin_emitir_token()
    {
        repositorio.Usuario = UsuarioValido() with { Estado = EstadoUsuario.INACTIVO };
        hasheador.ResultadoVerificacion = true;

        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso().EjecutarAsync(new("ana@example.com", Contrasena)));

        Assert.Equal(MensajeCredencialesInvalidas, excepcion.Message);
        Assert.Null(emisorToken.Argumentos);
    }

    [Fact]
    public async Task Rechaza_rol_inactivo_con_mensaje_generico_sin_emitir_token()
    {
        repositorio.Usuario = UsuarioValido() with { RolActivo = false };
        hasheador.ResultadoVerificacion = true;

        var excepcion = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso().EjecutarAsync(new("ana@example.com", Contrasena)));

        Assert.Equal(MensajeCredencialesInvalidas, excepcion.Message);
        Assert.Null(emisorToken.Argumentos);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" ", "Segura1!")]
    [InlineData("ana@example.com", "")]
    [InlineData("ana@example.com", "  ")]
    public async Task Rechaza_datos_vacios_antes_de_consultar_repositorio(
        string correo,
        string contrasena)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CrearCaso().EjecutarAsync(new(correo, contrasena)));

        Assert.Null(repositorio.CorreoConsultado);
        Assert.Null(hasheador.ArgumentosVerificados);
        Assert.Null(emisorToken.Argumentos);
    }

    [Fact]
    public async Task Propaga_cancellation_token_al_repositorio()
    {
        repositorio.Usuario = UsuarioValido();
        hasheador.ResultadoVerificacion = true;
        using var origen = new CancellationTokenSource();

        await CrearCaso().EjecutarAsync(
            new("ana@example.com", Contrasena),
            origen.Token);

        Assert.Equal(origen.Token, repositorio.CancellationTokenRecibido);
    }

    private static UsuarioAutenticacionDto UsuarioValido() =>
        new(
            42,
            "ana",
            Hash,
            "Ana Pérez",
            EstadoUsuario.ACTIVO,
            true,
            "Administrador",
            true);

    private sealed class UsuarioAutenticacionRepositorioStub : IUsuarioAutenticacionRepositorio
    {
        public UsuarioAutenticacionDto? Usuario { get; set; }
        public string? CorreoConsultado { get; private set; }
        public CancellationToken CancellationTokenRecibido { get; private set; }

        public Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
            string correo,
            CancellationToken cancellationToken = default)
        {
            CorreoConsultado = correo;
            CancellationTokenRecibido = cancellationToken;
            return Task.FromResult(Usuario);
        }
    }

    private sealed class HasheadorPasswordStub : IHasheadorPassword
    {
        public bool ResultadoVerificacion { get; set; }
        public (string Contrasena, string Hash)? ArgumentosVerificados { get; private set; }

        public string Hash(string contrasena) =>
            throw new NotSupportedException();

        public bool Verify(string contrasena, string hash)
        {
            ArgumentosVerificados = (contrasena, hash);
            return ResultadoVerificacion;
        }
    }

    private sealed class EmisorTokenStub : IEmisorToken
    {
        public (int UsuarioId, string Username, string NombreCompleto, string Rol, bool DebeCambiarPassword)?
            Argumentos { get; private set; }

        public string Emitir(
            int usuarioId,
            string username,
            string nombreCompleto,
            string rol,
            bool debeCambiarPassword)
        {
            Argumentos = (usuarioId, username, nombreCompleto, rol, debeCambiarPassword);
            return "jwt-de-prueba";
        }
    }
}
