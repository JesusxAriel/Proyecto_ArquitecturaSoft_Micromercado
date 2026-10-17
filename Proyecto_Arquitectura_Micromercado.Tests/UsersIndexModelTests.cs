using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Users;
using Proyecto_Arquitectura_Micromercado.Pages.Users;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UsersIndexModelTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(Roles.Cajero, false)]
    [InlineData(Roles.Administrador, true)]
    public async Task La_pagina_solo_autoriza_al_administrador(string? rol, bool autorizado)
    {
        var atributo = Assert.Single(typeof(IndexModel).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(Roles.Administrador, atributo.Roles);
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
    public async Task Get_carga_el_listado_y_los_roles_activos_desde_el_servicio()
    {
        var servicio = new ServicioPrueba();
        var pagina = CrearPagina(servicio);
        using var origen = new CancellationTokenSource();

        await pagina.OnGetAsync(origen.Token);

        Assert.Equal(servicio.Usuarios, pagina.Usuarios);
        Assert.Equal(servicio.RolesActivos, pagina.RolesActivos);
        Assert.Equal(1, servicio.LecturasUsuarios);
        Assert.Equal(1, servicio.LecturasRoles);
        Assert.All(servicio.Tokens, token => Assert.Equal(origen.Token, token));
    }

    [Fact]
    public async Task Crear_redirige_y_el_Get_refresca_el_listado_y_muestra_el_username_real_del_servicio()
    {
        var servicio = new ServicioPrueba();
        var pagina = CrearPagina(servicio);

        Assert.IsType<RedirectToPageResult>(await pagina.OnPostCreateAsync(default));

        Assert.Equal(new UsuarioCreacionDto("Ana", "Pérez", "ana@ejemplo.com", 2), servicio.Creado);
        Assert.Equal(1, servicio.Creaciones);
        Assert.Equal(42, pagina.UsuarioCreadoId);
        Assert.Equal("Usuario creado correctamente.", pagina.StatusMessage);

        var siguientePeticion = SeguirRedireccion(servicio, pagina);
        await siguientePeticion.OnGetAsync(default);

        Assert.Equal("Usuario creado correctamente.", siguientePeticion.StatusMessage);
        Assert.Equal("aperez7", siguientePeticion.UsernameGenerado);
        Assert.Contains(siguientePeticion.Usuarios, usuario => usuario.Id == 42 && usuario.Username == "aperez7");
        Assert.Equal(1, servicio.LecturasUsuarios);
        Assert.Equal(1, servicio.LecturasRoles);
        Assert.Equal(1, servicio.Creaciones);
    }

    [Fact]
    public async Task Correo_duplicado_muestra_error_en_el_campo_y_conserva_formulario_listado_y_roles()
    {
        var servicio = new ServicioPrueba { ErrorCreacion = new CorreoUsuarioDuplicadoException() };
        var pagina = CrearPagina(servicio);

        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));

        var error = Assert.Single(pagina.ModelState["NuevoUsuario.Correo"]!.Errors);
        Assert.Equal("El correo ya está registrado. Ingrese otro correo.", error.ErrorMessage);
        Assert.Equal("ana@ejemplo.com", pagina.NuevoUsuario.Correo);
        Assert.Equal(servicio.Usuarios, pagina.Usuarios);
        Assert.Equal(servicio.RolesActivos, pagina.RolesActivos);
        Assert.Null(pagina.StatusMessage);
        Assert.Null(pagina.UsuarioCreadoId);
    }

    [Fact]
    public async Task ModelState_invalido_no_crea_y_recarga_los_datos_sin_borrar_los_errores()
    {
        var servicio = new ServicioPrueba();
        var pagina = CrearPagina(servicio);
        pagina.ModelState.AddModelError("NuevoUsuario.IdRol", "Seleccione un rol.");

        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));

        Assert.Equal(0, servicio.Creaciones);
        Assert.False(pagina.ModelState.IsValid);
        Assert.Equal(servicio.Usuarios, pagina.Usuarios);
        Assert.Equal(servicio.RolesActivos, pagina.RolesActivos);
        Assert.Null(pagina.StatusMessage);
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
    public void El_formulario_valida_los_campos_obligatorios_y_el_formato(string campo, object? valor)
    {
        var entrada = FormularioValido();
        typeof(IndexModel.UsuarioFormulario).GetProperty(campo)!.SetValue(entrada, valor);
        var errores = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(entrada, new ValidationContext(entrada), errores, true));
        Assert.Contains(errores, error => error.MemberNames.Contains(campo));
    }

    [Fact]
    public void La_pagina_depende_exclusivamente_del_servicio_y_el_formulario_solo_acepta_cuatro_campos()
    {
        var constructor = Assert.Single(typeof(IndexModel).GetConstructors());
        Assert.Equal(typeof(IUsuarioAdministracionService), Assert.Single(constructor.GetParameters()).ParameterType);
        var campos = typeof(IndexModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(campos, campo => campo.FieldType.Name.Contains("Repository") ||
            (campo.FieldType.Namespace?.Contains("Infrastructure") ?? false));
        var entrada = Assert.Single(typeof(IndexModel).GetProperties(),
            propiedad => propiedad.GetCustomAttribute<BindPropertyAttribute>() is not null);
        Assert.Equal(nameof(IndexModel.NuevoUsuario), entrada.Name);
        Assert.Equal(new[] { "Apellido", "Correo", "IdRol", "Nombre" },
            entrada.PropertyType.GetProperties().Select(propiedad => propiedad.Name).OrderBy(nombre => nombre));
    }

    [Fact]
    public async Task Un_rol_rechazado_por_el_servicio_muestra_un_mensaje_seguro_y_recarga_roles()
    {
        var servicio = new ServicioPrueba { ErrorCreacion = new ArgumentException("detalle interno") };
        var pagina = CrearPagina(servicio);

        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));

        var mensaje = Assert.Single(pagina.ModelState[string.Empty]!.Errors).ErrorMessage;
        Assert.Contains("seleccione un rol activo", mensaje);
        Assert.DoesNotContain("detalle interno", mensaje);
        Assert.Equal(servicio.RolesActivos, pagina.RolesActivos);
    }

    [Fact]
    public async Task Error_inesperado_al_crear_no_expone_detalles_internos_ni_anuncia_exito()
    {
        var servicio = new ServicioPrueba { ErrorCreacion = new InvalidOperationException("SQL secreto") };
        var pagina = CrearPagina(servicio);

        Assert.IsType<PageResult>(await pagina.OnPostCreateAsync(default));

        Assert.Equal("No se pudo crear el usuario. Intente nuevamente.",
            Assert.Single(pagina.ModelState[string.Empty]!.Errors).ErrorMessage);
        Assert.Null(pagina.StatusMessage);
    }

    [Fact]
    public async Task Un_error_al_refrescar_despues_del_alta_conserva_el_exito_y_no_repite_la_creacion()
    {
        var servicio = new ServicioPrueba();
        var pagina = CrearPagina(servicio);
        await pagina.OnPostCreateAsync(default);
        servicio.ErrorListado = new InvalidOperationException("SQL secreto");
        var siguientePeticion = SeguirRedireccion(servicio, pagina);

        await siguientePeticion.OnGetAsync(default);

        Assert.Equal("Usuario creado correctamente.", siguientePeticion.StatusMessage);
        Assert.Contains("No se pudo cargar el listado", siguientePeticion.ErrorMessage);
        Assert.DoesNotContain("SQL secreto", siguientePeticion.ErrorMessage);
        Assert.Equal(1, servicio.Creaciones);
        Assert.Equal(servicio.RolesActivos, siguientePeticion.RolesActivos);
    }

    private static IndexModel.UsuarioFormulario FormularioValido() => new()
    {
        Nombre = "Ana", Apellido = "Pérez", Correo = "ana@ejemplo.com", IdRol = 2
    };

    private static IndexModel CrearPagina(ServicioPrueba servicio) =>
        new(servicio) { NuevoUsuario = FormularioValido() };

    // Las llamadas directas al PageModel no ejecutan el filtro TempData de Razor Pages.
    // Transferimos únicamente las propiedades marcadas como TempData entre peticiones.
    private static IndexModel SeguirRedireccion(ServicioPrueba servicio, IndexModel anterior)
    {
        var siguiente = CrearPagina(servicio);
        foreach (var propiedad in typeof(IndexModel).GetProperties()
                     .Where(propiedad => propiedad.GetCustomAttribute<TempDataAttribute>() is not null))
            propiedad.SetValue(siguiente, propiedad.GetValue(anterior));
        return siguiente;
    }

    private sealed class ServicioPrueba : IUsuarioAdministracionService
    {
        public List<UsuarioListadoDto> Usuarios { get; } =
            [new(1, "Luis", "Rojas", "luis@ejemplo.com", "lrojas", Roles.Administrador, "ACTIVO", new DateTime(2026, 10, 1))];
        public IReadOnlyList<RolActivoDto> RolesActivos { get; } = [new(2, Roles.Cajero)];
        public List<CancellationToken> Tokens { get; } = [];
        public int LecturasUsuarios { get; private set; }
        public int LecturasRoles { get; private set; }
        public int Creaciones { get; private set; }
        public UsuarioCreacionDto? Creado { get; private set; }
        public Exception? ErrorCreacion { get; init; }
        public Exception? ErrorListado { get; set; }

        public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default)
        {
            LecturasUsuarios++;
            Tokens.Add(cancellationToken);
            return ErrorListado is null ? Task.FromResult<IReadOnlyList<UsuarioListadoDto>>(Usuarios) :
                Task.FromException<IReadOnlyList<UsuarioListadoDto>>(ErrorListado);
        }

        public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default)
        {
            LecturasRoles++;
            Tokens.Add(cancellationToken);
            return Task.FromResult(RolesActivos);
        }

        public Task<int> CrearAsync(UsuarioCreacionDto usuario, CancellationToken cancellationToken = default)
        {
            Creaciones++;
            Tokens.Add(cancellationToken);
            Creado = usuario;
            if (ErrorCreacion is not null) return Task.FromException<int>(ErrorCreacion);
            Usuarios.Add(new(42, usuario.Nombre, usuario.Apellido, usuario.Correo, "aperez7", Roles.Cajero,
                "PENDIENTE", new DateTime(2026, 10, 17)));
            return Task.FromResult(42);
        }
    }
}
