namespace Proyecto_Arquitectura_Micromercado.Domain.Categories
{
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
