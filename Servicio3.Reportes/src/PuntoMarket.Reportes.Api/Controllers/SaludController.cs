using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Reportes.Application.Ports.Comunes;

namespace PuntoMarket.Reportes.Api.Controllers;

[ApiController]
[Route("api/salud")]
public class SaludController : ControllerBase
{
    private readonly IUsuarioActual _usuarioActual;

    public SaludController(IUsuarioActual usuarioActual)
    {
        _usuarioActual = usuarioActual;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Obtener() => Ok(new { estado = "activo" });

    [Authorize]
    [HttpGet("protegido")]
    public IActionResult ObtenerProtegido() =>
        Ok(new { id = _usuarioActual.Id, nombreDeUsuario = _usuarioActual.NombreDeUsuario });
}
