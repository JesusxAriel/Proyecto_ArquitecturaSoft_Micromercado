using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Data
{
    public interface IProveedorRepository
    {
        List<Proveedor> ObtenerActivos();
        Proveedor? ObtenerPorId(int id);
        void Insertar(Proveedor proveedor);
        void Actualizar(Proveedor proveedor);
        void Eliminar(int id);
    }
}