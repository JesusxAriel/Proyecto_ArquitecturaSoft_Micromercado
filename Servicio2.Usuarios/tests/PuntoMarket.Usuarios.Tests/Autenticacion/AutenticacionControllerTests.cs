using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PuntoMarket.Usuarios.Api.Contracts.Autenticacion;
using PuntoMarket.Usuarios.Api.Controllers;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;
using PuntoMarket.Usuarios.Domain.Enums;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class AutenticacionControllerTests
{
    [Fact]
    public async Task Inicio_valido_devuelve_200_con_token_sin_datos_internos()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.IniciarSesion(
            new IniciarSesionSolicitud { Correo = "ana@example.com", Contrasena = "Segura1!" },
            CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var respuesta = Assert.IsType<IniciarSesionRespuesta>(ok.Value);
        var json = JsonSerializer.Serialize(respuesta);

        Assert.Equal("jwt-de-prueba", respuesta.Token);
        Assert.DoesNotContain("PasswordHash", json);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NombreCompleto", json);
    }

    [Fact]
    public async Task Credenciales_incorrectas_devuelven_401_con_mensaje_generico()
    {
        var controlador = CrearControlador(usuarioInexistente: true);

        var resultado = await controlador.IniciarSesion(
            new IniciarSesionSolicitud { Correo = "ana@example.com", Contrasena = "mala" },
            CancellationToken.None);
        var noAutorizado = Assert.IsType<UnauthorizedObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(noAutorizado.Value);

        Assert.Equal(StatusCodes.Status401Unauthorized, problema.Status);
        Assert.Equal("Correo o contraseña incorrectos.", problema.Title);
    }

    [Theory]
    [InlineData("", "Segura1!")]
    [InlineData("ana@example.com", " ")]
    public async Task Datos_vacios_devuelven_400(string correo, string contrasena)
    {
        var controlador = CrearControlador();

        var resultado = await controlador.IniciarSesion(
            new IniciarSesionSolicitud { Correo = correo, Contrasena = contrasena },
            CancellationToken.None);
        var solicitudInvalida = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(solicitudInvalida.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, problema.Status);
    }

    [Fact]
    public void Solicitud_valida_requiere_correo_con_formato_y_contrasena()
    {
        var solicitud = new IniciarSesionSolicitud
        {
            Correo = "no-es-correo",
            Contrasena = string.Empty
        };
        var resultados = new List<ValidationResult>();

        var esValida = Validator.TryValidateObject(
            solicitud,
            new ValidationContext(solicitud),
            resultados,
            validateAllProperties: true);

        Assert.False(esValida);
        Assert.Equal(2, resultados.Count);
    }

    [Fact]
    public void Controlador_permite_acceso_anonimo()
    {
        var metodo = typeof(AutenticacionController).GetMethod(nameof(AutenticacionController.IniciarSesion))!;
        Assert.Contains(metodo.GetCustomAttributes(inherit: true),
            attribute => attribute is AllowAnonymousAttribute);
    }

    [Fact]
    public void Cambio_password_requiere_autenticacion()
    {
        var metodo = typeof(AutenticacionController).GetMethod(nameof(AutenticacionController.CambiarPassword))!;

        Assert.Contains(metodo.GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
        Assert.DoesNotContain(metodo.GetCustomAttributes(inherit: true),
            attribute => attribute is AllowAnonymousAttribute);
    }

    [Fact]
    public async Task Cambio_password_responde_sin_hash_ni_datos_del_usuario()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.CambiarPassword(
            new CambiarPasswordSolicitud
            {
                ContrasenaActual = "Segura1!",
                NuevaContrasena = "NuevaSegura2!"
            },
            CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var respuesta = Assert.IsType<CambiarPasswordRespuesta>(ok.Value);
        var json = JsonSerializer.Serialize(respuesta);

        Assert.True(respuesta.Cambiada);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("usuario", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cambio_password_rechaza_contrasena_actual_incorrecta_con_400()
    {
        var controlador = CrearControlador(contrasenaActualCorrecta: false);

        var resultado = await controlador.CambiarPassword(
            new CambiarPasswordSolicitud
            {
                ContrasenaActual = "incorrecta",
                NuevaContrasena = "NuevaSegura2!"
            },
            CancellationToken.None);

        var invalido = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(invalido.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problema.Status);
        Assert.Equal("La contraseña actual es incorrecta.", problema.Title);
    }

    [Fact]
    public async Task Cambio_password_rechaza_reutilizar_contrasena_actual_con_400()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.CambiarPassword(
            new CambiarPasswordSolicitud
            {
                ContrasenaActual = "Segura1!",
                NuevaContrasena = "Segura1!"
            },
            CancellationToken.None);

        var invalido = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(invalido.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problema.Status);
        Assert.Equal(
            "La nueva contraseña no puede ser igual a la contraseña actual.",
            problema.Title);
    }

    [Fact]
    public async Task Cambio_password_devuelve_mensaje_correcto_si_falta_contrasena_actual()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.CambiarPassword(
            new CambiarPasswordSolicitud
            {
                ContrasenaActual = "",
                NuevaContrasena = "NuevaSegura2!"
            },
            CancellationToken.None);

        var invalido = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(invalido.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problema.Status);
        Assert.Equal("La contraseña actual es obligatoria.", problema.Title);
    }

    [Fact]
    public async Task Cambio_password_rechaza_nueva_contrasena_debil()
    {
        var controlador = CrearControlador();

        var resultado = await controlador.CambiarPassword(
            new CambiarPasswordSolicitud
            {
                ContrasenaActual = "Segura1!",
                NuevaContrasena = "debil"
            },
            CancellationToken.None);

        var invalido = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(invalido.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problema.Status);
        Assert.Equal("La nueva contraseña no cumple con los requisitos de seguridad.", problema.Title);
    }

    [Fact]
    public async Task Error_inesperado_devuelve_respuesta_generica_sin_detalles()
    {
        const string detalleInterno = "secreto interno de persistencia";
        var controlador = CrearControlador(error: new InvalidOperationException(detalleInterno));

        var resultado = await controlador.IniciarSesion(
            new IniciarSesionSolicitud { Correo = "ana@example.com", Contrasena = "Segura1!" },
            CancellationToken.None);
        var error = Assert.IsType<ObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(error.Value);
        var json = JsonSerializer.Serialize(problema);

        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
        Assert.DoesNotContain(detalleInterno, json);
        Assert.DoesNotContain(nameof(InvalidOperationException), json);
    }

    private static AutenticacionController CrearControlador(
        UsuarioAutenticacionDto? usuario = null,
        Exception? error = null,
        bool usuarioInexistente = false,
        bool contrasenaActualCorrecta = true)
    {
        var repositorio = new RepositorioStub(
            usuario ?? (usuarioInexistente ? null : UsuarioValido()),
            error);
        var caso = new IniciarSesionCasoDeUso(
            repositorio,
            new HasheadorStub(),
            new EmisorStub());
        var casoCambioPassword = new CambiarPasswordCasoDeUso(
            repositorio,
            new HasheadorStub(contrasenaActualCorrecta),
            new UsuarioActualStub());

        return new AutenticacionController(
            caso,
            casoCambioPassword,
            NullLogger<AutenticacionController>.Instance);
    }

    private sealed class RepositorioStub(
        UsuarioAutenticacionDto? usuario,
        Exception? error) : IUsuarioAutenticacionRepositorio
    {
        public Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
            string correo,
            CancellationToken cancellationToken = default)
        {
            if (error is not null)
                throw error;

            return Task.FromResult(usuario);
        }

        public Task<UsuarioAutenticacionDto?> ObtenerPorIdAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            if (error is not null)
                throw error;

            return Task.FromResult(usuario?.Id == usuarioId ? usuario : null);
        }

        public Task<bool> ActualizarPasswordAsync(
            int usuarioId,
            int idUsuarioActualizacion,
            string passwordHashActual,
            string passwordHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario?.Id == usuarioId);
    }

    private sealed class HasheadorStub(bool contrasenaActualCorrecta = true) : IHasheadorPassword
    {
        public string Hash(string contrasena) => "hash-nuevo-no-sensible";
        public bool Verify(string contrasena, string hash) =>
            contrasenaActualCorrecta && contrasena == "Segura1!";
    }

    private sealed class EmisorStub : IEmisorToken
    {
        public string Emitir(
            int usuarioId,
            string username,
            string nombreCompleto,
            string rol,
            bool debeCambiarPassword) => "jwt-de-prueba";
    }

    private sealed class UsuarioActualStub : IUsuarioActual
    {
        public int Id => 42;
        public string Rol => "Administrador";
    }

    private static UsuarioAutenticacionDto UsuarioValido() =>
        new(42, "ana", "hash-sensible", "Ana Pérez", EstadoUsuario.ACTIVO, false, "Administrador", true);
}
