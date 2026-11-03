using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;

namespace Proyecto_Arquitectura_Micromercado.Servicios;

public sealed class UsuariosApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.GetAsync("api/usuarios", cancellationToken);
        return (await LeerAsync<ListarUsuariosRespuesta>(respuesta, cancellationToken)).Usuarios;
    }

    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.GetAsync("api/usuarios/roles", cancellationToken);
        return await LeerAsync<RolDto[]>(respuesta, cancellationToken);
    }

    public async Task<CrearUsuarioRespuesta> CrearAsync(CrearUsuarioDto usuario, CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.PostAsJsonAsync("api/usuarios", usuario, cancellationToken);
        return await LeerAsync<CrearUsuarioRespuesta>(respuesta, cancellationToken);
    }

    public async Task DarDeBajaAsync(int id, CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.DeleteAsync($"api/usuarios/{id}", cancellationToken);
        VerificarRespuesta(respuesta);
    }

    public async Task<ReenviarActivacionRespuesta> ReenviarActivacionAsync(int id, CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.PostAsync($"api/usuarios/{id}/reenviar-activacion", null, cancellationToken);
        return await LeerAsync<ReenviarActivacionRespuesta>(respuesta, cancellationToken);
    }

    public async Task<ActivarCuentaRespuesta> ActivarAsync(ActivarCuentaDto activacion, CancellationToken cancellationToken = default)
    {
        using var respuesta = await httpClient.PostAsJsonAsync("api/activacion", activacion, cancellationToken);
        return await LeerAsync<ActivarCuentaRespuesta>(respuesta, cancellationToken);
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        VerificarRespuesta(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new JsonException("La API devolvió una respuesta vacía.");
    }

    private static void VerificarRespuesta(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
            return;

        var mensaje = respuesta.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "La API requiere autenticación para realizar esta operación.",
            HttpStatusCode.Forbidden => "No tiene permisos para realizar esta operación.",
            HttpStatusCode.BadRequest => "La API rechazó los datos. Revise el formulario o solicite un nuevo enlace si la activación no se puede completar.",
            HttpStatusCode.NotFound => "El recurso solicitado no está disponible.",
            HttpStatusCode.Conflict => "La operación no se puede realizar: el usuario ya existe o su estado actual no lo permite.",
            _ => "No se pudo completar la operación con el servicio de usuarios. Si estaba creando una cuenta, actualice el listado antes de repetir el alta."
        };
        throw new HttpRequestException(mensaje, null, respuesta.StatusCode);
    }
}
