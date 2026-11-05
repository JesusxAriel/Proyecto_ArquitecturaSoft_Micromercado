using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;

namespace PuntoMarket.Ventas.Tests;

// Datos controlados exclusivamente de pruebas; los servicios reales consumen estos puertos de lectura.
internal sealed class ConsultasComplementariasDoble : IProductoVentaConsultaRepository,
    IClienteVentaConsultaRepository, IComprobanteRepository
{
    public List<(ProductoParaVenta Producto, bool Activo)> Productos { get; } =
    [
        (new(1, "Leche", "Bolsa 1L", 10.50m, 8), true),
        (new(2, "Leche inactiva", "Bolsa", 11m, 20), false),
        (new(3, "Arroz", "Paquete", 5m, 0), true)
    ];
    public List<(ClienteConsultaDto Cliente, bool Activo)> Clientes { get; } =
    [
        (new(3, "12345", "Cliente Uno"), true),
        (new(4, "12346", "Cliente inactivo"), false)
    ];
    public VentaParaComprobante? Venta { get; set; } = new(7, "V-20260615-0007", new DateTime(2026, 6, 15, 10, 0, 0),
        "12345", "<script>cliente</script>", "COMPLETADA", 31.50m, "cajero-origen",
        [new(1, "<b>Leche</b>", 1, 10.50m, 10.50m), new(1, "<b>Leche</b>", 2, 10.50m, 21m)]);
    public int Lecturas { get; private set; }
    public string? Termino { get; private set; }
    public int Limite { get; private set; }
    public DateOnly Hoy { get; private set; }
    public CancellationToken Token { get; private set; }

    private void Leer(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Token = token;
        Lecturas++;
    }

    public Task<ResultadoBusquedaParaVenta> BuscarAsync(string termino, int limite, DateOnly hoy, CancellationToken cancellationToken = default)
    {
        Leer(cancellationToken); Termino = termino; Limite = limite; Hoy = hoy;
        var productos = Productos.Where(p => p.Activo && (p.Producto.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase)
            || p.Producto.Presentacion.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Producto).OrderBy(p => p.Nombre).ThenBy(p => p.Id).ToArray();
        return Task.FromResult(new ResultadoBusquedaParaVenta(productos.Take(limite).ToArray(), productos.Length > limite));
    }
    public Task<ProductoParaVenta?> ObtenerAsync(int id, DateOnly hoy, CancellationToken cancellationToken = default)
    {
        Leer(cancellationToken); Hoy = hoy;
        return Task.FromResult(Productos.Where(p => p.Activo).Select(p => p.Producto).SingleOrDefault(p => p.Id == id));
    }
    public Task<IReadOnlyList<ClienteConsultaDto>> BuscarAsync(string termino, int limite, CancellationToken cancellationToken = default)
    {
        Leer(cancellationToken); Termino = termino; Limite = limite;
        return Task.FromResult<IReadOnlyList<ClienteConsultaDto>>(Clientes.Where(c => c.Activo &&
            (c.Cliente.NitCi.Contains(termino, StringComparison.OrdinalIgnoreCase) || c.Cliente.RazonSocial.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(c => c.Cliente).OrderBy(c => c.RazonSocial).ThenBy(c => c.Id).Take(limite).ToArray());
    }
    public Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        Leer(cancellationToken);
        return Task.FromResult(Venta?.IdVenta == idVenta ? Venta : null);
    }
}
