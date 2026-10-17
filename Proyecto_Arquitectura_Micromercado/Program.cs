using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Background;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using System.Globalization;
using Proyecto_Arquitectura_Micromercado.Application.Sales;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

        options.ModelMetadataDetailsProviders.Add(new CamposNoEnlazablesProvider());

        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ingrese un número válido.");
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Events.OnRedirectToLogin = SesionVencidaAjax.ResponderAsync;
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeClaims>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IUsuarioRepository, MySqlUsuarioRepository>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IComprobanteRepository, MySqlComprobanteRepository>();
builder.Services.AddScoped<IComprobanteService, ComprobanteService>();
builder.Services.AddSingleton<IGeneradorComprobante, GeneradorComprobanteHtml>();

builder.Services.AddScoped<IClienteConsultaRepository, MySqlClienteConsultaRepository>();
builder.Services.AddScoped<IClienteConsultaService, ClienteConsultaService>();
builder.Services.AddScoped<IClienteCreacionRepository, MySqlClienteCreacionRepository>();
builder.Services.AddScoped<IClienteCreacionService, ClienteCreacionService>();

builder.Services.AddSingleton(
    DatabaseConnection.GetInstance(builder.Configuration.GetConnectionString("MySqlConnection")!));

builder.Services.AddScoped<MySqlUnidadDeTrabajo>();
builder.Services.AddScoped<IUnidadDeTrabajo>(sp =>
    sp.GetRequiredService<MySqlUnidadDeTrabajo>());

builder.Services.AddScoped<CreatorPriceHistoryRepository, CreatorPriceHistoryRepositoryMySql>();
builder.Services.AddScoped<IPriceHistoryRepository>(sp =>
    sp.GetRequiredService<CreatorPriceHistoryRepository>().ObtenerRepositorio());

builder.Services.AddScoped<CreatorProductRepository, CreatorProductRepositoryMySql>();
builder.Services.AddScoped<IProductRepository>(sp =>
    sp.GetRequiredService<CreatorProductRepository>().ObtenerRepositorio());
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<CreatorCategoryRepository, CreatorCategoryRepositoryMySql>();
builder.Services.AddScoped<ICategoryRepository>(sp =>
    sp.GetRequiredService<CreatorCategoryRepository>().ObtenerRepositorio());
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<CreatorSupplierRepository, CreatorSupplierRepositoryMySql>();
builder.Services.AddScoped<ISupplierRepository>(sp =>
    sp.GetRequiredService<CreatorSupplierRepository>().ObtenerRepositorio());
builder.Services.AddScoped<ISupplierService, SupplierService>();

builder.Services.AddScoped<CreatorVentaRepository, CreatorVentaRepositoryMySql>();
builder.Services.AddScoped<IVentaRepository>(sp =>
    sp.GetRequiredService<CreatorVentaRepository>().ObtenerRepositorio());
builder.Services.AddScoped<IVentaConsultaRepository, MySqlVentaConsultaRepository>();
builder.Services.AddScoped<IVentaConsultaService, VentaConsultaService>();

builder.Services.AddScoped<CreatorLoteRepository, CreatorLoteRepositoryMySql>();
builder.Services.AddScoped<ILoteRepository>(sp =>
    sp.GetRequiredService<CreatorLoteRepository>().ObtenerRepositorio());

builder.Services.AddScoped<IConPrecioVigente>(sp => sp.GetRequiredService<IProductRepository>());
builder.Services.AddScoped<IFachadaActualizacionStockProducto, FachadaActualizacionStockProducto>();
builder.Services.AddScoped<IBajaDeLotesService, BajaDeLotesService>();
builder.Services.AddHostedService<BajaAutomaticaDeLotesVencidos>();
builder.Services.AddSingleton<IDetectorDeInterbloqueo, DetectorDeInterbloqueoMySql>();
builder.Services.AddScoped<IFachadaVenta, FachadaVenta>();
builder.Services.AddScoped<IFachadaAnulacion, FachadaAnulacion>();

var app = builder.Build();
var boliviaCulture = new CultureInfo("es-BO");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(boliviaCulture),
    SupportedCultures = [boliviaCulture],
    SupportedUICultures = [boliviaCulture]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
