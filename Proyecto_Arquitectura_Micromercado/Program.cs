using Microsoft.AspNetCore.Localization;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

        // Cierra el enlace de modelo de los campos de estado y auditoria de las
        // entidades: el navegador no puede imponerlos aunque los envie en el POST.
        options.ModelMetadataDetailsProviders.Add(new CamposNoEnlazablesProvider());

        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ingrese un número válido.");
    });

// El Singleton de conexion se inicializa una sola vez y se registra como dependencia,
// para que los repositorios la reciban por constructor en vez de leerla de un estatico
// global. Asi la dependencia queda explicita y el repositorio se puede construir en un
// test apuntando a otra cadena de conexion.
builder.Services.AddSingleton(
    DatabaseConnection.GetInstance(builder.Configuration.GetConnectionString("MySqlConnection")!));

// Unidad de trabajo: una por peticion HTTP. Los adaptadores MySQL la reciben por
// constructor, asi que todos comparten la misma conexion mientras haya una transaccion
// abierta. Se registra dos veces a proposito, sobre la MISMA instancia: el tipo concreto
// para los adaptadores, que necesitan pedirle la conexion, y el puerto para Application,
// que solo debe ver iniciar, confirmar y revertir.
builder.Services.AddScoped<MySqlUnidadDeTrabajo>();
builder.Services.AddScoped<IUnidadDeTrabajo>(sp =>
    sp.GetRequiredService<MySqlUnidadDeTrabajo>());

// Factory Method: unico punto de la aplicacion donde se elige el motor de persistencia.
// Se registra el Creador ABSTRACTO apuntando al Creador Concreto; para cambiar de motor
// se cambia solo el segundo tipo de cada par, sin tocar ningun otro archivo.
// El Creador es Scoped y ObtenerRepositorio() memoiza, por lo que hay exactamente un
// repositorio por peticion HTTP, incluso cuando otro Creador lo reutiliza.
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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();