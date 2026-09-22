using Microsoft.AspNetCore.Localization;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ingrese un número válido.");
    });

// --- MÓDULO PRODUCTO E HISTORIAL (FACTORY METHOD) ---
builder.Services.AddScoped<CreatorPriceHistoryRepository>();
builder.Services.AddScoped<IPriceHistoryRepository>(sp =>
    sp.GetRequiredService<CreatorPriceHistoryRepository>().CrearRepositorio());

builder.Services.AddScoped<CreatorProductRepository>();
builder.Services.AddScoped<IProductRepository>(sp =>
    sp.GetRequiredService<CreatorProductRepository>().CrearRepositorio());
builder.Services.AddScoped<IProductService, ProductService>();

// --- MÓDULO CATEGORÍAS (SIN CAMBIOS) ---
builder.Services.AddScoped<ICategoryRepository, MySqlCategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// --- MÓDULO PROVEEDORES (FACTORY METHOD) ---
builder.Services.AddScoped<CreatorSupplierRepository>();
builder.Services.AddScoped<ISupplierRepository>(sp =>
    sp.GetRequiredService<CreatorSupplierRepository>().CrearRepositorio());
builder.Services.AddScoped<ISupplierService, SupplierService>();

var app = builder.Build();
var boliviaCulture = new CultureInfo("es-BO");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(boliviaCulture),
    SupportedCultures = [boliviaCulture],
    SupportedUICultures = [boliviaCulture]
});

// Configure the HTTP request pipeline.
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