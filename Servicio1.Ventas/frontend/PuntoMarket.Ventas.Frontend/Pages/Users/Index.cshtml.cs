using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Users;
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IndexModel(IUsuariosGestionApiClient api, SesionesUsuario sesiones, ControlAltaUsuarios control) : PageModel
{
    private const string MensajeAltaRechazada = "No se pudo crear el usuario. Verifique los datos e intente de nuevo.";
    private const string MensajeAltaIncierta = "El usuario se creó pero no se pudo enviar el correo de activación. Use Reenviar activación más tarde.";
    private const string MensajeSolicitudProcesada = "Esta solicitud ya se procesó. Revise el listado antes de crear otro usuario.";
    public IReadOnlyList<UsuarioGestion> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolUsuarioGestion> Roles { get; private set; } = [];
    public UsuarioGestion? ConfirmarBaja { get; private set; }
    public string? ErrorGestion { get; private set; }
    public string? ErrorAlta { get; private set; }
    public bool AltaBloqueada { get; private set; }
    [BindProperty] public AltaUsuarioGestion Input { get; set; } = new();
    [BindProperty] public string? Solicitud { get; set; }
    [TempData] public string? MensajeGestion { get; set; }
    [TempData] public int? UsuarioCreadoId { get; set; }
    public bool EsPropia(UsuarioGestion u) => User.FindFirst(ClaimTypes.NameIdentifier)?.Value == u.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private (string Id, DateTimeOffset Vence) Sesion()
    {
        var id = User.FindFirst(AccesoUsuariosPreparado.ClaimSesion)?.Value;
        var identidad = sesiones.Identidad(id);
        return id is not null && identidad is not null ? (id, identidad.VenceEn) : throw new UsuariosGestionException(401);
    }
    private async Task CargarAsync(CancellationToken ct, bool conRoles = true)
    {
        Usuarios = await api.Listar(ct);
        if (conRoles) Roles = await api.Roles(ct);
    }
    private void NuevaSolicitud()
    {
        var s = Sesion();
        Solicitud = control.Nueva(s.Id, s.Vence);
        ModelState.Remove(nameof(Solicitud));
    }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try
        {
            await CargarAsync(ct);
            NuevaSolicitud();
            if (UsuarioCreadoId is int id)
            {
                var nombre = Usuarios.FirstOrDefault(u => u.Id == id)?.Username;
                MensajeGestion = nombre is null ? "Usuario creado." : $"Usuario creado. Nombre de usuario generado: {nombre}.";
            }
        }
        catch (UsuariosGestionException e) { Fallo(e); AltaBloqueada = true; }
        return Page();
    }
    public async Task<IActionResult> OnPostCrearAsync(CancellationToken ct)
    {
        try
        {
            await CargarAsync(ct);
            if (!Roles.Any(x => x.Id == Input.IdRol)) ModelState.AddModelError("Input.IdRol", "Seleccione uno de los roles disponibles.");
            if (!ModelState.IsValid) { Response.StatusCode = 400; return Page(); }
            var s = Sesion();
            if (!control.Iniciar(s.Id, Solicitud, Input.Correo, s.Vence))
            {
                AltaBloqueada = true;
                Response.StatusCode = 409;
                ErrorAlta = MensajeSolicitudProcesada;
                return Page();
            }
            var existiaAntes = Usuarios.Any(u => string.Equals(u.Correo, Input.Correo.Trim(), StringComparison.OrdinalIgnoreCase));
            try
            {
                var id = await api.Crear(Input, ct);
                UsuarioCreadoId = id;
                return RedirectToPage();
            }
            catch (UsuariosGestionException e)
            {
                var creadoSinCorreo = false;
                if (e.Estado == 409 && !existiaAntes)
                {
                    await RecargarListado(ct);
                    creadoSinCorreo = Usuarios.Any(u => string.Equals(u.Correo, Input.Correo.Trim(), StringComparison.OrdinalIgnoreCase));
                }
                if (e.Estado is 400 or 403 or 409 && !creadoSinCorreo)
                {
                    control.PermitirCorreccion(s.Id, Input.Correo);
                    NuevaSolicitud();
                    ErrorAlta = e.Estado == 403 ? e.Message : MensajeAltaRechazada;
                }
                else
                {
                    AltaBloqueada = true;
                    ErrorAlta = e.Estado == 401 ? e.Message : MensajeAltaIncierta;
                    await RecargarListado(ct);
                }
                Response.StatusCode = e.Estado;
            }
        }
        catch (UsuariosGestionException e) { Fallo(e); AltaBloqueada = true; }
        return Page();
    }
    private async Task RecargarListado(CancellationToken ct)
    {
        try { Usuarios = await api.Listar(ct); }
        catch (UsuariosGestionException) { Usuarios = []; }
    }
    public async Task<IActionResult> OnGetConfirmarBajaAsync(int id, CancellationToken ct)
    {
        try
        {
            await CargarAsync(ct);
            NuevaSolicitud();
            var u = Usuarios.SingleOrDefault(x => x.Id == id);
            if (u is null || EsPropia(u) || !(u.EsPendiente || u.Estado == "ACTIVO")) throw new UsuariosGestionException(409);
            ConfirmarBaja = u;
        }
        catch (UsuariosGestionException e) { Fallo(e); }
        return Page();
    }
    public async Task<IActionResult> OnPostBajaAsync(int id, bool confirmar, CancellationToken ct)
    {
        if (id <= 0 || !confirmar) { Response.StatusCode = 400; ErrorGestion = "Confirme la baja de una cuenta válida."; return await Recuperar(ct); }
        try
        {
            Usuarios = await api.Listar(ct);
            var u = Usuarios.SingleOrDefault(x => x.Id == id);
            if (u is null || EsPropia(u) || !(u.EsPendiente || u.Estado == "ACTIVO")) throw new UsuariosGestionException(409);
            await api.Baja(id, ct);
            MensajeGestion = "Usuario dado de baja.";
            return RedirectToPage();
        }
        catch (UsuariosGestionException e) { Fallo(e, "No se pudo confirmar la baja. Actualice el listado antes de intentarlo de nuevo."); return await Recuperar(ct); }
    }
    public async Task<IActionResult> OnPostReenviarAsync(int id, CancellationToken ct)
    {
        if (id <= 0) { Response.StatusCode = 400; ErrorGestion = "Seleccione un usuario válido."; return await Recuperar(ct); }
        try
        {
            Usuarios = await api.Listar(ct);
            var u = Usuarios.SingleOrDefault(x => x.Id == id);
            if (u is null || !u.EsPendiente || u.FechaActivacion is not null) throw new UsuariosGestionException(409);
            await api.Reenviar(id, ct);
            MensajeGestion = $"Se reenvió el correo de activación a {u.Correo}.";
            return RedirectToPage();
        }
        catch (UsuariosGestionException e) { Fallo(e, "No se pudo reenviar el correo de activación. Intente nuevamente más tarde."); return await Recuperar(ct); }
    }
    private async Task<IActionResult> Recuperar(CancellationToken ct)
    {
        try { await CargarAsync(ct); NuevaSolicitud(); }
        catch (UsuariosGestionException) { Usuarios = []; AltaBloqueada = true; }
        return Page();
    }
    private void Fallo(UsuariosGestionException e, string? incierto = null)
    {
        ErrorGestion = e.ResultadoIncierto && incierto is not null ? incierto : e.Estado == 409 ? "La operación no corresponde al estado actual del usuario." : e.Message;
        Response.StatusCode = e.Estado;
    }
}
