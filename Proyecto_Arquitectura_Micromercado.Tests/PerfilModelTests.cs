using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Cuenta;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class PerfilModelTests
{
    [Fact]
    public void La_pagina_exige_usuario_autenticado_sin_restringir_por_rol()
    {
        var atributo = Assert.Single(typeof(PerfilModel).GetCustomAttributes(typeof(AuthorizeAttribute), true));

        var autorizacion = Assert.IsType<AuthorizeAttribute>(atributo);
        Assert.Null(autorizacion.Roles);
        Assert.Null(autorizacion.Policy);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cajero")]
    public void Muestra_nombre_usuario_y_rol_tomados_de_la_sesion_y_los_claims(string rol)
    {
        var modelo = CrearModelo(rol, "aperez", "Ana Pérez", "05/11/2026 08:30");

        modelo.OnGet();

        Assert.Equal("Ana Pérez", modelo.Nombre);
        Assert.Equal("aperez", modelo.NombreUsuario);
        Assert.Equal(rol, modelo.Rol);
        Assert.Equal("05/11/2026 08:30", modelo.InicioDeSesion);
    }

    [Fact]
    public void Sin_nombre_para_mostrar_usa_el_nombre_de_usuario()
    {
        var modelo = CrearModelo("Cajero", "cmamani", null, null);

        modelo.OnGet();

        Assert.Equal("cmamani", modelo.Nombre);
        Assert.Null(modelo.InicioDeSesion);
    }

    private static PerfilModel CrearModelo(string rol, string usuario, string? nombre, string? inicio)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimsDeIdentidad.NombreUsuario, usuario),
                new Claim(ClaimsDeIdentidad.Rol, rol)
            ],
            "Prueba");
        var sesion = new SesionDePrueba();
        if (nombre is not null)
            sesion.Set(VariablesDeSesion.NombreParaMostrar, Encoding.UTF8.GetBytes(nombre));
        if (inicio is not null)
            sesion.Set(VariablesDeSesion.InicioDeSesion, Encoding.UTF8.GetBytes(inicio));

        var contexto = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        contexto.Features.Set<ISessionFeature>(new CaracteristicaDeSesion { Session = sesion });
        var accion = new ActionContext(contexto, new RouteData(), new PageActionDescriptor());

        return new PerfilModel { PageContext = new PageContext(accion) };
    }

    private sealed class CaracteristicaDeSesion : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }

    private sealed class SesionDePrueba : ISession
    {
        private readonly Dictionary<string, byte[]> valores = [];

        public bool IsAvailable => true;

        public string Id => "prueba";

        public IEnumerable<string> Keys => valores.Keys;

        public void Clear() => valores.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => valores.Remove(key);

        public void Set(string key, byte[] value) => valores[key] = value;

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
            valores.TryGetValue(key, out value);
    }
}
