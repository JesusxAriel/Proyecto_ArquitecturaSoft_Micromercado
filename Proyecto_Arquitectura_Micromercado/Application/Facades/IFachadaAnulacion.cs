namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

// Fachada (patron Facade) del caso de uso "anular una venta".
//
// Anular es una baja logica: marca la venta como anulada y devuelve al stock las
// unidades de cada linea, todo de forma atomica. La venta conserva sus datos y su
// detalle; no se borra nada fisicamente.
//
// Igual que IFachadaVenta, su implementacion consume internamente a
// IFachadaActualizacionStockProducto bajo la misma IUnidadDeTrabajo.
//
// ESTADO
// La estructura la define la Parte 1 del entregable. La implementacion y las firmas
// finales son de la Parte 3; la firma de abajo es la que ya existia y puede ajustarse
// (por ejemplo para recibir el motivo de la anulacion). Todavia no esta registrada en
// Program.cs: se registra junto con su implementacion.
public interface IFachadaAnulacion
{
    // Devuelve false si la venta no existe o ya estaba anulada.
    Task<bool> AnularVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
