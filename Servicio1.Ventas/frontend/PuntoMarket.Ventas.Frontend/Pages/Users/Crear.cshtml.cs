using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Users;
[Authorize(Roles="Administrador")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CrearModel(IUsuariosGestionApiClient api,SesionesUsuario sesiones,ControlAltaUsuarios control) : PageModel
{
    [BindProperty] public AltaUsuarioGestion Input {get;set;}=new();
    [BindProperty] public string? Solicitud {get;set;}
    public IReadOnlyList<RolUsuarioGestion> Roles {get;private set;}=[];
    public string? ErrorGestion {get;private set;}
    public bool Bloqueada {get;private set;}
    private (string Id,DateTimeOffset Vence) Sesion(){var id=User.FindFirst(AccesoUsuariosPreparado.ClaimSesion)?.Value;var i=sesiones.Identidad(id);return id is not null&&i is not null?(id,i.VenceEn):throw new UsuariosGestionException(401);}
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try{Roles=await api.Roles(ct);var s=Sesion();Solicitud=control.Nueva(s.Id,s.Vence);}catch(UsuariosGestionException e){Error(e);Bloqueada=true;}return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        try {
            Roles=await api.Roles(ct);
            if(!Roles.Any(x=>x.Id==Input.IdRol)) ModelState.AddModelError("Input.IdRol","Seleccione uno de los roles activos disponibles.");
            if(!ModelState.IsValid){Response.StatusCode=400;return Page();}
            var s=Sesion();
            if(!control.Iniciar(s.Id,Solicitud,Input.Correo,s.Vence)) { Bloqueada=true;throw new UsuariosGestionException(409); }
            try {
                await api.Crear(Input,ct);
                TempData["MensajeGestion"]="La API confirmó el alta. Verifique la recepción del correo de activación antes de utilizar la cuenta.";
                return RedirectToPage("Index");
            } catch(UsuariosGestionException e) {
                if(e.Estado is 400 or 403 or 409) {control.PermitirCorreccion(s.Id,Input.Correo);Solicitud=control.Nueva(s.Id,s.Vence);ModelState.Remove(nameof(Solicitud));}
                else Bloqueada=true;
                throw;
            }
        }catch(UsuariosGestionException e){Error(e);return Page();}
    }
    private void Error(UsuariosGestionException e){ErrorGestion=Bloqueada&&e.Estado==409?"Esta solicitud o correo ya se procesó o no pudo confirmarse. Consulte el listado antes de otra acción.":e.Message;Response.StatusCode=e.Estado;}
}
