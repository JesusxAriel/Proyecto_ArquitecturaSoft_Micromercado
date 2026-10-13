using System.ComponentModel.DataAnnotations;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public sealed class ClienteCreacionService(
    IClienteCreacionRepository repositorio, IUsuarioActual usuarioActual) : IClienteCreacionService
{
    public Task<int> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        cancellationToken.ThrowIfCancellationRequested();
        var normalizado = new ClienteCreacionDto
        {
            NitCi = cliente.NitCi?.Trim() ?? string.Empty,
            RazonSocial = cliente.RazonSocial?.Trim() ?? string.Empty
        };
        Validator.ValidateObject(normalizado, new ValidationContext(normalizado), validateAllProperties: true);
        return repositorio.CrearAsync(normalizado.NitCi, normalizado.RazonSocial,
            usuarioActual.Id, cancellationToken);
    }
}
