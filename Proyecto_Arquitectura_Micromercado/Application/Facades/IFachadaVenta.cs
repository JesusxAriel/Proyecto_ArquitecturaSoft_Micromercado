using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

// Fachada (patron Facade) del caso de uso "registrar una venta".
//
// POR QUE EXISTE
// Registrar una venta coordina varios colaboradores y un limite transaccional: el
// cliente, el precio vigente del producto, el descuento de stock, el usuario de la
// sesion y la confirmacion o reversion. Si la Razor Page tuviera que conocerlos a todos
// seria el adaptador de entrada haciendo trabajo de aplicacion. Con la fachada la Page
// conoce UNA interfaz y llama UN metodo.
//
// COMO SE RELACIONA CON LAS OTRAS DOS FACHADAS
// Las Pages consumen solo IFachadaVenta e IFachadaAnulacion. Sus implementaciones
// consumen internamente a IFachadaActualizacionStockProducto y lo hacen bajo la MISMA
// IUnidadDeTrabajo, de modo que cabecera, detalle y stock se confirman o se revierten
// juntos.
//
// ESTADO
// La estructura la define la Parte 1 del entregable. La implementacion y las firmas
// finales (incluidos los DTOs de venta) son de la Parte 3; la firma de abajo es la que ya
// existia y puede ajustarse. Esta interfaz todavia no esta registrada en Program.cs:
// se registra junto con su implementacion.
public interface IFachadaVenta
{
    // Registra la venta completa de forma atomica: cabecera, detalle y descuento de
    // stock. Si cualquier paso falla, no se guarda nada.
    Task<ResultadoVentaDto> RegistrarVentaAsync(
        NuevaVentaDto venta,
        CancellationToken cancellationToken = default);
}
