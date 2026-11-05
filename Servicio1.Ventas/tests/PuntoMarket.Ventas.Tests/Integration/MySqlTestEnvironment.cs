using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Facades;
using PuntoMarket.Ventas.Infrastructure.Database;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Tests.Integration;

public sealed class MySqlIntegrationFactAttribute : FactAttribute
{
    public MySqlIntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("PUNTOMARKET_MYSQL_TESTS") != "AUTORIZADO" ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PUNTOMARKET_MYSQL_TEST_CONNECTION")))
            Skip = "Integración MySQL no autorizada/configurada: requiere base aislada dedicada.";
    }
}

// No usa la configuración productiva ni MICROMERCADO_PRUEBAS_MYSQL.
internal sealed class MySqlTestEnvironment
{
    private readonly string connectionString;
    public DatabaseConnection Conexiones { get; }

    private MySqlTestEnvironment(string cadena)
    {
        connectionString = cadena;
        Conexiones = new DatabaseConnection(cadena);
    }

    public static async Task<MySqlTestEnvironment> CrearAsync()
    {
        if (Environment.GetEnvironmentVariable("PUNTOMARKET_MYSQL_TESTS") != "AUTORIZADO")
            throw new InvalidOperationException("Falta autorización explícita para integración MySQL.");
        var cadena = Environment.GetEnvironmentVariable("PUNTOMARKET_MYSQL_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Falta conexión exclusiva de pruebas.");
        var builder = new MySqlConnectionStringBuilder(cadena);
        ValidarNombre(builder.Database);
        var entorno = new MySqlTestEnvironment(cadena);
        // Primero solo lecturas: ninguna escritura hasta verificar base y marcador.
        Assert.Equal(builder.Database, await entorno.EscalarAsync<string>("SELECT DATABASE();"));
        Assert.Equal("SOLO_PRUEBAS_VENTAS", await entorno.EscalarAsync<string>(
            "SELECT proposito FROM PUNTOMARKET_TEST_GUARD WHERE id = 1;"));
        Assert.Equal(7, await entorno.EscalarAsync<int>("""
            SELECT COUNT(*) FROM information_schema.TRIGGERS
            WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME IN
            ('trg_venta_bi','trg_venta_bu','trg_venta_bd','trg_detalle_venta_bi',
             'trg_lote_ai','trg_lote_au','trg_lote_bu');
            """));
        Assert.Equal(0, await entorno.EscalarAsync<int>("""
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ('VENTA','DETALLE_VENTA','LOTE','PRODUCTO')
            AND ENGINE <> 'InnoDB';
            """));
        return entorno;
    }

