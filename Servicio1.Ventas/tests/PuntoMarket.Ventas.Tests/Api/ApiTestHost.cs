using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Ventas.Api;
using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Application.Stock;
using PuntoMarket.Ventas.Domain.Sales;
using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Tests.Api;

internal sealed class ApiTestHost : IAsyncDisposable
{
    public string Key { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public const string Issuer = "usuarios-tests";
    public const string Audience = "ventas-tests";
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public EstadoHttp Estado { get; } = new();

    public static async Task<ApiTestHost> CrearAsync(bool puertosDePrueba = true, bool jwtConfigurado = true,
        Action<IServiceCollection>? configurarServicios = null)
    {
        var host = new ApiTestHost();
        host.App = ApiBootstrap.Crear(["--urls", "http://127.0.0.1:0"], builder =>
        {
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = jwtConfigurado ? host.Key : null,
                ["Jwt:Issuer"] = jwtConfigurado ? Issuer : null,
                ["Jwt:Audience"] = jwtConfigurado ? Audience : null,
                ["ConnectionStrings:Ventas"] = null
            });
            if (!puertosDePrueba) return;
            builder.Services.AddSingleton(host.Estado);
            builder.Services.AddScoped<PuertosHttp>();
            Reemplazar<IUnidadDeTrabajo>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            Reemplazar<IVentaRepository>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            Reemplazar<ILoteRepository>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            Reemplazar<IConPrecioVigente>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            Reemplazar<IClienteActivoConsulta>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            Reemplazar<IVentaConsultaRepository>(builder.Services, sp => sp.GetRequiredService<PuertosHttp>());
            configurarServicios?.Invoke(builder.Services);
        });
        await host.App.StartAsync();
        var direccion = host.App.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        host.Client = new HttpClient { BaseAddress = new Uri(direccion) };
        return host;
    }

    private static void Reemplazar<T>(IServiceCollection servicios, Func<IServiceProvider, T> factory) where T : class
    {
        servicios.RemoveAll<T>();
        servicios.AddScoped(factory);
    }

    // Emisión exclusivamente de pruebas: claves efímeras, nunca compartidas con producción.
    public string Token(string rol = "Cajero", string sub = "7", string? key = null,
        string issuer = Issuer, string audience = Audience, bool expirado = false,
        string algoritmo = SecurityAlgorithms.HmacSha256, bool debeCambiar = false,
        bool sinExpiracion = false, bool sinIndicador = false)
    {
        var claims = new List<Claim>
        {
            new("sub", sub), new("unique_name", "cajero.test"),
            new("nombre", "Cajero de prueba"), new("role", rol)
        };
        if (!sinIndicador) claims.Add(new("debeCambiarPassword", debeCambiar ? "true" : "false", ClaimValueTypes.Boolean));
        var token = new JwtSecurityToken(issuer, audience, claims,
            notBefore: DateTime.UtcNow.AddMinutes(-20),
            expires: sinExpiracion ? null : expirado ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? Key)), algoritmo));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Autenticar(string rol = "Cajero", string sub = "7") =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(rol, sub));

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
    }
}

internal sealed class EstadoHttp
{
    public object Sync { get; } = new();
    public VentaRepositoryEnMemoria Ventas { get; } = new();
    public InMemoryLoteRepository Lotes { get; } = new();
    public ConcurrentBag<(Guid Sesion, string Operacion)> Operaciones { get; } = [];
    public ConcurrentBag<TaskCompletionSource> Disposiciones { get; } = [];
    public Exception? Fallo { get; set; }
    public bool? IncluirAnuladas { get; set; }
    public int? Limite { get; set; }

    public EstadoHttp() => Lotes.Agregar(1, new LoteDisponible(10, 1000, null), cantidadInicial: 1000);
}

