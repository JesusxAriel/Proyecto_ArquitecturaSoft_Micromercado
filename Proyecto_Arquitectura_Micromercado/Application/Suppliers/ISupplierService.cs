using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Application.Suppliers;

public interface ISupplierService :
    IServicioCRUD<Supplier, int, Supplier, Supplier>,
    IConListado,
    IConBusqueda
{
}