using Proyecto_Arquitectura_Micromercado.Models;

namespace Proyecto_Arquitectura_Micromercado.Data
{
    public interface IProveedorRepository
    {
        List<Proveedor> GetActive();
        Proveedor? GetById(int id);
        bool ExistsCompanyName(string nombreEmpresa, int idExcluido);
        void Add(Proveedor proveedor);
        void Update(Proveedor proveedor);
        void Delete(int id);
    }
}
