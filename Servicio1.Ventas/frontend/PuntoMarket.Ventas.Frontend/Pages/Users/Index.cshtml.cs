using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
namespace PuntoMarket.Ventas.Frontend.Pages.Users;
[Authorize(Roles="Administrador")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class IndexModel(IUsuariosGestionApiClient api) : PageModel
{
    public IReadOnlyList<UsuarioGestion> Usuarios {get;private set;}=[];
    public UsuarioGestion? ConfirmarBaja {get;private set;}
    public string? ErrorGestion {get;private set;}
    [TempData] public string? MensajeGestion {get;set;}
    public bool EsPropia(UsuarioGestion u)=>User.FindFirst(ClaimTypes.NameIdentifier)?.Value==u.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { try {Usuarios=await api.Listar(ct);}catch(UsuariosGestionException e){Error(e);} return Page(); }
    public async Task<IActionResult> OnGetConfirmarBajaAsync(int id,CancellationToken ct)
    {
        try { Usuarios=await api.Listar(ct);var u=Usuarios.SingleOrDefault(x=>x.Id==id);if(u is null||EsPropia(u)||!(u.EsPendiente||u.Estado=="ACTIVO")) throw new UsuariosGestionException(409);ConfirmarBaja=u; }
        catch(UsuariosGestionException e){Error(e);} return Page();
    }
    public async Task<IActionResult> OnPostBajaAsync(int id,bool confirmar,CancellationToken ct)
    {
        if(!ModelState.IsValid||id<=0||!confirmar) {Response.StatusCode=400;ErrorGestion="Confirme la baja de una cuenta válida.";return Page();}
        try {
            Usuarios=await api.Listar(ct);var u=Usuarios.SingleOrDefault(x=>x.Id==id);
            if(u is null||EsPropia(u)||!(u.EsPendiente||u.Estado=="ACTIVO")) throw new UsuariosGestionException(409);
            await api.Baja(id,ct);MensajeGestion="La API confirmó la baja lógica de la cuenta.";return RedirectToPage();
        } catch(UsuariosGestionException e){Error(e);Usuarios=[];return Page();}
    }
    public async Task<IActionResult> OnPostReenviarAsync(int id,CancellationToken ct)
    {
        if(!ModelState.IsValid||id<=0) {Response.StatusCode=400;ErrorGestion="Identificador inválido.";return Page();}
        try {
            Usuarios=await api.Listar(ct);var u=Usuarios.SingleOrDefault(x=>x.Id==id);
            if(u is null||!u.EsPendiente||u.FechaActivacion is not null) throw new UsuariosGestionException(409);
            await api.Reenviar(id,ct);MensajeGestion="La API confirmó la solicitud de reenvío. Verifique la recepción del mensaje.";return RedirectToPage();
        } catch(UsuariosGestionException e){Error(e);Usuarios=[];return Page();}
    }
    private void Error(UsuariosGestionException e){ErrorGestion=e.Message;Response.StatusCode=e.Estado;}
}
