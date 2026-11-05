using System.Text;
using System.Net.NetworkInformation;
using MySql.Data.MySqlClient;
using PuntoMarket.Reportes.Api;

// Solo consume archivos externos existentes. No crea claves, cuentas ni configuración privada.
WebApplication? app = null;
try
{
    var claveRuta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "puntomarket-demo-secrets", "jwt-demo-key.txt");
    if (!File.Exists(claveRuta)) throw new ConfiguracionException("Falta la clave externa existente de Ventas Demo. Solicite instrucciones; no se generó ninguna clave.");
    var clave = await File.ReadAllTextAsync(claveRuta);
    if (Encoding.UTF8.GetByteCount(clave) < 32) throw new ConfiguracionException("La clave Demo existente no es válida.");
    var conexionRuta = Environment.GetEnvironmentVariable("REPORTES_DEMO_CONNECTION_FILE");
    if (string.IsNullOrWhiteSpace(conexionRuta) || !Path.IsPathFullyQualified(conexionRuta) || !File.Exists(conexionRuta))
        throw new ConfiguracionException("Falta REPORTES_DEMO_CONNECTION_FILE: indique un archivo privado externo con una conexión MySQL de solo lectura y TLS. No se reutilizará la cuenta de Ventas ni se crearán usuarios.");
    var raiz = new DirectoryInfo(AppContext.BaseDirectory);
    while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName, "src", "PuntoMarket.Reportes.Api"))) raiz = raiz.Parent;
    if (raiz is null) throw new ConfiguracionException("No se encontró Servicio3.Reportes.");
    var archivo = Path.GetFullPath(conexionRuta);
    var workspace = Directory.GetParent(raiz.FullName)!.FullName + Path.DirectorySeparatorChar;
    if (archivo.StartsWith(workspace, StringComparison.OrdinalIgnoreCase)) throw new ConfiguracionException("El archivo privado de conexión debe estar fuera del workspace.");
    var cadena = new MySqlConnectionStringBuilder(await File.ReadAllTextAsync(archivo));
    if (cadena.Server != "127.0.0.1" || cadena.Port != 3308 || cadena.Database != "puntomarket_ventas_demo_local" ||
        string.IsNullOrWhiteSpace(cadena.UserID) || cadena.UserID.Equals("ventas_demo", StringComparison.OrdinalIgnoreCase) || cadena.UserID.Equals("root", StringComparison.OrdinalIgnoreCase) ||
        cadena.SslMode is not (MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull))
        throw new ConfiguracionException("La conexión debe apuntar a la Demo 127.0.0.1:3308, usar una cuenta independiente y requerir TLS.");
    cadena.ConnectionTimeout = 10;
    await using (var conexion = new MySqlConnection(cadena.ConnectionString))
    {
        await conexion.OpenAsync();
        // SHOW GRANTS es inspección, no administra permisos. Rechaza roles o privilegios de escritura.
        await using var comando = new MySqlCommand("SHOW GRANTS FOR CURRENT_USER", conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        var select = false;
        while (await lector.ReadAsync())
        {
            var grant = lector.GetString(0);
            if (grant.StartsWith("GRANT USAGE ON ", StringComparison.OrdinalIgnoreCase)) continue;
            if (!grant.StartsWith("GRANT SELECT ON ", StringComparison.OrdinalIgnoreCase) || grant.Contains("WITH GRANT OPTION", StringComparison.OrdinalIgnoreCase))
                throw new ConfiguracionException("La cuenta tiene roles o permisos adicionales. Se requiere una cuenta con SELECT únicamente; solicite autorización antes de administrar usuarios.");
            select = true;
        }
        if (!select) throw new ConfiguracionException("La cuenta no tiene un permiso SELECT verificable.");
    }
    if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(p => p.Port == 5012)) throw new ConfiguracionException("El puerto 5012 está ocupado; no se reemplaza el proceso.");
    app = ReportesBootstrap.Crear(["--contentRoot", Path.Combine(raiz.FullName, "src", "PuntoMarket.Reportes.Api"), "--environment", "LocalDemo"], builder => {
        builder.WebHost.UseUrls("http://localhost:5012");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {
            ["Jwt:Key"] = clave, ["Jwt:Issuer"] = "PuntoMarket.DemoLocal", ["Jwt:Audience"] = "PuntoMarket.Ventas.DemoLocal",
            ["ConnectionStrings:MySql"] = cadena.ConnectionString
        });
        builder.Logging.ClearProviders();
    });
    await app.StartAsync();
    Console.WriteLine("Reportes activo en http://localhost:5012. JWT habilitado; cuenta independiente con SELECT y TLS verificados. Sin secretos en salida.");
    await app.WaitForShutdownAsync();
}
catch (ConfiguracionException e) { Console.Error.WriteLine(e.Message); Environment.ExitCode = 1; }
catch (Exception e) { Console.Error.WriteLine("No se pudo iniciar Reportes. Tipo: " + e.GetType().Name + ". Revise la conexión privada y la disponibilidad MySQL; no se imprimen detalles sensibles."); Environment.ExitCode = 1; }
finally { if (app is not null) await app.DisposeAsync(); }
sealed class ConfiguracionException(string mensaje) : Exception(mensaje);