    internal static void ValidarNombre(string nombre)
    {
        if (!Regex.IsMatch(nombre, "^puntomarket_ventas_test_[a-z0-9_]{1,40}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("La base debe llamarse puntomarket_ventas_test_<sufijo> y ser dedicada.");
    }

    public async Task<T> EscalarAsync<T>(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new MySqlConnection(connectionString);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros) comando.Parameters.AddWithValue(nombre, valor);
        var resultado = await comando.ExecuteScalarAsync();
        return (T)Convert.ChangeType(resultado ?? throw new InvalidOperationException("Resultado nulo inesperado."), typeof(T));
    }

    public async Task EjecutarAsync(string sql, params (string Nombre, object Valor)[] parametros)
    {
        await using var conexion = new MySqlConnection(connectionString);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros) comando.Parameters.AddWithValue(nombre, valor);
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<DatosMySql> SembrarAsync()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..16];
        await using var conexion = new MySqlConnection(connectionString);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        async Task<int> Insertar(string sql, params (string Nombre, object Valor)[] parametros)
        {
            await using var comando = new MySqlCommand(sql, conexion, transaccion);
            foreach (var (nombre, valor) in parametros) comando.Parameters.AddWithValue(nombre, valor);
            await comando.ExecuteNonQueryAsync();
            return checked((int)comando.LastInsertedId);
        }
        try
        {
            var rol = await Insertar("INSERT INTO ROL(nombre) VALUES(@n);", ("@n", "test-" + sufijo));
            var usuario = await Insertar("""
                INSERT INTO USUARIO(username,passwordHash,nombreCompleto,idRol)
                VALUES(@n,@hash,'Usuario de prueba',@rol);
                """, ("@n", "test-" + sufijo), ("@hash", Guid.NewGuid().ToString("N")), ("@rol", rol));
            var categoria = await Insertar("INSERT INTO CATEGORIAS(nombre,codigo,idUsuarioAdmin) VALUES(@n,@c,@u);",
                ("@n", "test-" + sufijo), ("@c", sufijo), ("@u", usuario));
            var proveedor = await Insertar("INSERT INTO PROVEEDOR(nombreEmpresa,numeroEmpresa,idUsuarioAdmin) VALUES(@n,@c,@u);",
                ("@n", "test-" + sufijo), ("@c", sufijo), ("@u", usuario));
            var empaque = await Insertar("INSERT INTO EMPAQUE(nombre,idUsuarioAdmin) VALUES(@n,@u);",
                ("@n", "test-" + sufijo), ("@u", usuario));
            var producto = await Insertar("""
                INSERT INTO PRODUCTO(nombre,idEmpaque,precioVenta,idCategoria,idProveedor,idUsuarioAdmin)
                VALUES(@n,@e,10,@c,@p,@u);
                """, ("@n", "test-" + sufijo), ("@e", empaque), ("@c", categoria), ("@p", proveedor), ("@u", usuario));
            var cliente = await Insertar("INSERT INTO CLIENTE(nitCi,razonSocial,idUsuarioCreacion) VALUES(@nit,'Cliente de prueba',@u);",
                ("@nit", sufijo), ("@u", usuario));
            var lote1 = await Insertar("""
                INSERT INTO LOTE(idProducto,codigoLote,cantidadInicial,cantidadDisponible,fechaVencimiento,idUsuarioAdmin)
                VALUES(@p,'FEFO-1',3,3,DATE_ADD(CURDATE(), INTERVAL 30 DAY),@u);
                """, ("@p", producto), ("@u", usuario));
            var lote2 = await Insertar("""
                INSERT INTO LOTE(idProducto,codigoLote,cantidadInicial,cantidadDisponible,fechaVencimiento,idUsuarioAdmin)
                VALUES(@p,'FEFO-2',10,10,NULL,@u);
                """, ("@p", producto), ("@u", usuario));
            await transaccion.CommitAsync();
            return new DatosMySql(usuario, cliente, producto, lote1, lote2);
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public FachadaVenta Registro(PersistenciaVentasMySql sesion, DatosMySql datos)
    {
        var usuario = new UsuarioActualEnMemoria(id: datos.Usuario);
        var stock = new FachadaActualizacionStockProducto(sesion.Lotes, TimeProvider.System, sesion.Unidad, usuario);
        return new FachadaVenta(sesion.Unidad, sesion.Ventas, sesion.Precios, stock, usuario,
            TimeProvider.System, new DetectorDeInterbloqueoMySql(), sesion.Clientes);
    }

    public FachadaAnulacion Anulacion(PersistenciaVentasMySql sesion, DatosMySql datos)
    {
        var usuario = new UsuarioActualEnMemoria(id: datos.Usuario);
        var stock = new FachadaActualizacionStockProducto(sesion.Lotes, TimeProvider.System, sesion.Unidad, usuario);
        return new FachadaAnulacion(sesion.Unidad, sesion.Ventas, stock, usuario);
    }

    public Task<int> Disponible(int lote) => EscalarAsync<int>("SELECT cantidadDisponible FROM LOTE WHERE id=@id;", ("@id", lote));
    public Task<int> Stock(int producto) => EscalarAsync<int>("SELECT stockActual FROM PRODUCTO WHERE id=@id;", ("@id", producto));
    public Task<int> CantidadVentas(int cliente) => EscalarAsync<int>("SELECT COUNT(*) FROM VENTA WHERE idCliente=@id;", ("@id", cliente));
}

internal sealed record DatosMySql(int Usuario, int Cliente, int Producto, int Lote1, int Lote2);
