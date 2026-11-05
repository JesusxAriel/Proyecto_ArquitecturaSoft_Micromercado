using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
namespace PuntoMarket.Ventas.Frontend.Clients;

public interface IUsuariosApiClient
{
    Task<string> IniciarSesion(string correo, string contrasena, CancellationToken ct);
    Task CambiarPassword(string token, string contrasenaActual, string nuevaContrasena, CancellationToken ct);
}
public sealed class UsuariosApiException(int estado, string? mensaje = null) : Exception(mensaje ?? estado switch {
    400 => "Ingrese un correo y una contraseña válidos.",
    401 => "Correo o contraseña incorrectos.",
    403 => "La cuenta no está activa.",
    _ => "Usuarios no está disponible. Intente nuevamente más tarde."
})
{
    public int Estado { get; } = estado;
}
public sealed class UsuariosApiClient(HttpClient http) : IUsuariosApiClient
{
    public async Task<string> IniciarSesion(string correo, string contrasena, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena)) throw new UsuariosApiException(400);
        // Contraseña solo en el cuerpo POST; ninguna credencial se conserva como estado del cliente.
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "api/autenticacion/login") {
            Content = JsonContent.Create(new { correo, contrasena })
        };
        try
        {
            using var respuesta = await http.SendAsync(peticion, ct);
            if ((int)respuesta.StatusCode != 200) throw new UsuariosApiException((int)respuesta.StatusCode is 400 or 401 ? (int)respuesta.StatusCode : 503);
            if (respuesta.Content.Headers.ContentType?.MediaType != "application/json") throw new UsuariosApiException(503);
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("token", out var token) || token.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(token.GetString()) || token.GetString()!.Length > 16384)
                throw new UsuariosApiException(503);
            return token.GetString()!; // El consumidor deberá validarlo antes de crear una sesión.
        }
        catch (HttpRequestException) { throw new UsuariosApiException(503); }
        catch (JsonException) { throw new UsuariosApiException(503); }
        catch (InvalidOperationException) { throw new UsuariosApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new UsuariosApiException(503); }
    }

    public async Task CambiarPassword(
        string token,
        string contrasenaActual,
        string nuevaContrasena,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UsuariosApiException(401, "La sesión no es válida. Inicie sesión nuevamente.");
        if (string.IsNullOrWhiteSpace(contrasenaActual) || string.IsNullOrWhiteSpace(nuevaContrasena))
            throw new UsuariosApiException(400, "Ingrese la contraseña actual y una nueva contraseña.");

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "api/autenticacion/cambiar-password")
        {
            Content = JsonContent.Create(new { contrasenaActual, nuevaContrasena })
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var respuesta = await http.SendAsync(peticion, ct);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UsuariosApiException(401, "La sesión venció. Inicie sesión nuevamente.");
            if (respuesta.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UsuariosApiException(403);
            if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                throw new UsuariosApiException(400, await LeerTituloProblema(respuesta, ct));
            if (!respuesta.IsSuccessStatusCode)
                throw new UsuariosApiException(503);
            if (respuesta.Content.Headers.ContentType?.MediaType != "application/json")
                throw new UsuariosApiException(503);

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("cambiada", out var cambiada) ||
                cambiada.ValueKind != JsonValueKind.True)
                throw new UsuariosApiException(503);
        }
        catch (HttpRequestException) { throw new UsuariosApiException(503); }
        catch (JsonException) { throw new UsuariosApiException(503); }
        catch (InvalidOperationException) { throw new UsuariosApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new UsuariosApiException(503); }
    }

    private static async Task<string> LeerTituloProblema(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.Content.Headers.ContentType?.MediaType != "application/problem+json")
            return "La nueva contraseña no cumple los requisitos o la contraseña actual es incorrecta.";
        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (json.RootElement.TryGetProperty("title", out var titulo) &&
                titulo.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(titulo.GetString()) &&
                titulo.GetString()!.Length <= 300)
                return titulo.GetString()!;
        }
        catch (JsonException) { }
        return "La nueva contraseña no cumple los requisitos o la contraseña actual es incorrecta.";
    }
}
