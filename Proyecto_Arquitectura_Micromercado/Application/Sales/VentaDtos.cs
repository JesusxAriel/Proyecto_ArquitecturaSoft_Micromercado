namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

// Contratos de datos de la fachada de venta.
//
// Son records propios de Application, NO entidades de Domain. Es deliberado: en los
// modulos existentes la entidad de dominio se bindea directo del formulario HTTP, lo
// que acopla el nucleo a la presentacion. El modulo de ventas arranca sin ese problema.

// Lo que la UI envia al registrar una venta.
//
// No incluye el total ni los precios: los pone la fachada leyendolos de la base. Si el
// precio viniera del navegador seria manipulable por el cliente.
public sealed record NuevaVentaDto(
    int IdCliente,
    IReadOnlyList<LineaVentaDto> Lineas);

// Una linea del detalle tal como la arma la UI: que producto y cuantas unidades.
public sealed record LineaVentaDto(
    int IdProducto,
    int Cantidad);

// Lo que la fachada devuelve: el identificador para abrir el recibo y el total ya
// calculado del lado del servidor.
public sealed record ResultadoVentaDto(
    int IdVenta,
    decimal Total);
