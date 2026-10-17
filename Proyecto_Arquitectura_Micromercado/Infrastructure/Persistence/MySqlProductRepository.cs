using Proyecto_Arquitectura_Micromercado.Application.Security;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlProductRepository(
    MySqlUnidadDeTrabajo unidadDeTrabajo,
    IPriceHistoryRepository priceHistoryRepository,
    IUsuarioActual usuarioActual) : IProductRepository
{
    private const int DuplicateKeyErrorNumber = 1062;
    private readonly IPriceHistoryRepository _priceHistoryRepository = priceHistoryRepository;

    public async Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, idEmpaque, empaquePresentacion, precioVenta, precioCosto,
                   stockMinimo, idCategoria, nombreCategoria, idProveedor,
                   nombreProveedor, stockCalculado
            FROM vw_productos_con_stock
            ORDER BY nombre;
            """;

        var products = new List<ProductListItem>();
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess,
            cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(MapListItem(reader));
        }

        return products;
    }

    public Task<PagedResult<ProductListItem>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default) =>
        GetPagedCoreAsync(page, pageSize, search,
            "nombre LIKE @search OR empaquePresentacion LIKE @search OR nombreCategoria LIKE @search OR nombreProveedor LIKE @search",
            cancellationToken);

    public async Task<PagedResult<ProductListItem>> GetPagedForSalesAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        return await PagedSqlRunner.RunAsync(
            alquiler.Conexion,
            alquiler.Transaccion,
            columns: """
                p.id, p.nombre, p.idEmpaque, e.nombre AS empaquePresentacion,
                p.precioVenta, p.stockMinimo, p.stockActual AS stockCalculado
                """,
            from: "PRODUCTO p INNER JOIN EMPAQUE e ON e.id = p.idEmpaque",
            baseWhere: "p.estaActivo = 1",
            searchCondition: "p.nombre LIKE @search OR e.nombre LIKE @search",
            orderBy: "p.nombre, p.id",
            search,
            page,
            pageSize,
            MapSalesItem,
            cancellationToken);
    }

    private async Task<PagedResult<ProductListItem>> GetPagedCoreAsync(
        int page,
        int pageSize,
        string? search,
        string searchCondition,
        CancellationToken cancellationToken)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        return await PagedSqlRunner.RunAsync(
            alquiler.Conexion,
            alquiler.Transaccion,
            columns: """
                id, nombre, idEmpaque, empaquePresentacion, precioVenta, precioCosto,
                stockMinimo, idCategoria, nombreCategoria, idProveedor,
                nombreProveedor, stockCalculado
                """,
            from: "vw_productos_con_stock",
            baseWhere: null,
            searchCondition: searchCondition,
            orderBy: "nombre, id",
            search,
            page,
            pageSize,
            MapListItem,
            cancellationToken);
    }

    private static ProductListItem MapSalesItem(System.Data.Common.DbDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("id")),
        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
        IdEmpaque = reader.GetInt32(reader.GetOrdinal("idEmpaque")),
        EmpaquePresentacion = reader.GetString(reader.GetOrdinal("empaquePresentacion")),
        PrecioVenta = reader.GetDecimal(reader.GetOrdinal("precioVenta")),
        StockMinimo = reader.GetInt32(reader.GetOrdinal("stockMinimo")),
        StockCalculado = reader.GetInt32(reader.GetOrdinal("stockCalculado"))
    };

    private static ProductListItem MapListItem(System.Data.Common.DbDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("id")),
        Nombre = reader.GetString(reader.GetOrdinal("nombre")),
        IdEmpaque = reader.GetInt32(reader.GetOrdinal("idEmpaque")),
        EmpaquePresentacion = reader.GetString(reader.GetOrdinal("empaquePresentacion")),
        PrecioVenta = reader.GetDecimal(reader.GetOrdinal("precioVenta")),
        PrecioCosto = reader.GetDecimal(reader.GetOrdinal("precioCosto")),
        StockMinimo = reader.GetInt32(reader.GetOrdinal("stockMinimo")),
        IdCategoria = reader.GetInt32(reader.GetOrdinal("idCategoria")),
        NombreCategoria = reader.GetString(reader.GetOrdinal("nombreCategoria")),
        IdProveedor = reader.GetInt32(reader.GetOrdinal("idProveedor")),
        NombreProveedor = reader.GetString(reader.GetOrdinal("nombreProveedor")),
        StockCalculado = reader.GetInt32(reader.GetOrdinal("stockCalculado"))
    };

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, idEmpaque, precioVenta, precioCosto,
                   stockMinimo, idCategoria, idProveedor, estaActivo
            FROM PRODUCTO
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Product
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            IdEmpaque = reader.GetInt32(reader.GetOrdinal("idEmpaque")),
            PrecioVenta = reader.GetDecimal(reader.GetOrdinal("precioVenta")),
            PrecioCosto = reader.GetDecimal(reader.GetOrdinal("precioCosto")),
            StockMinimo = reader.GetInt32(reader.GetOrdinal("stockMinimo")),
            IdCategoria = reader.GetInt32(reader.GetOrdinal("idCategoria")),
            IdProveedor = reader.GetInt32(reader.GetOrdinal("idProveedor")),
            EstaActivo = reader.GetBoolean(reader.GetOrdinal("estaActivo"))
        };
    }

    public async Task<bool> ExistsNombreEmpaqueAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM PRODUCTO
                WHERE estaActivo = 1
                  AND id <> @idExcluido
                  AND idEmpaque = @idEmpaque
                  AND TRIM(REGEXP_REPLACE(nombre, '[[:space:]]+', ' ')) = @nombre
            );
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@nombre", nombre.Trim());
        command.Parameters.AddWithValue("@idEmpaque", idEmpaque);
        command.Parameters.AddWithValue("@idExcluido", idExcluido);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> ExistenProductosActivosDelProveedorAsync(
        int idProveedor,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM PRODUCTO
                WHERE idProveedor = @idProveedor
                  AND estaActivo = 1
            );
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@idProveedor", idProveedor);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<decimal?> ObtenerPrecioVigenteAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT precioVenta
            FROM PRODUCTO
            WHERE id = @idProducto
              AND estaActivo = 1
            FOR UPDATE;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@idProducto", idProducto);
        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is null || resultado == DBNull.Value ? null : Convert.ToDecimal(resultado);
    }

    public async Task<DescripcionDeProducto?> ObtenerDescripcionAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.nombre, e.nombre AS empaquePresentacion
            FROM PRODUCTO p
            INNER JOIN EMPAQUE e ON e.id = p.idEmpaque
            WHERE p.id = @idProducto;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@idProducto", idProducto);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new DescripcionDeProducto(reader.GetString(reader.GetOrdinal("nombre")), reader.GetString(reader.GetOrdinal("empaquePresentacion")))
            : null;
    }

    public async Task<ResultadoBusquedaParaVenta> BuscarParaVentaAsync(
        string termino,
        int limite,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.id, p.nombre, e.nombre AS empaquePresentacion, p.precioVenta, p.stockActual
            FROM PRODUCTO p
            INNER JOIN EMPAQUE e ON e.id = p.idEmpaque
            WHERE p.estaActivo = 1 AND (p.nombre LIKE @patron OR e.nombre LIKE @patron)
            ORDER BY p.nombre, p.id
            LIMIT @limite;
            """;

        return await ReintentoDeLecturas.EjecutarAsync(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            command.Parameters.AddWithValue("@patron", SqlLike.ContainsPattern(termino));
            command.Parameters.AddWithValue("@limite", limite + 1);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var productos = new List<ProductoParaVenta>();
            while (await reader.ReadAsync(cancellationToken))
            {
                productos.Add(new ProductoParaVenta(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("nombre")),
                    reader.GetString(reader.GetOrdinal("empaquePresentacion")),
                    reader.GetDecimal(reader.GetOrdinal("precioVenta")),
                    reader.GetInt32(reader.GetOrdinal("stockActual"))));
            }

            var hayMas = productos.Count > limite;
            return new ResultadoBusquedaParaVenta(hayMas ? productos.Take(limite).ToList() : productos, hayMas);
        });
    }

    public Task<IReadOnlyList<LookupOption>> GetPackagingsAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombre FROM EMPAQUE WHERE estaActivo = 1 ORDER BY nombre;", cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombre FROM CATEGORIAS WHERE estaActivo = 1 ORDER BY nombre;", cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombreEmpresa FROM PROVEEDOR WHERE estaActivo = 1 ORDER BY nombreEmpresa;", cancellationToken);

    public async Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT h.id, h.idProducto, p.nombre AS NombreProducto,
                   h.precioVentaAnterior, h.precioVentaNuevo,
                   h.precioCostoAnterior, h.precioCostoNuevo,
                   h.motivoCambio, h.idUsuario, h.fechaCambio
            FROM HISTORIAL_PRECIO h
            INNER JOIN PRODUCTO p ON p.id = h.idProducto
            ORDER BY h.fechaCambio DESC;
            """;

        var history = new List<ProductPriceHistory>();
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new ProductPriceHistory
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                IdProducto = reader.GetInt32(reader.GetOrdinal("idProducto")),
                NombreProducto = reader.GetString(reader.GetOrdinal("NombreProducto")),
                PrecioVentaAnterior = reader.GetDecimal(reader.GetOrdinal("precioVentaAnterior")),
                PrecioVentaNuevo = reader.GetDecimal(reader.GetOrdinal("precioVentaNuevo")),
                PrecioCostoAnterior = reader.IsDBNull(reader.GetOrdinal("precioCostoAnterior"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("precioCostoAnterior")),
                PrecioCostoNuevo = reader.IsDBNull(reader.GetOrdinal("precioCostoNuevo"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("precioCostoNuevo")),
                MotivoCambio = reader.IsDBNull(reader.GetOrdinal("motivoCambio"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("motivoCambio")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("idUsuario")),
                FechaCambio = reader.GetDateTime(reader.GetOrdinal("fechaCambio"))
            });
        }

        return history;
    }

    public async Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        await _priceHistoryRepository.AddPriceHistoryAsync(history, cancellationToken);
    }

    public async Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO PRODUCTO
                (nombre, idEmpaque, precioVenta, precioCosto, stockMinimo,
                 idCategoria, idProveedor, estaActivo, idUsuarioAdmin)
            VALUES
                (@nombre, @idEmpaque, @precioVenta, @precioCosto, @stockMinimo,
                 @idCategoria, @idProveedor, 1, @idUsuarioAdmin);
            SELECT LAST_INSERT_ID();
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        AddProductParameters(command, product);
        command.Parameters.AddWithValue("@idUsuarioAdmin", usuarioActual.Id);

        int generatedId;
        try
        {
            generatedId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == DuplicateKeyErrorNumber)
        {
            throw new DuplicateProductException(ex);
        }

        product.Id = generatedId;
        return generatedId;
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string motivoSql = "SET @motivo_cambio_precio = @motivo;";
        const string updateSql = """
            UPDATE PRODUCTO
            SET nombre = @nombre,
                idEmpaque = @idEmpaque,
                precioVenta = @precioVenta,
                precioCosto = @precioCosto,
                stockMinimo = @stockMinimo,
                idCategoria = @idCategoria,
                idProveedor = @idProveedor,
                idUsuarioActualizacion = @idUsuarioActualizacion
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        await using (var motivoCommand = new MySqlCommand(motivoSql, alquiler.Conexion, alquiler.Transaccion))
        {
            motivoCommand.Parameters.AddWithValue(
                "@motivo",
                string.IsNullOrWhiteSpace(product.MotivoCambio)
                    ? DBNull.Value
                    : product.MotivoCambio.Trim());
            await motivoCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var updateCommand = new MySqlCommand(updateSql, alquiler.Conexion, alquiler.Transaccion);
        AddProductParameters(updateCommand, product);
        updateCommand.Parameters.AddWithValue("@id", product.Id);
        updateCommand.Parameters.AddWithValue("@idUsuarioActualizacion", usuarioActual.Id);

        try
        {
            return await updateCommand.ExecuteNonQueryAsync(cancellationToken) == 1;
        }
        catch (MySqlException ex) when (ex.Number == DuplicateKeyErrorNumber)
        {
            throw new DuplicateProductException(ex);
        }
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE PRODUCTO
            SET estaActivo = 0,
                idUsuarioActualizacion = @idUsuarioActualizacion
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@idUsuarioActualizacion", usuarioActual.Id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private async Task<IReadOnlyList<LookupOption>> GetLookupAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var options = new List<LookupOption>();
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new LookupOption(reader.GetInt32(0), reader.GetString(1)));
        }

        return options;
    }

    private static void AddProductParameters(MySqlCommand command, Product product)
    {
        command.Parameters.AddWithValue("@nombre", product.Nombre.Trim());
        command.Parameters.AddWithValue("@idEmpaque", product.IdEmpaque);
        command.Parameters.AddWithValue("@precioVenta", product.PrecioVenta);
        command.Parameters.AddWithValue("@precioCosto", product.PrecioCosto);
        command.Parameters.AddWithValue("@stockMinimo", product.StockMinimo);
        command.Parameters.AddWithValue("@idCategoria", product.IdCategoria);
        command.Parameters.AddWithValue("@idProveedor", product.IdProveedor);
    }
}
