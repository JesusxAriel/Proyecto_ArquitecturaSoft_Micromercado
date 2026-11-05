using System.Net.Http.Json;
using System.Text.Json;

namespace PuntoMarket.Ventas.Frontend.Clients;

public interface IActivacionApiClient
{
    Task Activar(string token, string nuevaContrasena, string confirmarContrasena, CancellationToken ct);
}

public sealed class ActivacionApiException(int estado) : Exception(estado == 400
    ? "El enlace de activación no es válido o ha expirado. Solicite uno nuevo al administrador."
    : "No se pudo confirmar la activación. Intente iniciar sesión antes de solicitar un enlace nuevo; no se repitió la operación.")
{
    public int Estado { get; } = estado;
}

// Anónimo: no utiliza ResolucionSesion ni adjunta un JWT. Nunca lee errores remotos sensibles.
public sealed class ActivacionApiClient(HttpClient http) : IActivacionApiClient
{
    public async Task Activar(string token, string nuevaContrasena, string confirmarContrasena, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/activacion") {
            Content = JsonContent.Create(new { token, nuevaContrasena, confirmarContrasena })
        };
        try
        {
            using var response = await http.SendAsync(request, ct);
            if ((int)response.StatusCode == 400) throw new ActivacionApiException(400);
            if ((int)response.StatusCode != 200 || response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new ActivacionApiException(503);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("activada", out var activada) || activada.ValueKind != JsonValueKind.True)
                throw new ActivacionApiException(503);
        }
        catch (HttpRequestException) { throw new ActivacionApiException(503); }
        catch (JsonException) { throw new ActivacionApiException(503); }
        catch (InvalidOperationException) { throw new ActivacionApiException(503); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ActivacionApiException(503); }
    }
}
