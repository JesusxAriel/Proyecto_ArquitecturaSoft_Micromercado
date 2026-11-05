using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using PuntoMarket.Reportes.Api.Contracts.ListadoVentas;
using PuntoMarket.Reportes.Api.Contracts.VentasPorCategoria;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace PuntoMarket.Reportes.Api.Documentation;

public static class DocumentacionReportes
{
    public static IServiceCollection AgregarDocumentacionReportes(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PuntoMarket.Reportes", Version = "v1",
                Description = "API de reportes de solo lectura. Los reportes requieren JWT y rol Administrador. " +
                    "La cookie Razor no autentica esta API; su JWT permanece en el servidor Razor. " +
                    "Authorize admite un JWT válido obtenido por un mecanismo autorizado, nunca la clave de firma. No se emiten tokens aquí." });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Introduzca solo un JWT válido, sin prefijo Bearer. No introducir claves ni cookies." });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
            options.OperationFilter<OperacionesReportesFilter>();
        });
        return services;
    }
}

// Solo metadatos OpenAPI: no modifica filtros, autorización ni respuestas del controlador.
public sealed class OperacionesReportesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var ruta = context.ApiDescription.RelativePath?.Split('?')[0];
        var publico = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        var listado = ruta == "api/reportes/ventas";
        var reporte = listado || ruta == "api/reportes/ventas-por-categoria";
        if (publico) operation.Security = [];
        operation.Summary = ruta switch {
            "api/reportes/ventas" => "Listado de ventas (Administrador)",
            "api/reportes/ventas-por-categoria" => "Ventas por categoría (Administrador)",
            "api/salud" => "Salud del proceso (sin comprobar MySQL)",
            _ => "Identidad del usuario autenticado"
        };
        operation.Description = publico ? "Acceso público, sin JWT." : reporte
            ? "Requiere JWT válido y rol Administrador. Totales de todas las coincidencias; tope/top limita solo filas. " +
              "Sin formato devuelve JSON; pdf y xlsx descargan archivos. " + (listado ? "Monto y promedio consideran ventas completadas." : "Solo ventas completadas. Incluye gráfico en las exportaciones según su implementación existente.")
            : "Requiere JWT válido, sin restricción adicional de rol.";
        foreach (var parametro in operation.Parameters ?? [])
            parametro.Description = parametro.Name?.ToLowerInvariant() switch {
                "desde" => "Fecha inicial inclusiva (yyyy-MM-dd), opcional.",
                "hasta" => "Fecha final inclusiva (yyyy-MM-dd), opcional; no anterior a desde.",
                "estado" => "COMPLETADA o ANULADA; omitir para ambos estados.",
                "cliente" => "CI/NIT o razón social, entre 2 y 150 caracteres si se especifica.",
                "tope" => "Máximo de filas: 1–1000; predeterminado 1000. No limita los totales.",
                "idcategoria" => "Identificador positivo de categoría; omitir para todas.",
                "top" => "Primeras 1–100 categorías por monto; omitir para todas. No limita los totales.",
                "formato" => "json (predeterminado), pdf o xlsx. El enum de categorías también admite 0=Json, 1=Pdf, 2=Xlsx.",
                _ => parametro.Description
            };
        operation.Responses ??= new OpenApiResponses();
        if (reporte)
        {
            var tipo = listado ? typeof(RespuestaListadoVentas) : typeof(RespuestaVentasPorCategoria);
            operation.Responses["200"] = new OpenApiResponse { Description = "Reporte JSON o archivo según formato.", Content = new Dictionary<string,OpenApiMediaType> {
                ["application/json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(tipo, context.SchemaRepository) },
                ["application/pdf"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } },
                ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } }
            } };
            operation.Responses["400"] = new OpenApiResponse { Description = "Filtros o formato inválidos." };
            operation.Responses["403"] = new OpenApiResponse { Description = "El usuario no tiene rol Administrador." };
            operation.Responses["500"] = new OpenApiResponse { Description = "Error de consulta o generación del reporte." };
        }
        if (!publico) operation.Responses["401"] = new OpenApiResponse { Description = "JWT ausente, vencido o inválido." };
    }
}
