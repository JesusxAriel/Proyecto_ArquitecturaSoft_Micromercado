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
using Proyecto_Arquitectura_Micromercado.Application.Users;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Background;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Cache;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Email;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using System.Globalization;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;


using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

        options.ModelMetadataDetailsProviders.Add(new CamposNoEnlazablesProvider());

        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ingrese un número válido.");

        options.Filters.Add(new CambioPasswordObligatorioPageFilter());
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
        options.Events.OnValidatePrincipal = ValidacionCookieUsuarioActivo.ValidarAsync;
    });


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = OpcionesDeRedis.Crear(
        builder.Configuration.GetConnectionString("Redis")
        ?? "redis:6379");

    options.InstanceName = "PuntoMarket:";
});

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
builder.Services.AddScoped<IActivacionUsuarioRepository, MySqlActivacionUsuarioRepository>();
builder.Services.AddScoped<IActivacionUsuarioService, ActivacionUsuarioService>();
builder.Services.AddScoped<IUsuarioAdministracionRepository, MySqlUsuarioAdministracionRepository>();
builder.Services.AddScoped<IUsuarioAdministracionService, UsuarioAdministracionService>();
builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection(SmtpEmailOptions.SectionName));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.Configure<ActivacionEmailOptions>(
    builder.Configuration.GetSection(ActivacionEmailOptions.SectionName));
builder.Services.AddScoped<IEmailActivacionUsuario, EmailActivacionUsuario>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IIdentificadorReplica, IdentificadorReplicaDelEntorno>();
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

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysPath"] ?? "/keys"))
    .SetApplicationName("PuntoMarket");

var app = builder.Build();

var replicaId = app.Services.GetRequiredService<IIdentificadorReplica>().Valor;

app.Use(async (context, next) =>
{
    context.Items["ReplicaId"] = replicaId;
    context.Response.Headers["X-Replica-Id"] = replicaId;

    await next();
});
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
