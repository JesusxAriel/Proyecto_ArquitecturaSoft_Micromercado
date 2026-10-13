namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public sealed class DuplicateSupplierException : ArgumentException
{
    public DuplicateSupplierException(Exception? innerException = null)
        : base(SupplierValidation.NombreDuplicadoMessage, innerException)
    {
    }
}