// Dobles controlados solo del borde de persistencia. Endpoints, JWT, políticas y fachadas son reales.
internal sealed class PuertosHttp : IUnidadDeTrabajo, IVentaRepository, ILoteRepository,
    IConPrecioVigente, IClienteActivoConsulta, IVentaConsultaRepository, IAsyncDisposable
{
    private readonly EstadoHttp estado;
    private readonly Guid sesion = Guid.NewGuid();
    private readonly TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PuertosHttp(EstadoHttp estado)
    {
        this.estado = estado;
        estado.Disposiciones.Add(disposed);
    }

    private void Registrar(string operacion)
    {
        estado.Operaciones.Add((sesion, operacion));
        if (estado.Fallo is not null) throw estado.Fallo;
    }

    public Task IniciarAsync(CancellationToken cancellationToken = default) { Registrar("iniciar"); return Task.CompletedTask; }
    public Task ConfirmarAsync(CancellationToken cancellationToken = default) { Registrar("confirmar"); return Task.CompletedTask; }
    public Task RevertirAsync(CancellationToken cancellationToken = default) { estado.Operaciones.Add((sesion, "revertir")); return Task.CompletedTask; }

    public Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default)
    {
        Registrar("cabecera");
        lock (estado.Sync) return estado.Ventas.InsertarCabeceraAsync(idCliente, idUsuario, cancellationToken);
    }
    public Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default)
    {
        Registrar("detalle");
        lock (estado.Sync) return estado.Ventas.InsertarDetalleAsync(idVenta, idProducto, idLote, cantidad, precioUnitario, cancellationToken);
    }
    public Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, CancellationToken cancellationToken = default)
    {
        Registrar("cerrar");
        lock (estado.Sync) return estado.Ventas.CerrarVentaAsync(idVenta, nroComprobante, montoTotal, cancellationToken);
    }
    public Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        Registrar("leer-anulacion");
        lock (estado.Sync) return estado.Ventas.ObtenerParaAnularAsync(idVenta, cancellationToken);
    }
    public Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default)
    {
        Registrar("anular");
        lock (estado.Sync) return estado.Ventas.MarcarAnuladaAsync(idVenta, motivo, idUsuarioAnulacion, cancellationToken);
    }
    public Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default) => ObtenerParaAnularAsync(idVenta, cancellationToken);
    public Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default)
    {
        Registrar("total-dia");
        lock (estado.Sync) return Task.FromResult(estado.Ventas.Cabeceras.Where(c => c.Estado == EstadoVenta.Completada).Sum(c => c.MontoTotal));
    }
    public Task<bool> ExisteActivoAsync(int idCliente, CancellationToken cancellationToken = default)
    { Registrar("cliente"); return Task.FromResult(idCliente == 3); }
    public Task<decimal?> ObtenerPrecioVigenteAsync(int idProducto, CancellationToken cancellationToken = default)
    { Registrar("precio"); return Task.FromResult<decimal?>(idProducto == 1 ? 10m : null); }
    public Task<DescripcionDeProducto?> ObtenerDescripcionAsync(int idProducto, CancellationToken cancellationToken = default) =>
        Task.FromResult<DescripcionDeProducto?>(idProducto == 1 ? new("Producto de prueba", "Unidad") : null);
    public Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(int idProducto, CancellationToken cancellationToken = default)
    { Registrar("lotes"); lock (estado.Sync) return estado.Lotes.ObtenerVigentesParaDescuentoAsync(idProducto, cancellationToken); }
    public Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    { Registrar("descontar"); lock (estado.Sync) return estado.Lotes.DescontarAsync(idLote, cantidad, cancellationToken); }
    public Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    { Registrar("restaurar"); lock (estado.Sync) return estado.Lotes.RestaurarAsync(idLote, cantidad, cancellationToken); }
    public Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(DateOnly hoy, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DesactivarAsync(int idLote, int? idUsuario, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(bool incluirAnuladas, int limite, CancellationToken cancellationToken = default)
    {
        Registrar("historial"); estado.IncluirAnuladas = incluirAnuladas; estado.Limite = limite;
        lock (estado.Sync) return Task.FromResult<IReadOnlyList<VentaResumenDto>>(estado.Ventas.Cabeceras
            .Where(c => incluirAnuladas || c.Estado == EstadoVenta.Completada).Take(limite)
            .Select(c => new VentaResumenDto(c.Id, c.NroComprobante!, new DateTime(2026, 6, 15), "Cliente", c.Estado, c.MontoTotal)).ToArray());
    }
    public Task<VentaDetalleConFilasDto?> ObtenerDetalleAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        Registrar("consulta-detalle");
        lock (estado.Sync)
        {
            var c = estado.Ventas.Cabeceras.SingleOrDefault(c => c.Id == idVenta);
            return Task.FromResult(c is null ? null : new VentaDetalleConFilasDto(c.Id, c.NroComprobante!,
                new DateTime(2026, 6, 15), "123", "Cliente", "Cajero", c.Estado, c.MontoTotal, c.Motivo,
                null, c.IdUsuarioAnulacion?.ToString(), c.Detalles.Select(d =>
                    new FilaDetalleVenta(d.IdProducto, "Producto", d.Cantidad, d.PrecioUnitario, d.Subtotal, "LOTE-10")).ToArray()));
        }
    }
    public ValueTask DisposeAsync() { disposed.TrySetResult(); return ValueTask.CompletedTask; }
}
