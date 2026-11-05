using System.ComponentModel.DataAnnotations;
using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Application.Security;

namespace PuntoMarket.Ventas.Application.Customers;

public sealed class ClienteCreacionDto
{
    [Required(ErrorMessage = "Ingrese el CI/NIT.")]
    [StringLength(30, ErrorMessage = "El CI/NIT admite hasta 30 caracteres.")]
    public string NitCi { get; init; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la razón social.")]
    [StringLength(150, ErrorMessage = "La razón social admite hasta 150 caracteres.")]
    public string RazonSocial { get; init; } = string.Empty;
}

public sealed class ClienteDuplicadoException : Exception
{
    public ClienteDuplicadoException() : base("Ya existe un cliente activo con ese CI/NIT.") { }
}

public interface IClienteCreacionRepository
{
    Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion,
        CancellationToken cancellationToken = default);
}

public interface IClienteCreacionService
{
    Task<ClienteConsultaDto> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default);
}

public sealed class ClienteCreacionService(
    IClienteCreacionRepository repositorio,
    IUnidadDeTrabajo unidad,
    IUsuarioActual usuarioActual) : IClienteCreacionService
{
    public async Task<ClienteConsultaDto> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        cancellationToken.ThrowIfCancellationRequested();
        var nitCi = cliente.NitCi?.Trim() ?? string.Empty;
        var razonSocial = cliente.RazonSocial?.Trim() ?? string.Empty;
        var normalizado = new ClienteCreacionDto { NitCi = nitCi, RazonSocial = razonSocial };
        Validator.ValidateObject(normalizado, new ValidationContext(normalizado), validateAllProperties: true);

        await unidad.IniciarAsync(cancellationToken);
        try
        {
            var id = await repositorio.CrearAsync(nitCi, razonSocial, usuarioActual.Id, cancellationToken);
            await unidad.ConfirmarAsync(cancellationToken);
            return new ClienteConsultaDto(id, nitCi, razonSocial);
        }
        catch
        {
            await unidad.RevertirAsync(CancellationToken.None);
            throw;
        }
    }
}
