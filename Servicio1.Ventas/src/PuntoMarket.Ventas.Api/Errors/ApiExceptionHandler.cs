using System.Data.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Api.Errors;

public sealed class ServicioNoConfiguradoException : Exception;

public static class ErroresHttp
{
    public static Task EscribirAsync(HttpContext contexto, int estado, string titulo,
        string? codigo = null, IDictionary<string, object?>? datos = null)
    {
        contexto.Response.StatusCode = estado;
        var problem = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            Type = $"https://httpstatuses.com/{estado}",
            Instance = contexto.Request.Path
        };
        problem.Extensions["traceId"] = contexto.TraceIdentifier;
        if (codigo is not null) problem.Extensions["code"] = codigo;
        if (datos is not null) foreach (var item in datos) problem.Extensions[item.Key] = item.Value;
        return contexto.Response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json", cancellationToken: contexto.RequestAborted);
    }
}

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken cancellationToken)
    {
        var (estado, titulo, codigo) = error switch
        {
            PrecioCambiadoException => (409, "El precio vigente cambió. Actualice el detalle de venta.", "precio_cambiado"),
            StockInsuficienteException => (409, "Stock insuficiente para completar la venta.", "stock_insuficiente"),
            RestauracionExcedeCantidadInicialException => (409, "La restauración excedería la cantidad inicial de un lote.", "restauracion_invalida"),
            InvalidOperationException { Message: "Cliente inexistente o inactivo." } => (404, "Cliente inexistente o inactivo.", "cliente_no_disponible"),
            InvalidOperationException { Message: "Producto inexistente o inactivo." } => (404, "Producto inexistente o inactivo.", "producto_no_disponible"),
            InvalidOperationException { Message: "La venta no existe o ya fue anulada." } => (409, "La venta no existe o ya fue anulada.", "venta_no_anulable"),
            InvalidOperationException ex when ValidacionConocida(ex.Message) => (400, "Datos de venta no válidos.", "validacion"),
            MySqlException { Number: 1213 or 1205 or 1062 or 1452 or 3819 or 1644 } => (409, "La operación presenta un conflicto de persistencia.", "conflicto_persistencia"),
            ClienteDuplicadoException => (409, "Ya existe un cliente activo con ese CI/NIT.", "cliente_duplicado"),
            ValidationException => (400, "Los datos del cliente no son válidos.", "validacion_cliente"),
            DbException => (503, "La persistencia no está disponible.", "persistencia_no_disponible"),
            ServicioNoConfiguradoException => (503, "El servicio de ventas no está configurado.", "configuracion_pendiente"),
            _ => (500, "Ocurrió un error al procesar la solicitud.", "error_interno")
        };
        IDictionary<string, object?>? datos = error is PrecioCambiadoException precio
            ? new Dictionary<string, object?> { ["idProducto"] = precio.IdProducto, ["precioVigente"] = precio.PrecioVigente }
            : null;
        await ErroresHttp.EscribirAsync(http, estado, titulo, codigo, datos);
        return true;
    }

    private static bool ValidacionConocida(string mensaje) => mensaje is
        "Debe seleccionar un cliente para registrar la venta." or
        "Debe seleccionar al menos un producto para registrar la venta." or
        "Cada línea debe tener un producto válido y una cantidad mayor a cero." or
        "Debe indicar la venta que se desea anular." or
        "Debe indicar el motivo de la anulación.";
}
