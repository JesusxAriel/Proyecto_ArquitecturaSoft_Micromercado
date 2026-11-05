using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Pages.Users;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UsersIndexModelTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("Cajero", false)]
    [InlineData("Administrador", true)]
    public async Task La_pagina_solo_autoriza_al_administrador(string? rol, bool autorizado)
    {
        var atributo = Assert.Single(typeof(IndexModel).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("Administrador", atributo.Roles);
        Assert.Empty(typeof(IndexModel).GetCustomAttributes<AllowAnonymousAttribute>());
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthorization();
        using var proveedor = servicios.BuildServiceProvider();
        var politica = await AuthorizationPolicy.CombineAsync(
            proveedor.GetRequiredService<IAuthorizationPolicyProvider>(), [atributo]);
        var identidad = rol is null ? new ClaimsIdentity() :
            new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "Prueba");
        var resultado = await proveedor.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(identidad), null, politica!);
        Assert.Equal(autorizado, resultado.Succeeded);
    }

    [Fact]
    public async Task Get_deserializa_el_contrato_y_conserva_expiracion_y_fechas_de_la_API()
    {
        using var api = new ApiPrueba();
        var pagina = CrearPagina(api.Cliente);
        await pagina.OnGetAsync(default);
        var usuario = Assert.Single(pagina.Usuarios);
        Assert.Equal("aperez", usuario.Username);
        Assert.True(usuario.EstaExpirado);
        Assert.Equal(new DateTime(2099, 1, 1), usuario.TokenActivacionExpira);
        Assert.Null(usuario.FechaActivacion);
        Assert.Equal("Cajero", Assert.Single(pagina.RolesActivos).Nombre);
        Assert.Equal(new[] { "GET /api/usuarios", "GET /api/usuarios/roles" }, api.Peticiones);
    }

    [Fact]
    public async Task Crear_envia_solo_los_campos_del_alta_y_recupera_username_tras_redireccion()
    {
        using var api = new ApiPrueba();
        var pagina = CrearPagina(api.Cliente);
        Assert.IsType<RedirectToPageResult>(await pagina.OnPostCreateAsync(default));
        Assert.Equal(42, pagina.UsuarioCreadoId);
        Assert.Equal("Usuario creado correctamente.", pagina.StatusMessage);
        using var cuerpo = JsonDocument.Parse(Assert.Single(api.Cuerpos));
        Assert.Equal(new[] { "apellido", "correo", "idRol", "nombre" },
            cuerpo.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal("Ana", cuerpo.RootElement.GetProperty("nombre").GetString());
        Assert.Equal(2, cuerpo.RootElement.GetProperty("idRol").GetInt32());
        await pagina.OnGetAsync(default);
        Assert.Equal("aperez", pagina.UsernameGenerado);
        Assert.Single(api.Peticiones, p => p == "POST /api/usuarios");
    }

    [Fact]
    public async Task Baja_y_reenvio_usan_los_verbos_y_rutas_de_la_API()
    {
        using var api = new ApiPrueba();
        var pagina = CrearPagina(api.Cliente);
        Assert.IsType<RedirectToPageResult>(await pagina.OnPostReenviarActivacionAsync(42, default));
        Assert.Equal("Se envió un nuevo enlace de activación.", pagina.StatusMessage);
        Assert.IsType<RedirectToPageResult>(await pagina.OnPostDesactivarAsync(42, default));
        Assert.Equal("Usuario dado de baja correctamente.", pagina.StatusMessage);
        Assert.Equal(new[] { "POST /api/usuarios/42/reenviar-activacion", "DELETE /api/usuarios/42" }, api.Peticiones);
    }

    [Fact]
    public async Task Un_reenvio_no_realizado_no_anuncia_exito()
    {
        using var api = new ApiPrueba { Reenviado = false };
        var pagina = CrearPagina(api.Cliente);
        Assert.IsType<PageResult>(await pagina.OnPostReenviarActivacionAsync(42, default));
        Assert.Null(pagina.StatusMessage);
        Assert.Contains("No se reenvió", pagina.ErrorMessage);
    }

    [Fact]
    public async Task ModelState_invalido_no_envia_alta_y_recarga_listado_y_roles()
    {
        using var api = new ApiPrueba();
        var pagina = CrearPagina(api.Cliente);
        pagina.ModelState.AddModelError("NuevoUsuario.IdRol", "Seleccione un rol.");
        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));
        Assert.DoesNotContain(api.Peticiones, p => p.StartsWith("POST"));
        Assert.Single(pagina.Usuarios);
        Assert.Single(pagina.RolesActivos);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Error_de_alta_conserva_formulario_y_no_expone_detalles_internos(HttpStatusCode estado)
    {
        using var api = new ApiPrueba { ErrorAlta = estado };
        var pagina = CrearPagina(api.Cliente);
        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));
        Assert.Equal("ana@ejemplo.com", pagina.NuevoUsuario.Correo);
        var error = Assert.Single(pagina.ModelState[string.Empty]!.Errors).ErrorMessage;
        Assert.DoesNotContain("SQL secreto", error);
        Assert.Contains("antes de repetir el alta", error);
        Assert.Null(pagina.StatusMessage);
        Assert.Null(pagina.UsuarioCreadoId);
        Assert.Single(pagina.Usuarios);
        Assert.Single(pagina.RolesActivos);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Error_de_autenticacion_no_se_presenta_como_listado_vacio_exitoso(HttpStatusCode estado)
    {
        using var api = new ApiPrueba { ErrorListado = estado };
        var pagina = CrearPagina(api.Cliente);
        await pagina.OnGetAsync(default);
        Assert.NotNull(pagina.ErrorMessage);
        Assert.Empty(pagina.Usuarios);
        Assert.Single(pagina.RolesActivos);
    }

    [Fact]
    public async Task JSON_invalido_se_muestra_como_error_y_permite_cargar_roles()
    {
        using var api = new ApiPrueba { JsonListado = "no es JSON" };
        var pagina = CrearPagina(api.Cliente);
        await pagina.OnGetAsync(default);
        Assert.Contains("respuesta inválida", pagina.ErrorMessage);
        Assert.Single(pagina.RolesActivos);
    }

    [Theory]
    [InlineData("Nombre", "")]
    [InlineData("Nombre", "   ")]
    [InlineData("Apellido", "")]
    [InlineData("Correo", "")]
    [InlineData("Correo", "correo-invalido")]
    [InlineData("IdRol", null)]
    [InlineData("IdRol", 0)]
    [InlineData("IdRol", -1)]
    public void El_formulario_valida_campos_obligatorios_y_formato(string campo, object? valor)
    {
        var entrada = FormularioValido();
        typeof(IndexModel.UsuarioFormulario).GetProperty(campo)!.SetValue(entrada, valor);
        var errores = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(entrada, new ValidationContext(entrada), errores, true));
        Assert.Contains(errores, e => e.MemberNames.Contains(campo));
    }

    [Fact]
    public void Las_paginas_solo_reciben_el_cliente_HTTP_y_el_formulario_de_alta_tiene_cuatro_campos()
    {
        foreach (var tipo in new[] { typeof(IndexModel), typeof(ActivarModel) })
        {
            var constructor = Assert.Single(tipo.GetConstructors());
            Assert.Equal(typeof(UsuariosApiClient), Assert.Single(constructor.GetParameters()).ParameterType);
        }
        var entrada = Assert.Single(typeof(IndexModel).GetProperties(),
            p => p.GetCustomAttribute<BindPropertyAttribute>() is not null);
        Assert.Equal(new[] { "Apellido", "Correo", "IdRol", "Nombre" },
            entrada.PropertyType.GetProperties().Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Activacion_envia_token_y_contrasenas_y_muestra_exito()
    {
        using var api = new ApiPrueba();
        var pagina = CrearActivacion(api.Cliente);
        Assert.IsType<PageResult>(await pagina.OnPostAsync(default));
        Assert.True(pagina.ActivacionCompleta);
        Assert.Equal("POST /api/activacion", Assert.Single(api.Peticiones));
        var cuerpo = JsonSerializer.Deserialize<ActivarCuentaDto>(Assert.Single(api.Cuerpos), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(new ActivarCuentaDto("token+seguro", "Clave123!", "Clave123!"), cuerpo);
        Assert.Equal("no-store", pagina.Response.Headers.CacheControl);
        Assert.Equal("no-referrer", pagina.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public void Activacion_lee_token_por_query_sin_consultar_su_validez_antes_del_POST()
    {
        using var api = new ApiPrueba();
        var pagina = CrearActivacion(api.Cliente);
        Assert.True(typeof(ActivarModel).GetProperty(nameof(ActivarModel.Token))!
            .GetCustomAttribute<BindPropertyAttribute>()!.SupportsGet);
        pagina.OnGet();
        Assert.False(pagina.TokenInvalido);
        Assert.Empty(api.Peticiones);
        Assert.Single(typeof(ActivarModel).GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task Activacion_sin_token_o_con_formulario_invalido_no_llama_API()
    {
        using var api = new ApiPrueba();
        var pagina = CrearActivacion(api.Cliente);
        pagina.Token = null;
        await pagina.OnPostAsync(default);
        Assert.True(pagina.TokenInvalido);
        pagina.Token = "token";
        pagina.ModelState.AddModelError("Input.ConfirmarContrasena", "Las contraseñas no coinciden.");
        await pagina.OnPostAsync(default);
        Assert.Empty(api.Peticiones);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, HttpStatusCode.BadRequest)]
    public async Task Activacion_rechazada_muestra_error(bool activada, HttpStatusCode? error)
    {
        using var api = new ApiPrueba { Activada = activada, ErrorActivacion = error };
        var pagina = CrearActivacion(api.Cliente);
        await pagina.OnPostAsync(default);
        Assert.False(pagina.ActivacionCompleta);
        Assert.False(pagina.ModelState.IsValid);
    }

    private static IndexModel.UsuarioFormulario FormularioValido() => new()
    {
        Nombre = "Ana", Apellido = "Pérez", Correo = "ana@ejemplo.com", IdRol = 2
    };

    private static IndexModel CrearPagina(UsuariosApiClient cliente) => new(cliente) { NuevoUsuario = FormularioValido() };

    private static ActivarModel CrearActivacion(UsuariosApiClient cliente) => new(cliente)
    {
        PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
        Token = "token+seguro",
        Input = new ActivarModel.ActivarInput { Contrasena = "Clave123!", ConfirmarContrasena = "Clave123!" }
    };

    private sealed class ApiPrueba : HttpMessageHandler
    {
        private readonly HttpClient http;
        public UsuariosApiClient Cliente { get; }
        public List<string> Peticiones { get; } = [];
        public List<string> Cuerpos { get; } = [];
        public bool Reenviado { get; init; } = true;
        public bool Activada { get; init; } = true;
        public HttpStatusCode? ErrorAlta { get; init; }
        public HttpStatusCode? ErrorListado { get; init; }
        public HttpStatusCode? ErrorActivacion { get; init; }
        public string? JsonListado { get; init; }

        public ApiPrueba()
        {
            http = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://usuarios.test/") };
            Cliente = new UsuariosApiClient(http);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ruta = request.RequestUri!.AbsolutePath;
            Peticiones.Add($"{request.Method} {ruta}");
            if (request.Content is not null)
                Cuerpos.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            var error = (request.Method.Method, ruta) switch
            {
                ("POST", "/api/usuarios") => ErrorAlta,
                ("GET", "/api/usuarios") => ErrorListado,
                ("POST", "/api/activacion") => ErrorActivacion,
                _ => null
            };
            if (error.HasValue)
                return new HttpResponseMessage(error.Value) { Content = new StringContent("SQL secreto") };
            return (request.Method.Method, ruta) switch
            {
                ("GET", "/api/usuarios") when JsonListado is not null => new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(JsonListado, Encoding.UTF8, "application/json") },
                ("GET", "/api/usuarios") => Json(new ListarUsuariosRespuesta([
                    new(42, "Ana", "Pérez", "ana@ejemplo.com", "aperez", "Cajero", "PENDIENTE",
                        new DateTime(2026, 1, 1), new DateTime(2099, 1, 1), null, true)])),
                ("GET", "/api/usuarios/roles") => Json(new[] { new RolDto(2, "Cajero") }),
                ("POST", "/api/usuarios") => Json(new CrearUsuarioRespuesta(42), HttpStatusCode.Created),
                ("POST", "/api/usuarios/42/reenviar-activacion") => Json(new ReenviarActivacionRespuesta(Reenviado)),
                ("DELETE", "/api/usuarios/42") => new HttpResponseMessage(HttpStatusCode.NoContent),
                ("POST", "/api/activacion") => Json(new ActivarCuentaRespuesta(Activada)),
                _ => throw new InvalidOperationException($"Petición inesperada: {request.Method} {ruta}")
            };
        }

        private static HttpResponseMessage Json<T>(T contenido, HttpStatusCode estado = HttpStatusCode.OK) =>
            new(estado) { Content = JsonContent.Create(contenido) };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                http.Dispose();
            base.Dispose(disposing);
        }
    }
}
