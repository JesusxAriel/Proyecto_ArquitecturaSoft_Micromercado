using Microsoft.Extensions.Options;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

public sealed class ValidarJwtValidacionOpciones : IValidateOptions<JwtValidacionOpciones>
{
    public ValidateOptionsResult Validate(string? name, JwtValidacionOpciones opciones)
    {
        try
        {
            opciones.Validar();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }
}
