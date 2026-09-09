using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Repositories
{
    public interface ICategoriaRepository
    {
        List<Categoria> ObtenerActivas();
        Categoria? ObtenerPorId(int id);
        bool Actualizar(Categoria categoria);
        bool Desactivar(int id, int idUsuarioAdmin);
    }
}