using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Clients;

public interface IUsuariosCuentaApiClient
{
    Task CambiarPassword(string contrasenaActual, string nuevaContrasena, CancellationToken ct);
}

public sealed class UsuariosCuentaException(int estado, string mensaje) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}

public sealed class UsuariosCuentaApiClient(HttpClient http, ResolucionSesion sesion) : IUsuariosCuentaApiClient
{
    private const string MensajeGenerico = "No se pudo cambiar la contraseña. Revise los datos ingresados.";
    private const string MensajeNoDisponible = "El servicio de usuarios no está disponible. Intente nuevamente más tarde.";
    private static readonly string[] MensajesConocidos =
    [
        "La contraseña actual es incorrecta.",
        "La contraseña actual es obligatoria.",
        "La nueva contraseña no cumple con los requisitos de seguridad.",
        "La nueva contraseña no puede ser igual a la contraseña actual.",
        "La cuenta no está activa."
    ];

    public async Task CambiarPassword(string contrasenaActual, string nuevaContrasena, CancellationToken ct)
    {
        var token = sesion.ObtenerToken() ?? throw new UsuariosCuentaException(401, "La sesión venció o no es válida. Inicie sesión nuevamente.");
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "api/autenticacion/cambiar-password")
        {
            Content = JsonContent.Create(new { contrasenaActual, nuevaContrasena })
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(solicitud, ct);
            await Validar(respuesta, ct);
        }
        catch (HttpRequestException) { throw new UsuariosCuentaException(503, MensajeNoDisponible); }
        catch (JsonException) { throw new UsuariosCuentaException(503, MensajeNoDisponible); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new UsuariosCuentaException(503, MensajeNoDisponible); }
    }

    private async Task Validar(HttpResponseMessage respuesta, CancellationToken ct)
    {
        var codigo = (int)respuesta.StatusCode;
        if (codigo == 401)
        {
            await sesion.InvalidarAsync();
            throw new UsuariosCuentaException(401, "La sesión venció o no es válida. Inicie sesión nuevamente.");
        }

        if (codigo is 400 or 403) throw new UsuariosCuentaException(codigo, await MensajeDeLaApi(respuesta, ct));
        if (codigo != 200 || respuesta.Content.Headers.ContentType?.MediaType != "application/json") throw new UsuariosCuentaException(503, MensajeNoDisponible);

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
        var cambiada = json.RootElement.ValueKind == JsonValueKind.Object &&
            json.RootElement.TryGetProperty("cambiada", out var valor) && valor.ValueKind == JsonValueKind.True;
        if (!cambiada) throw new UsuariosCuentaException(503, MensajeNoDisponible);
    }

    private static async Task<string> MensajeDeLaApi(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            var titulo = json.RootElement.TryGetProperty("title", out var valor) ? valor.GetString() : null;
            return titulo is not null && MensajesConocidos.Contains(titulo) ? titulo : MensajeGenerico;
        }
        catch (JsonException) { return MensajeGenerico; }
        catch (InvalidOperationException) { return MensajeGenerico; }
    }
}
