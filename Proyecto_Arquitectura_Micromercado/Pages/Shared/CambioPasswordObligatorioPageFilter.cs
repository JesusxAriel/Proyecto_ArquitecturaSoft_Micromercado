using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public sealed class CambioPasswordObligatorioPageFilter : IAsyncPageFilter
{
    private const string PaginaCambioPassword = "/Users/CambiarPassword";
    private const string PaginaCerrarSesion = "/Logout";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) =>
        Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var pagina = context.ActionDescriptor.RouteValues.TryGetValue("page", out var ruta)
            ? ruta
            : null;

        if (DebeRedirigir(context.HttpContext.User, pagina))
        {
            context.Result = new RedirectToPageResult(PaginaCambioPassword);
            return;
        }

        await next();
    }

    public static bool DebeRedirigir(ClaimsPrincipal usuario, string? pagina) =>
        usuario.Identity?.IsAuthenticated == true
        && bool.TryParse(
            usuario.FindFirst(ClaimsDeIdentidad.DebeCambiarPassword)?.Value,
            out var debeCambiarPassword)
        && debeCambiarPassword
        && !string.Equals(pagina, PaginaCambioPassword, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(pagina, PaginaCerrarSesion, StringComparison.OrdinalIgnoreCase);
}
