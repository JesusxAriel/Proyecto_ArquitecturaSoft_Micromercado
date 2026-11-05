using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Application.Stock;
using PuntoMarket.Ventas.Infrastructure.Database;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

/// <summary>
/// Agrupa los adaptadores de una operación sobre la misma unidad de trabajo.
/// Crear una instancia por operación y disponerla con await using; no compartir entre solicitudes.
/// </summary>
public sealed class PersistenciaVentasMySql : IAsyncDisposable
{
    public MySqlUnidadDeTrabajo Unidad { get; }
    public IVentaRepository Ventas { get; }
    public ILoteRepository Lotes { get; }
    public IConPrecioVigente Precios { get; }
    public IClienteActivoConsulta Clientes { get; }
    public IVentaConsultaRepository Consultas { get; }
    public IProductoVentaConsultaRepository ProductosConsulta { get; }
    public IClienteVentaConsultaRepository ClientesConsulta { get; }
    public IClienteCreacionRepository ClientesCreacion { get; }
    public IComprobanteRepository Comprobantes { get; }

    public PersistenciaVentasMySql(DatabaseConnection conexiones)
    {
        ArgumentNullException.ThrowIfNull(conexiones);
        Unidad = new MySqlUnidadDeTrabajo(conexiones);
        Ventas = new MySqlVentaRepository(Unidad);
        Lotes = new MySqlLoteRepository(Unidad);
        Precios = new MySqlPrecioVigenteRepository(Unidad);
        Clientes = new MySqlClienteActivoConsulta(Unidad);
        Consultas = new MySqlVentaConsultaRepository(Unidad);
        ProductosConsulta = new MySqlProductoVentaConsultaRepository(Unidad);
        ClientesConsulta = new MySqlClienteVentaConsultaRepository(Unidad);
        ClientesCreacion = new MySqlClienteCreacionRepository(Unidad);
        Comprobantes = new MySqlComprobanteRepository(Unidad);
    }

    public ValueTask DisposeAsync() => Unidad.DisposeAsync();
}
