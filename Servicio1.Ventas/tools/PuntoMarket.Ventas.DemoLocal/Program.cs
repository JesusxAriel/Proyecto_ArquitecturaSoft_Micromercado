using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Api;
using PuntoMarket.Ventas.Application.Products;
using PuntoMarket.Ventas.Application.Customers;
using PuntoMarket.Ventas.Infrastructure.Persistence;

// Herramienta operativa local: hospeda la API original, sin sustituir servicios/puertos.
// No agrega un emisor de tokens al API; los JWT de comprobacion solo viven aqui.
WebApplication? app = null;
try
{
    var directorio = new DirectoryInfo(AppContext.BaseDirectory);
    while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "database", "demo-env", "compose.yaml")))
        directorio = directorio.Parent;
    if (directorio is null) throw new InvalidOperationException("No se encuentra Servicio1.Ventas.");
    var raiz = directorio.FullName;
    var envFile = Path.Combine(raiz, "database", "demo-env", ".env");
    var configuracion = File.ReadAllLines(envFile).Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
        .Select(l => l.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);
    if (configuracion["VENTAS_DEMO_USER"] != "ventas_demo") throw new InvalidOperationException("Usuario Demo inesperado.");
    var rutaPrivada = Path.GetFullPath(configuracion["VENTAS_DEMO_PASSWORD_FILE"]);
    var carpetaPrivada = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "puntomarket-demo-secrets") + Path.DirectorySeparatorChar;
    if (!rutaPrivada.StartsWith(carpetaPrivada, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Ruta privada inesperada.");
    if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(p => p.Port == 5002))
        throw new InvalidOperationException("Puerto 5002 ocupado; no se reemplaza otro proceso.");

    // Builder codifica caracteres especiales. Nunca imprimir ni persistir esta cadena.
    var cadena = new MySqlConnectionStringBuilder
    {
        Server = "127.0.0.1", Port = 3308, Database = "puntomarket_ventas_demo_local",
        UserID = configuracion["VENTAS_DEMO_USER"], Password = File.ReadAllText(rutaPrivada),
        SslMode = MySqlSslMode.Required, ConnectionTimeout = 10
    };
    await using var conexion = new MySqlConnection(cadena.ConnectionString);
    await conexion.OpenAsync();
    await using (var comando = new MySqlCommand("SELECT DATABASE(),CURRENT_USER();", conexion))
    await using (var lector = await comando.ExecuteReaderAsync())
    {
        if (!await lector.ReadAsync() || lector.GetString(0) != "puntomarket_ventas_demo_local" || !lector.GetString(1).StartsWith("ventas_demo@", StringComparison.Ordinal))
            throw new InvalidOperationException("Destino de conexion inesperado.");
    }
    var antes = await Huella(conexion);
    Console.WriteLine("MySQL Demo conectado mediante TLS requerido; destino y cuenta verificados (sin credenciales en salida).");

    // Clave compartida solo por los procesos locales; fuera del repositorio y con ACL privada.
    var rutaClave = Path.Combine(carpetaPrivada, "jwt-demo-key.txt");
    if (!File.Exists(rutaClave))
    {
        using var archivoClave = new FileStream(rutaClave, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        archivoClave.Write(Encoding.UTF8.GetBytes(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))));
    }
    var clave = File.ReadAllText(rutaClave);
    if (Encoding.UTF8.GetByteCount(clave) < 32) throw new InvalidOperationException("Clave privada Demo inválida.");
    const string emisor = "PuntoMarket.DemoLocal";
    const string audiencia = "PuntoMarket.Ventas.DemoLocal";
    app = ApiBootstrap.Crear(["--contentRoot", Path.Combine(raiz, "src", "PuntoMarket.Ventas.Api"), "--environment", "LocalDemo"], builder =>
    {
        builder.WebHost.UseUrls("http://localhost:5002");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Ventas"] = cadena.ConnectionString,
            ["Jwt:Key"] = clave, ["Jwt:Issuer"] = emisor, ["Jwt:Audience"] = audiencia
        });
        // La herramienta emite solo resultados saneados, no logs de peticiones/errores con secretos.
        builder.Logging.ClearProviders();
    });
    await using (var scope = app.Services.CreateAsyncScope())
    {
        if (scope.ServiceProvider.GetRequiredService<IProductoVentaConsultaRepository>().GetType() != typeof(MySqlProductoVentaConsultaRepository) ||
            scope.ServiceProvider.GetRequiredService<IClienteVentaConsultaRepository>().GetType() != typeof(MySqlClienteVentaConsultaRepository))
            throw new InvalidOperationException("La DI no contiene los repositorios MySQL esperados.");
    }
    await app.StartAsync();
    using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5002"), Timeout = TimeSpan.FromSeconds(20) };
    var verificadas = 0;
    async Task<JsonElement?> Get(string ruta, HttpStatusCode esperado, string? token = null)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (token is not null) peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var respuesta = await http.SendAsync(peticion);
        if (respuesta.StatusCode != esperado) throw new InvalidOperationException($"Estado HTTP inesperado: {ruta}.");
        verificadas++;
        Console.WriteLine($"GET {ruta} -> {(int)respuesta.StatusCode}");
        if (esperado != HttpStatusCode.OK) return null;
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }
    string Token(string rol, string? otraClave = null) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        emisor, audiencia,
        [new Claim("sub", "2"), new Claim("unique_name", "cajero1"), new Claim("role", rol), new Claim("debeCambiarPassword", "false")],
        notBefore: DateTime.UtcNow.AddSeconds(-5), expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(otraClave ?? clave)), SecurityAlgorithms.HmacSha256)));
    var jwt = Token("Cajero");
    await Get("/health", HttpStatusCode.OK);
    await Get("/api/productos?termino=leche", HttpStatusCode.Unauthorized);
    await Get("/api/clientes?termino=DEMO", HttpStatusCode.Unauthorized);
    await Get("/api/productos/1", HttpStatusCode.Unauthorized, Token("Cajero", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))));
    await Get("/api/productos/1", HttpStatusCode.Forbidden, Token("Observador"));

    string nombre;
    decimal precio;
    int stock;
    await using (var comando = new MySqlCommand("SELECT p.nombre,p.precioVenta,(SELECT COALESCE(SUM(l.cantidadDisponible),0) FROM LOTE l WHERE l.idProducto=p.id AND l.estaActivo=1 AND l.cantidadDisponible>0 AND (l.fechaVencimiento IS NULL OR l.fechaVencimiento>=@hoy)) FROM PRODUCTO p WHERE p.id=1 AND p.estaActivo=1;", conexion))
    {
        comando.Parameters.AddWithValue("@hoy", DateTime.Today);
        await using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) throw new InvalidOperationException("Falta producto original 1.");
        nombre = lector.GetString(0); precio = lector.GetDecimal(1); stock = Convert.ToInt32(lector.GetValue(2), CultureInfo.InvariantCulture);
    }
    var productos = (await Get("/api/productos?termino=leche", HttpStatusCode.OK, jwt))!.Value;
    if (!productos.GetProperty("productos").EnumerateArray().Any(p => p.GetProperty("id").GetInt32() == 1 && p.GetProperty("nombre").GetString() == nombre))
        throw new InvalidOperationException("Busqueda de productos no coincide con MySQL.");
    var detalle = (await Get("/api/productos/1", HttpStatusCode.OK, jwt))!.Value;
    if (detalle.GetProperty("nombre").GetString() != nombre || detalle.GetProperty("precioVenta").GetDecimal() != precio || detalle.GetProperty("stockDisponible").GetInt32() != stock)
        throw new InvalidOperationException("Detalle no coincide con MySQL.");
    var p = (await Get("/api/productos/1/precio", HttpStatusCode.OK, jwt))!.Value;
    var s = (await Get("/api/productos/1/stock", HttpStatusCode.OK, jwt))!.Value;
    if (p.GetProperty("precioVenta").GetDecimal() != precio || s.GetProperty("stockDisponible").GetInt32() != stock)
        throw new InvalidOperationException("Precio/stock no coincide con MySQL.");
    // Leer el identificador de la semilla existente: sirve tanto para instalaciones nuevas
    // sinteticas como para una Demo anterior sin modificar sus registros.
    string terminoCliente;
    await using (var consultaCliente = new MySqlCommand("SELECT nitCi FROM CLIENTE WHERE id=2 AND estaActivo=1", conexion))
        terminoCliente = Convert.ToString(await consultaCliente.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Falta cliente de comprobacion 2.");
    var rutaCliente = "/api/clientes?termino=" + Uri.EscapeDataString(terminoCliente);
    var clientes = (await Get(rutaCliente, HttpStatusCode.OK, jwt))!.Value;
    await using (var comando = new MySqlCommand("SELECT id,nitCi,razonSocial FROM CLIENTE WHERE id=2 AND estaActivo=1;", conexion))
    await using (var lector = await comando.ExecuteReaderAsync())
    {
        if (!await lector.ReadAsync() || !clientes.EnumerateArray().Any(c => c.GetProperty("id").GetInt32() == lector.GetInt32(0) && c.GetProperty("nitCi").GetString() == lector.GetString(1) && c.GetProperty("razonSocial").GetString() == lector.GetString(2)))
            throw new InvalidOperationException("Cliente HTTP no coincide con MySQL.");
    }
    var porNit = (await Get(rutaCliente, HttpStatusCode.OK, jwt))!.Value;
    if (!porNit.EnumerateArray().Any(c => c.GetProperty("id").GetInt32() == 2)) throw new InvalidOperationException("Busqueda por NIT incorrecta.");
    await Get("/api/productos/2147483647", HttpStatusCode.NotFound, jwt);
    var vacio = (await Get("/api/productos?termino=ZZZ_SIN_COINCIDENCIAS_DEMO", HttpStatusCode.OK, jwt))!.Value;
    if (vacio.GetProperty("productos").GetArrayLength() != 0) throw new InvalidOperationException("Busqueda vacia incorrecta.");
    var despues = await Huella(conexion);
    if (antes.Count != despues.Count || antes.Any(kv => !despues.TryGetValue(kv.Key, out var valor) || valor != kv.Value))
        throw new InvalidOperationException("Cambio el contenido de las tablas durante la verificacion.");
    Console.WriteLine($"Verificadas {verificadas} solicitudes GET reales; repositorios MySQL confirmados por DI.");
    Console.WriteLine($"Producto 1: {nombre}; precio {precio.ToString("0.00", CultureInfo.InvariantCulture)}; stock disponible {stock}.");
    Console.WriteLine("Cliente de comprobacion 2 coincide con MySQL. Checksums de las 11 tablas iguales antes/despues.");
    jwt = "";
    if (args.Contains("--solo-verificar", StringComparer.Ordinal))
    {
        await app.StopAsync();
        Console.WriteLine("Comprobacion finalizada; API detenida por --solo-verificar.");
    }
    else
    {
        Console.WriteLine($"API original activa en http://localhost:5002 (PID {Environment.ProcessId}). Clave privada externa compartida con Razor; ningun token se publica o guarda.");
        await app.WaitForShutdownAsync();
    }
}
catch (Exception error)
{
    // No volcar excepciones, SQL ni objetos de configuracion que puedan contener secretos.
    Console.Error.WriteLine("Fallo de comprobacion local. Tipo: " + error.GetType().Name +
        (error is MySqlException mysql ? $"; codigo MySQL: {mysql.Number}" : "") + ". Sin reintentos ni escrituras.");
    Environment.ExitCode = 1;
}
finally { if (app is not null) await app.DisposeAsync(); }

static async Task<Dictionary<string, string>> Huella(MySqlConnection conexion)
{
    const string tablas = "ROL,USUARIO,CATEGORIAS,PROVEEDOR,EMPAQUE,PRODUCTO,LOTE,HISTORIAL_PRECIO,CLIENTE,VENTA,DETALLE_VENTA";
    await using var comando = new MySqlCommand("CHECKSUM TABLE " + tablas + " EXTENDED;", conexion);
    await using var lector = await comando.ExecuteReaderAsync();
    var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
    while (await lector.ReadAsync())
    {
        if (lector.IsDBNull(1)) throw new InvalidOperationException("No se pudo comprobar checksum de una tabla.");
        resultado.Add(lector.GetString(0), Convert.ToString(lector.GetValue(1), CultureInfo.InvariantCulture)!);
    }
    if (resultado.Count != 11) throw new InvalidOperationException("Snapshot incompleto.");
    return resultado;
}
