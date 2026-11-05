using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using PuntoMarket.Ventas.Api.Errors;
using PuntoMarket.Ventas.Api.Security;
using PuntoMarket.Ventas.Api.Documentation;
using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Facades;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Application.Security;
using PuntoMarket.Ventas.Application.Stock;
using PuntoMarket.Ventas.Infrastructure.Database;
using PuntoMarket.Ventas.Infrastructure.Comprobantes;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Api;

public static class ApiBootstrap
{
    public static WebApplication Crear(string[] args, Action<WebApplicationBuilder>? configurar = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ApiBootstrap).Assembly.FullName
        });
        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) &&
            string.IsNullOrWhiteSpace(builder.Configuration["HTTP_PORTS"]) &&
            string.IsNullOrWhiteSpace(builder.Configuration["HTTPS_PORTS"]))
            builder.WebHost.UseUrls("http://0.0.0.0:5002");
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        // API sin cookies: no leer/escribir anillos de claves del perfil del sistema.
        builder.Services.AddDataProtection();
        builder.Services.Configure<KeyManagementOptions>(options => options.XmlRepository = new ClavesTemporales());
        builder.Services.AddControllers().AddJsonOptions(o =>
            o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeJwt>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDetectorDeInterbloqueo, DetectorDeInterbloqueoMySql>();
        builder.Services.AddSingleton(sp =>
        {
            var cadena = sp.GetRequiredService<IConfiguration>().GetConnectionString("Ventas");
            if (string.IsNullOrWhiteSpace(cadena)) throw new ServicioNoConfiguradoException();
            try { return new DatabaseConnection(cadena); }
            catch (ArgumentException) { throw new ServicioNoConfiguradoException(); }
        });
        // Los alias resuelven la misma unidad; DisposeAsync es idempotente.
        builder.Services.AddScoped<PersistenciaVentasMySql>();
        builder.Services.AddScoped<IUnidadDeTrabajo>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Unidad);
        builder.Services.AddScoped<IVentaRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Ventas);
        builder.Services.AddScoped<ILoteRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Lotes);
        builder.Services.AddScoped<IConPrecioVigente>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Precios);
        builder.Services.AddScoped<IClienteActivoConsulta>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Clientes);
        builder.Services.AddScoped<IVentaConsultaRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Consultas);
        builder.Services.AddScoped<IProductoVentaConsultaRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().ProductosConsulta);
        builder.Services.AddScoped<IClienteVentaConsultaRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().ClientesConsulta);
        builder.Services.AddScoped<IClienteCreacionRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().ClientesCreacion);
        builder.Services.AddScoped<IComprobanteRepository>(sp => sp.GetRequiredService<PersistenciaVentasMySql>().Comprobantes);
        builder.Services.AddScoped<IProductoVentaConsultaService, ProductoVentaConsultaService>();
        builder.Services.AddScoped<IClienteVentaConsultaService, ClienteVentaConsultaService>();
        builder.Services.AddScoped<IClienteCreacionService, ClienteCreacionService>();
        builder.Services.AddScoped<IComprobanteService, ComprobanteService>();
        builder.Services.AddSingleton<IGeneradorComprobante, GeneradorComprobanteHtml>();
        builder.Services.AddScoped<IFachadaVenta, FachadaVenta>();
        builder.Services.AddScoped<IFachadaAnulacion, FachadaAnulacion>();
        builder.Services.AddScoped<IFachadaActualizacionStockProducto, FachadaActualizacionStockProducto>();
        builder.Services.AddScoped<IVentaConsultaService, VentaConsultaService>();

        // Permite configurar el host y sustituir exclusivamente puertos en pruebas HTTP.
        configurar?.Invoke(builder);
        var documentacionLocal = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("LocalDemo");
        if (documentacionLocal) builder.Services.AgregarDocumentacionVentas();
        builder.Services.AgregarSeguridadJwt(builder.Configuration);
        var app = builder.Build();
        if (documentacionLocal)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("v1/swagger.json", "PuntoMarket.Ventas v1");
                options.ConfigObject.PersistAuthorization = false;
            });
        }
        app.UseExceptionHandler();
        app.UseStatusCodePages(async context =>
        {
            var http = context.HttpContext;
            await ErroresHttp.EscribirAsync(http, http.Response.StatusCode, "Solicitud no completada.");
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous().WithTags("Health");
        app.MapGet("/images/logo.jpg", (IWebHostEnvironment entorno) =>
        {
            var ruta = Path.Combine(entorno.ContentRootPath, "wwwroot", "images", "logo.jpg");
            return File.Exists(ruta) ? Results.File(ruta, "image/jpeg") : Results.NotFound();
        }).AllowAnonymous().WithTags("Recursos");
        app.MapControllers();
        return app;
    }
}
