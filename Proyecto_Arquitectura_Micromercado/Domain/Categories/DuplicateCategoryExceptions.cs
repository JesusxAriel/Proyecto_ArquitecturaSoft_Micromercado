namespace Proyecto_Arquitectura_Micromercado.Domain.Categories
{
    // Derivan de ArgumentException para que las páginas muestren el mensaje como error de validación.
    public sealed class DuplicateCategoryCodeException : ArgumentException
    {
        public DuplicateCategoryCodeException(Exception? innerException = null)
            : base(CategoryValidation.CodeDuplicateMessage, innerException)
        {
        }
    }

    public sealed class DuplicateCategoryNameException : ArgumentException
    {
        public DuplicateCategoryNameException(Exception? innerException = null)
            : base(CategoryValidation.NameDuplicateMessage, innerException)
        {
        }
    }
}
