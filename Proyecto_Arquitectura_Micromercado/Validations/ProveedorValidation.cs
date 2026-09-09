using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Validations
{
    public static class ProveedorValidation
    {
        public static List<string> Validate(Proveedor proveedor)
        {
            var errors = new List<string>();

            if (!Validation.IsRequired(proveedor.NombreEmpresa))
            {
                errors.Add("El nombre de la empresa es obligatorio.");
            }
            else if (!Validation.HasValidLength(proveedor.NombreEmpresa, 150))
            {
                errors.Add("El nombre de la empresa no puede superar los 150 caracteres.");
            }

            if (!Validation.IsValidPhoneNumber(proveedor.NumeroEmpresa))
            {
                errors.Add("El número de contacto debe tener entre 6 y 20 dígitos, sin espacios ni letras.");
            }

            if (!Validation.IsValidEmail(proveedor.CorreoReferencia))
            {
                errors.Add("El correo de referencia no tiene un formato válido (ej. nombre@dominio.com).");
            }

            return errors;
        }
    }
}