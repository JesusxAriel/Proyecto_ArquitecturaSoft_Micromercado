using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace PuntoMarket.Ventas.Frontend.Clients;
public sealed record UsuarioGestion(int Id, string Nombre, string Apellido, string Correo, string Username, string Rol, string Estado, DateTime FechaCreacion, DateTime? TokenActivacionExpira, DateTime? FechaActivacion, bool EstaExpirado)
{
    // La API presenta como Expirado un PENDIENTE cuyo enlace venció; no es un estado SQL nuevo.
    [JsonIgnore] public bool EsPendiente => Estado == "PENDIENTE" || (Estado == "Expirado" && EstaExpirado);
    [JsonIgnore] public string EstadoVisible => EsPendiente && EstaExpirado ? "Vencido" : Estado switch
    {
        "PENDIENTE" => "Pendiente",
        "ACTIVO" => "Activo",
        "INACTIVO" => "Inactivo",
        _ => Estado
    };
}
public sealed record ListadoUsuariosGestion(IReadOnlyList<UsuarioGestion> Usuarios);
public sealed record RolUsuarioGestion(int Id, string Nombre);
public sealed record AltaUsuarioGestionResultado(int Id);
public sealed record ReenvioUsuarioGestionResultado(bool Reenviado);
public sealed class AltaUsuarioGestion : IValidatableObject
{
    [Required(ErrorMessage="Ingrese el nombre.")][StringLength(150)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage="Ingrese el apellido.")][StringLength(150)] public string Apellido { get; set; } = "";
    [Required(ErrorMessage="Ingrese el correo.")][EmailAddress(ErrorMessage="Ingrese un correo válido.")][StringLength(150)] public string Correo { get; set; } = "";
    [Range(1,int.MaxValue,ErrorMessage="Seleccione un rol activo.")] public int IdRol { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var nombre=Nombre??""; var apellido=Apellido??"";
        if(nombre.Trim().Length+apellido.Trim().Length+1>150 || nombre.Any(char.IsControl)||apellido.Any(char.IsControl))
            yield return new ValidationResult("Nombre y apellido deben sumar hasta 150 caracteres válidos.",[nameof(Nombre),nameof(Apellido)]);
    }
}
