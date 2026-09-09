using Proyecto_Arquitectura_Micromercado.Data;
using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Validations
{
    /// <summary>
    /// Reglas de negocio de un proveedor.
    /// Deja de ser static para poder recibir el repositorio por constructor
    /// y asi verificar reglas que dependen de datos ya registrados.
    /// </summary>
    public class ProveedorValidation
    {
        private const int MaxLengthNombreEmpresa = 150;

        private readonly IProveedorRepository proveedorRepository;

        public ProveedorValidation(IProveedorRepository proveedorRepository)
        {
            this.proveedorRepository = proveedorRepository;
        }

        /// <summary>
        /// Devuelve los errores encontrados.
        /// Una lista vacia significa que el proveedor es valido.
        /// </summary>
        public List<string> Validate(Proveedor proveedor)
        {
            var errors = new List<string>();

            ValidateNombreEmpresa(proveedor, errors);
            ValidateNombreEmpresaNotDuplicated(proveedor, errors);
            ValidateNumeroEmpresa(proveedor, errors);
            ValidateCorreoReferencia(proveedor, errors);
            ValidateCorreoRequiredForAutogestionado(proveedor, errors);

            return errors;
        }

        private void ValidateNombreEmpresa(Proveedor proveedor, List<string> errors)
        {
            if (!Validation.IsRequired(proveedor.NombreEmpresa))
            {
                errors.Add("El nombre de la empresa es obligatorio.");
                return;
            }

            if (!Validation.HasValidLength(proveedor.NombreEmpresa, MaxLengthNombreEmpresa))
            {
                errors.Add($"El nombre de la empresa no puede superar los {MaxLengthNombreEmpresa} caracteres.");
            }
        }

        private void ValidateNombreEmpresaNotDuplicated(Proveedor proveedor, List<string> errors)
        {
            if (!Validation.IsRequired(proveedor.NombreEmpresa))
            {
                return;
            }

            if (proveedorRepository.ExistsCompanyName(proveedor.NombreEmpresa, proveedor.Id))
            {
                errors.Add("Ya existe un proveedor registrado con ese nombre de empresa.");
            }
        }

        private void ValidateNumeroEmpresa(Proveedor proveedor, List<string> errors)
        {
            if (!Validation.IsValidPhoneNumber(proveedor.NumeroEmpresa))
            {
                errors.Add("El numero de contacto debe tener entre 6 y 20 digitos, sin espacios ni letras.");
            }
        }

        private void ValidateCorreoReferencia(Proveedor proveedor, List<string> errors)
        {
            if (!Validation.IsValidEmail(proveedor.CorreoReferencia))
            {
                errors.Add("El correo de referencia no tiene un formato valido (ej. nombre@dominio.com).");
            }
        }

        private void ValidateCorreoRequiredForAutogestionado(Proveedor proveedor, List<string> errors)
        {
            if (proveedor.EsAutogestionado && !Validation.IsRequired(proveedor.CorreoReferencia))
            {
                errors.Add("Un proveedor autogestionado necesita un correo de referencia para coordinar las reposiciones.");
            }
        }
    }
}