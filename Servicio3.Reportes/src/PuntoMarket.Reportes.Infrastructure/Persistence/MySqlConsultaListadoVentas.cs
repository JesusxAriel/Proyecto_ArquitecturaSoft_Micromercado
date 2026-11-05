using System.Text;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Infrastructure.Persistence;

public class MySqlConsultaListadoVentas : IConsultaListadoVentas
{
    private const string Seleccion = """
        SELECT v.id, v.nroComprobante, v.fechaVenta, c.nitCi, c.razonSocial, u.username, v.estado, v.montoTotal,
               SUM(CASE WHEN v.estado = 'COMPLETADA' THEN 1 ELSE 0 END) OVER () AS completadas,
               SUM(CASE WHEN v.estado = 'ANULADA' THEN 1 ELSE 0 END) OVER () AS anuladas,
               SUM(CASE WHEN v.estado = 'COMPLETADA' THEN v.montoTotal ELSE 0 END) OVER () AS montoCompleto,
               MAX(CASE WHEN v.estado = 'COMPLETADA' THEN v.montoTotal END) OVER () AS ventaMasAlta
        FROM VENTA v
        INNER JOIN CLIENTE c ON c.id = v.idCliente
        INNER JOIN USUARIO u ON u.id = v.idUsuario
        """;

    private const string Orden = " ORDER BY v.fechaVenta DESC, v.id DESC";

    private readonly ConexionMySql _conexion;

    public MySqlConsultaListadoVentas(ConexionMySql conexion)
    {
        _conexion = conexion;
    }

    public async Task<ResultadoListadoVentas> ConsultarAsync(
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente,
        int? tope,
        CancellationToken cancelacion)
    {
        using var conexion = _conexion.Abrir();
        using var comando = CrearComando(conexion, rango, estado, cliente, tope);
        using var lector = await comando.ExecuteReaderAsync(cancelacion);

        var filas = new List<FilaDeVenta>();
        var totales = new TotalesListadoVentas(0, 0, 0m, 0m);
        while (await lector.ReadAsync(cancelacion))
        {
            if (filas.Count == 0)
            {
                var completadas = Convert.ToInt32(lector.GetValue(8));
                var monto = lector.GetDecimal(10);
                totales = new(completadas, Convert.ToInt32(lector.GetValue(9)), monto,
                    completadas == 0 ? 0m : decimal.Round(monto / completadas, 2, MidpointRounding.AwayFromZero),
                    lector.IsDBNull(11) ? 0m : lector.GetDecimal(11));
            }
            filas.Add(MapearFila(lector));
        }
        return new(filas, totales);
    }

    private static MySqlCommand CrearComando(
        MySqlConnection conexion,
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente,
        int? tope)
    {
        var comando = conexion.CreateCommand();
        var sql = new StringBuilder(Seleccion);

        AgregarCondiciones(comando, sql, rango, estado, cliente);
        sql.Append(Orden);
        AgregarLimite(comando, sql, tope);

        comando.CommandText = sql.ToString();
        return comando;
    }

    private static void AgregarCondiciones(
        MySqlCommand comando,
        StringBuilder sql,
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente)
    {
        var condiciones = new List<string>();

        if (rango.Desde is { } desde)
        {
            condiciones.Add("v.fechaVenta >= @desde");
            comando.Parameters.AddWithValue("@desde", desde.ToDateTime(TimeOnly.MinValue));
        }

        if (rango.Hasta is { } hasta)
        {
            condiciones.Add("v.fechaVenta < @hastaExclusivo");
            comando.Parameters.AddWithValue("@hastaExclusivo", hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        if (estado is { } estadoBuscado)
        {
            condiciones.Add("v.estado = @estado");
            comando.Parameters.AddWithValue("@estado", NombreEnLaBase(estadoBuscado));
        }

        if (cliente is not null)
        {
            condiciones.Add("(c.nitCi LIKE @cliente OR c.razonSocial LIKE @cliente)");
            comando.Parameters.AddWithValue("@cliente", PatronDeBusqueda(cliente));
        }

        if (condiciones.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", condiciones));
    }

    private static void AgregarLimite(MySqlCommand comando, StringBuilder sql, int? tope)
    {
        if (tope is null)
            return;

        sql.Append(" LIMIT @tope");
        comando.Parameters.AddWithValue("@tope", tope.Value);
    }

    private static string NombreEnLaBase(EstadoDeVenta estado) => estado.ToString().ToUpperInvariant();

    private static string PatronDeBusqueda(string cliente) =>
        $"%{cliente.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";

    private static FilaDeVenta MapearFila(System.Data.Common.DbDataReader lector) =>
        new(
            lector.GetInt32(0),
            lector.GetString(1),
            lector.GetDateTime(2),
            lector.GetString(3),
            lector.GetString(4),
            lector.GetString(5),
            LeerEstado(lector.GetString(6)),
            lector.GetDecimal(7));

    private static EstadoDeVenta LeerEstado(string valor) => Enum.Parse<EstadoDeVenta>(valor, ignoreCase: true);
}
