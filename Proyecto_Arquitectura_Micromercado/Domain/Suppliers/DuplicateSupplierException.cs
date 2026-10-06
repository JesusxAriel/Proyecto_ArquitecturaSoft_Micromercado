namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

// Deriva de ArgumentException para que la página lo muestre como error de validación.
public sealed class DuplicateSupplierException : ArgumentException
{
    public DuplicateSupplierException(Exception? innerException = null)
        : base(SupplierValidation.NombreDuplicadoMessage, innerException)
    {
    }
}
