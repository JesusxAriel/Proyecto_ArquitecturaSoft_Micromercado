using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Clients;
public interface IUsuariosGestionApiClient
{
    Task<IReadOnlyList<UsuarioGestion>> Listar(CancellationToken ct);
    Task<IReadOnlyList<RolUsuarioGestion>> Roles(CancellationToken ct);
    Task<int> Crear(AltaUsuarioGestion alta,CancellationToken ct);
    Task Baja(int id,CancellationToken ct);
    Task Reenviar(int id,CancellationToken ct);
}
public sealed class UsuariosGestionException(int estado,bool incierto=false) : Exception(incierto
    ? "No se pudo confirmar la operación. Puede haberse guardado antes de fallar el correo o la conexión. Consulte el listado antes de realizar otra acción; no se reintentó automáticamente."
    : estado switch { 400=>"Revise los datos de la solicitud.",401=>"La sesión venció o no es válida. Inicie sesión nuevamente.",403=>"No tiene permiso para gestionar usuarios.",409=>"La cuenta ya existe o la operación no corresponde a su estado actual.",_=>"Usuarios no está disponible. Intente consultar nuevamente más tarde." })
{
    public int Estado {get;}=estado;
    public bool ResultadoIncierto {get;}=incierto;
}
public sealed class UsuariosGestionApiClient(HttpClient http,ResolucionSesion sesion) : IUsuariosGestionApiClient
{
    public async Task<IReadOnlyList<UsuarioGestion>> Listar(CancellationToken ct) {
        var usuarios=(await Enviar<ListadoUsuariosGestion>(HttpMethod.Get,"api/usuarios",null,200,ct)).Usuarios;
        return usuarios is not null&&usuarios.All(u=>u is not null&&u.Id>0)?usuarios:throw new UsuariosGestionException(503);
    }
    public async Task<IReadOnlyList<RolUsuarioGestion>> Roles(CancellationToken ct) {
        var roles=await Enviar<List<RolUsuarioGestion>>(HttpMethod.Get,"api/usuarios/roles",null,200,ct);
        return roles.All(r=>r is not null&&r.Id>0&&!string.IsNullOrWhiteSpace(r.Nombre))?roles:throw new UsuariosGestionException(503);
    }
    public async Task<int> Crear(AltaUsuarioGestion alta,CancellationToken ct)
    {
        var r=await Enviar<AltaUsuarioGestionResultado>(HttpMethod.Post,"api/usuarios",new { alta.Nombre,alta.Apellido,alta.Correo,alta.IdRol },201,ct);
        return r.Id>0?r.Id:throw new UsuariosGestionException(503,true);
    }
    public async Task Baja(int id,CancellationToken ct) { ValidarId(id); await Enviar<object>(HttpMethod.Delete,$"api/usuarios/{id}",null,204,ct); }
    public async Task Reenviar(int id,CancellationToken ct) { ValidarId(id); if(!(await Enviar<ReenvioUsuarioGestionResultado>(HttpMethod.Post,$"api/usuarios/{id}/reenviar-activacion",null,200,ct)).Reenviado) throw new UsuariosGestionException(503,true); }
    private static void ValidarId(int id) { if(id<=0) throw new UsuariosGestionException(400); }
    private async Task<T> Enviar<T>(HttpMethod metodo,string ruta,object? cuerpo,int esperado,CancellationToken ct)
    {
        var token=sesion.ObtenerToken()??throw new UsuariosGestionException(401);
        var escritura=metodo!=HttpMethod.Get;
        using var req=new HttpRequestMessage(metodo,ruta);
        req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        if(cuerpo is not null) req.Content=JsonContent.Create(cuerpo);
        try {
            using var r=await http.SendAsync(req,ct);
            var codigo=(int)r.StatusCode;
            if(codigo==401) { await sesion.InvalidarAsync(); throw new UsuariosGestionException(401); }
            if(codigo is 400 or 403 or 409) throw new UsuariosGestionException(codigo);
            if(codigo!=esperado) throw new UsuariosGestionException(503,escritura);
            if(esperado==204) return default!;
            if(r.Content.Headers.ContentType?.MediaType!="application/json") throw new UsuariosGestionException(503,escritura);
            return await r.Content.ReadFromJsonAsync<T>(cancellationToken:ct)??throw new UsuariosGestionException(503,escritura);
        }
        catch(HttpRequestException) { throw new UsuariosGestionException(503,escritura); }
        catch(JsonException) { throw new UsuariosGestionException(503,escritura); }
        catch(OperationCanceledException) when(!ct.IsCancellationRequested) { throw new UsuariosGestionException(503,escritura); }
    }
}
