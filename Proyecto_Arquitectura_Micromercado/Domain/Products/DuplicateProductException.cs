namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public sealed class DuplicateProductException : ArgumentException
{
    public DuplicateProductException(Exception? innerException = null)
        : base(ProductValidation.DuplicateMessage, innerException)
    {
    }
}
