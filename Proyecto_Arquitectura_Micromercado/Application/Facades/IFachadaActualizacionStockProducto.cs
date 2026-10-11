namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

// Fachada (patron Facade) interna que concentra los cambios de stock de un producto.
//
// A diferencia de las otras dos, las Razor Pages NO la consumen: la usan por dentro
// IFachadaVenta (para descontar) e IFachadaAnulacion (para restituir). Asi la regla de
// como se mueve el stock vive en un solo lugar y las dos operaciones comparten la misma
// IUnidadDeTrabajo.
//
// Nota: el stock no es una columna de PRODUCTO sino que se lleva por lote
// (LOTE.cantidadDisponible, con salida FEFO). Esta decision esta aprobada por el magister.
//
// ESTADO
// Interfaz vacia a proposito. Los metodos, sus parametros y la implementacion son de la
// Parte 3 del entregable. Todavia no esta registrada en Program.cs.
public interface IFachadaActualizacionStockProducto
{
}
