namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

// Deriva de ArgumentException para que la página lo muestre como error de validación.
public sealed class DuplicateProductException : ArgumentException
{
    public DuplicateProductException(Exception? innerException = null)
        : base(ProductValidation.DuplicateMessage, innerException)
    {
    }
}
