using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PuntoMarket.Ventas.Application.Comprobantes;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Sales;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PuntoMarket.Ventas.Api.Documentation;

public static class DocumentacionVentas
{
    public static IServiceCollection AgregarDocumentacionVentas(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PuntoMarket.Ventas", Version = "v1",
                Description = "API hexagonal de Ventas. Cajero y Administrador pueden registrar ventas y crear clientes. " +
                    "Solo Administrador puede anular. La API valida JWT Bearer, no utiliza la cookie Razor. " +
                    "Authorize requiere un JWT válido emitido por un emisor confiable configurado en la API; no hay login ni emisión de tokens aquí. " +
                    "El token Demo de Razor permanece exclusivamente en el servidor y no se puede extraer desde Swagger. " +
                    "Try it out ejecuta operaciones reales: los POST requieren autorización para modificar la base elegida."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Introduzca solo el JWT válido, sin el prefijo Bearer. No use cookies de Razor ni claves de firma."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
            options.OperationFilter<OperacionesVentasFilter>();
        });
        return services;
    }
}

// Metadatos de documentación únicamente. No interviene en autorización ni en casos de uso.
public sealed class OperacionesVentasFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var ruta = context.ApiDescription.RelativePath?.Split('?')[0] ?? "";
        var metodo = context.ApiDescription.HttpMethod;
        var accion = context.MethodInfo.Name;
        var controlador = context.MethodInfo.DeclaringType?.Name;
        var anonimo = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        if (anonimo) operation.Security = [];

        var (resumen, tipo) = (controlador, accion) switch
        {
            ("ProductosController", "Buscar") => ("Buscar productos activos", typeof(ResultadoBusquedaParaVenta)),
            ("ProductosController", "Detalle") => ("Consultar producto activo", typeof(ProductoParaVenta)),
            ("ProductosController", "Precio") => ("Consultar precio vigente", typeof(PrecioDocumentado)),
            ("ProductosController", "Stock") => ("Consultar stock vendible de lotes vigentes", typeof(StockDocumentado)),
            ("ClientesController", "Buscar") => ("Buscar clientes activos", typeof(IReadOnlyList<ClienteConsultaDto>)),
            ("ClientesController", "Crear") => ("Crear cliente (Cajero o Administrador)", typeof(ClienteConsultaDto)),
            ("VentasController", "Registrar") => ("Registrar venta con FEFO (Cajero o Administrador)", typeof(ResultadoVentaDto)),
            ("VentasController", "Anular") => ("Anular venta y restaurar lotes originales (solo Administrador)", (Type?)null),
            ("VentasController", "Historial") => ("Listar las últimas 50 ventas", typeof(IReadOnlyList<VentaResumenDto>)),
            ("VentasController", "Detalle") => ("Consultar venta, lotes y auditoría de anulación", typeof(VentaDetalleDto)),
            ("VentasController", "TotalDia") => ("Consultar total de ventas completadas del día MySQL", typeof(TotalDocumentado)),
            ("ComprobantesController", "Consultar") => ("Obtener comprobante JSON", typeof(ComprobanteDto)),
            ("ComprobantesController", "Html") => ("Obtener comprobante HTML", typeof(string)),
            _ when ruta == "health" => ("Comprobar proceso activo (no verifica MySQL)", typeof(SaludDocumentada)),
            _ => ("Obtener logo público", typeof(string))
        };
        operation.Summary = resumen;
        operation.Description = anonimo ? "Acceso público, sin JWT."
            : "Requiere JWT válido, rol Cajero o Administrador y debeCambiarPassword=false. La cookie del frontend no autentica esta API.";
        if (controlador == "VentasController" && accion == "Anular")
            operation.Description = "Solo Administrador con JWT válido y debeCambiarPassword=false. Motivo obligatorio, máximo 255 caracteres. " +
                "Restaura lotes originales dentro de una transacción. Venta inexistente o ya anulada: 409. Sin reintento automático ante respuesta perdida.";
        if (controlador == "VentasController" && accion == "Registrar")
            operation.Description += " PrecioEsperado es opcional; si difiere del vigente devuelve 409. Stock insuficiente: 409. " +
                "Cliente o producto inexistente/inactivo: 404. No se confirma una venta parcial.";

        foreach (var parametro in operation.Parameters ?? [])
            parametro.Description = parametro.Name switch
            {
                "id" => "Identificador entero positivo del recurso.",
                "incluirAnuladas" => "false por defecto; true incluye anuladas entre las últimas 50 ventas.",
                "termino" when controlador == "ProductosController" => "Nombre o presentación; 2 a 150 caracteres. Sin filtro suficiente devuelve lista vacía. Máximo 10 resultados y HayMas.",
                "termino" => "CI/NIT o razón social; hasta 150 caracteres. Vacío devuelve lista vacía. Máximo 20 resultados.",
                _ => parametro.Description
            };
        if (operation.RequestBody is not null)
            operation.RequestBody.Description = accion switch
            {
                "Registrar" => "Cliente positivo y al menos una línea no nula con producto y cantidad positivos; precioEsperado opcional. No enviar identificadores de empleado: se obtienen del JWT.",
                "Anular" => "Solo motivo no vacío, máximo 255 caracteres. El ID de venta va en la ruta; el empleado procede del JWT.",
                _ => "CI/NIT obligatorio hasta 30 caracteres y razón social obligatoria hasta 150. El creador se obtiene del JWT."
            };

        operation.Responses = new OpenApiResponses();
        var exito = metodo == "POST" ? (accion == "Anular" ? "204" : "201") : "200";
        var media = accion == "Html" ? "text/html" : ruta == "images/logo.jpg" ? "image/jpeg" : "application/json";
        operation.Responses[exito] = Respuesta(exito == "204" ? "Anulación confirmada, sin cuerpo." : "Operación completada.", tipo, media);
        if (!anonimo)
        {
            Error("400", "Solicitud, filtros o identificadores no válidos (ValidationProblemDetails o ProblemDetails).");
            Error("401", "Se requiere JWT válido y no vencido.");
            Error("403", "Rol no permitido o cambio de contraseña pendiente.");
            Error("503", "Persistencia no disponible o servicio no configurado. No asumir rollback ante pérdida de respuesta.");
            Error("500", "Error interno no previsto, sin detalles privados.");
            if (accion is "Detalle" or "Precio" or "Stock" or "Consultar" or "Html" or "Registrar")
                Error("404", "Recurso inexistente o no disponible.");
            if (metodo == "POST")
                Error("409", accion == "Anular" ? "Venta inexistente/ya anulada, restauración inválida o conflicto de persistencia."
                    : accion == "Crear" ? "CI/NIT activo duplicado o conflicto de persistencia." : "Precio cambiado, stock insuficiente o conflicto de persistencia.");
        }
        else if (ruta == "images/logo.jpg") Error("404", "Logo no disponible.");

        void Error(string estado, string descripcion) => operation.Responses[estado] = Respuesta(descripcion, typeof(ProblemDetails), "application/problem+json");
        OpenApiResponse Respuesta(string descripcion, Type? respuesta, string contenido) => new()
        {
            Description = descripcion,
            Content = respuesta is null ? null : new Dictionary<string, OpenApiMediaType>
            {
                [contenido] = new() { Schema = context.SchemaGenerator.GenerateSchema(respuesta, context.SchemaRepository) }
            }
        };
    }

    private sealed record PrecioDocumentado(int Id, decimal PrecioVenta);
    private sealed record StockDocumentado(int Id, int StockDisponible);
    private sealed record TotalDocumentado(decimal Total);
    private sealed record SaludDocumentada(string Status);
}
