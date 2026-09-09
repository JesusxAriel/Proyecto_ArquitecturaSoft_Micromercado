using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Data
{
    public interface IProveedorRepository
    {
        List<Proveedor> GetActive();
        Proveedor? GetById(int id);
        void Add(Proveedor proveedor);
        void Update(Proveedor proveedor);
        void Delete(int id);
    }
}