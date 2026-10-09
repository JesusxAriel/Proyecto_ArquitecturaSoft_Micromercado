namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

// Fachada (patron Facade) del caso de uso de venta.
//
// POR QUE EXISTE
// Registrar una venta no es una operacion sobre una sola entidad: coordina seis
// colaboradores y un limite transaccional.
//
//     RegistrarVentaAsync
//        |- servicio de cliente      -> busca el cliente o lo crea desde el modal
//        |- servicio de producto     -> precio vigente y validacion de stock
//        |- IUnidadDeTrabajo         -> abre la transaccion  ---------+
//        |- repositorio de venta     -> 1 INSERT                      |  todo dentro
//        |- repositorio de detalle   -> N INSERT                      |  de la misma
//        |- repositorio de producto  -> N UPDATE de stock             |  transaccion
//        |- usuario de sesion        -> estampa la auditoria          |
//        +- confirmar / revertir  -------------------------------------+
//
// Sin la fachada, el PageModel tendria que conocer esos seis colaboradores y ademas
// ordenar la transaccion: seria el adaptador de entrada haciendo trabajo de
// aplicacion. Con la fachada, la Page conoce UNA interfaz y llama UN metodo por caso
// de uso.
//
// POR QUE SOLO TIENE DOS METODOS
// Una fachada que se limita a reenviar a un servicio no es una fachada, es un
// envoltorio sin valor. Por eso aca solo estan los dos casos que orquestan de verdad.
// El listado de ventas y el detalle de una venta son consultas de una sola
// responsabilidad: van en un IVentaService normal, no aca.
//
// ESTADO
// Este archivo define unicamente el contrato. La implementacion (VentaFacade) depende
// de los repositorios de venta, detalle y cliente, que pertenecen a las Partes 2 y 3
// del entregable y todavia no existen.
public interface IVentaFacade
{
    // Registra la venta completa de forma atomica: cabecera, detalle y descuento de
    // stock. Si cualquier paso falla, no se guarda nada.
    //
    // Devuelve el identificador de la venta y el total calculado en el servidor.
    // Lanza ArgumentException si el carrito viene vacio o si algun producto no tiene
    // stock suficiente, de modo que la Page lo muestre como error de validacion
    // igual que en el resto del sistema.
    Task<ResultadoVentaDto> RegistrarVentaAsync(
        NuevaVentaDto venta,
        CancellationToken cancellationToken = default);

    // Anulacion logica: marca la venta como anulada y devuelve al stock las unidades
    // de cada linea, tambien de forma atomica. La venta conserva sus datos y su
    // detalle; no se borra nada fisicamente.
    //
    // Devuelve false si la venta no existe o ya estaba anulada.
    Task<bool> AnularVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
