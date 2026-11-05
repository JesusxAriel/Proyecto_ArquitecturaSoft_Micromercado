using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.ListadoVentas;

public static class TextosDelListadoVentas
{
    public const string Titulo = "Listado de ventas";

    public static string Estado(EstadoDeVenta estado) =>
        estado == EstadoDeVenta.Completada ? "Completada" : "Anulada";

    public static string EstadoFiltrado(FiltrosDeListadoVentas filtros) =>
        filtros.Estado is { } estado ? Estado(estado) : "Todos los estados";

    public static string ClienteFiltrado(FiltrosDeListadoVentas filtros) =>
        filtros.Cliente ?? "Todos los clientes";

    public static string Limite(FiltrosDeListadoVentas filtros) =>
        filtros.Tope is { } tope ? $"Las {tope} ventas más recientes" : "Sin límite";
}
