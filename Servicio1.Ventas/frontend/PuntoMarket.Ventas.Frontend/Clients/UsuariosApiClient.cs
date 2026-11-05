using System.Net.Http.Json;
using System.Text.Json;
namespace PuntoMarket.Ventas.Frontend.Clients;

public interface IUsuariosApiClient
{
    Task<string> IniciarSesion(string correo, string contrasena, CancellationToken ct);
}
public sealed class UsuariosApiException(int estado) : Exception(estado switch {
    400 => "Ingrese un correo y una contraseña válidos.",
    401 => "Correo o contraseña incorrectos.",
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
}
