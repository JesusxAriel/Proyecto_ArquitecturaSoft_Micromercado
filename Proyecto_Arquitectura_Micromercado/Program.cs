using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ingrese un número válido.");
    });

builder.Services.AddScoped<IProductRepository, MySqlProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<ICategoryRepository, MySqlCategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<ISupplierRepository, MySqlSupplierRepository>();
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();