using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Validations
{
    public class CategoriaValidation
    {
        public bool EsNombreValido(string nombre)
        {
            return !string.IsNullOrWhiteSpace(nombre)
                   && nombre.Length <= 150;
        }

        public bool EsCodigoValido(string codigo)
        {
            return !string.IsNullOrWhiteSpace(codigo)
                   && codigo.Length <= 20;
        }

        public bool EsDescripcionValida(string? descripcion)
        {
            return string.IsNullOrWhiteSpace(descripcion)
                   || descripcion.Length <= 255;
        }

        public bool EsPasilloValido(string? pasilloUbicacion)
        {
            return string.IsNullOrWhiteSpace(pasilloUbicacion)
                   || pasilloUbicacion.Length <= 20;
        }

        public bool EsCategoriaValida(Categoria categoria)
        {
            return EsNombreValido(categoria.Nombre)
                   && EsCodigoValido(categoria.Codigo)
                   && EsDescripcionValida(categoria.Descripcion)
                   && EsPasilloValido(categoria.PasilloUbicacion);
        }
    }
}
