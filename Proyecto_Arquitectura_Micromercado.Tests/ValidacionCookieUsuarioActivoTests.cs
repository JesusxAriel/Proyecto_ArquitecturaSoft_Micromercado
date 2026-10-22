using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ValidacionCookieUsuarioActivoTests
{
    [Fact]
    public async Task Comprueba_el_estado_actual_del_usuario_por_id_de_claim()
    {
        var usuarios = new Repositorio { IdActivo = 8 };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsDeIdentidad.IdUsuario, "8")],
            "Cookies"));

        Assert.True(await ValidacionCookieUsuarioActivo.EstaActivaAsync(principal, usuarios));
        Assert.Equal(8, usuarios.UltimoIdConsultado);
    }

    [Fact]
    public async Task Rechaza_principal_sin_un_id_valido_sin_consultar_repositorio()
    {
        var usuarios = new Repositorio { IdActivo = 8 };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsDeIdentidad.IdUsuario, "no-es-un-id")],
            "Cookies"));

        Assert.False(await ValidacionCookieUsuarioActivo.EstaActivaAsync(principal, usuarios));
        Assert.Null(usuarios.UltimoIdConsultado);
    }

    [Fact]
    public async Task Una_cuenta_desactivada_no_pasa_la_validacion_de_cookie()
    {
        var usuarios = new Repositorio();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsDeIdentidad.IdUsuario, "8")],
            "Cookies"));

        Assert.False(await ValidacionCookieUsuarioActivo.EstaActivaAsync(principal, usuarios));
    }

    private sealed class Repositorio : IUsuarioRepository
    {
        public int? IdActivo { get; init; }
        public int? UltimoIdConsultado { get; private set; }

        public Task<bool> EstaActivoAsync(int usuarioId, CancellationToken cancellationToken = default)
        {
            UltimoIdConsultado = usuarioId;
            return Task.FromResult(IdActivo == usuarioId);
        }

        public Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(string correo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CredencialesDeUsuario?> ObtenerCredencialesPorIdAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ActualizarPasswordAsync(int usuarioId, string nuevoPasswordHash, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
