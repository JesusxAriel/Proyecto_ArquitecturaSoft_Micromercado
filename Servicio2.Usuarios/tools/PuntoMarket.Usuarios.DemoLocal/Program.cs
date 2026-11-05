using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using PuntoMarket.Usuarios.Api;
using PuntoMarket.Usuarios.Infrastructure.Security;
WebApplication? app = null;
try
{
    var raiz = new DirectoryInfo(AppContext.BaseDirectory);
    while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName, "src", "PuntoMarket.Usuarios.Api"))) raiz = raiz.Parent;
    if (raiz is null) throw new ConfiguracionException("No se encontró Servicio2.Usuarios.");
    var claveRuta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "puntomarket-demo-secrets", "jwt-demo-key.txt");
    if (!File.Exists(claveRuta)) throw new ConfiguracionException("Falta la clave JWT Demo externa existente. No se generó otra clave.");
    var clave = await File.ReadAllTextAsync(claveRuta);
    new JwtEmisorTokenOpciones { Key = clave, Issuer = "PuntoMarket.DemoLocal", Audience = "PuntoMarket.Ventas.DemoLocal", ExpireMinutes = 5 }.Validar();
    var ruta = Environment.GetEnvironmentVariable("USUARIOS_DEMO_CONNECTION_FILE");
    if (string.IsNullOrWhiteSpace(ruta) || !Path.IsPathFullyQualified(ruta) || !File.Exists(ruta))
        throw new ConfiguracionException("Falta USUARIOS_DEMO_CONNECTION_FILE: archivo privado externo con conexión de cuenta exclusiva de Usuarios y TLS. No se crean cuentas ni se reutiliza la cuenta SELECT de Reportes.");
    if (Path.GetFullPath(ruta).StartsWith(raiz.Parent!.FullName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new ConfiguracionException("El archivo de conexión debe estar fuera del workspace.");
    var cadena = new MySqlConnectionStringBuilder(await File.ReadAllTextAsync(ruta));
    if (cadena.Server != "127.0.0.1" || cadena.Port != 3308 || cadena.Database != "puntomarket_ventas_demo_local" ||
        string.IsNullOrWhiteSpace(cadena.UserID) || cadena.UserID.Equals("root", StringComparison.OrdinalIgnoreCase) || cadena.UserID.Equals("ventas_demo", StringComparison.OrdinalIgnoreCase) ||
        cadena.SslMode is not (MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull))
        throw new ConfiguracionException("Conexión incompatible: se requiere cuenta independiente, destino Demo 127.0.0.1:3308 y TLS obligatorio.");
    cadena.ConnectionTimeout = 10;
    if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(p => p.Port == 5003)) throw new ConfiguracionException("Puerto 5003 ocupado. No se sustituye ningún proceso.");
    await using (var conexion = new MySqlConnection(cadena.ConnectionString))
    {
        await conexion.OpenAsync();
        await using (var comando = new MySqlCommand("SELECT DATABASE()", conexion))
            if (Convert.ToString(await comando.ExecuteScalarAsync()) != "puntomarket_ventas_demo_local") throw new ConfiguracionException("Base efectiva inesperada.");
        var usuario = new HashSet<string>(); var rol = new HashSet<string>();
        await using var grants = new MySqlCommand("SHOW GRANTS FOR CURRENT_USER", conexion);
        await using var lector = await grants.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            var grant = lector.GetString(0);
            if (grant.StartsWith("GRANT USAGE ON ", StringComparison.OrdinalIgnoreCase)) continue;
            var match = Regex.Match(grant, @"^GRANT ([A-Z, ]+) ON `puntomarket_ventas_demo_local`\.`(USUARIO|ROL)` TO ", RegexOptions.IgnoreCase);
            if (!match.Success || grant.Contains("WITH GRANT OPTION", StringComparison.OrdinalIgnoreCase)) throw new ConfiguracionException("Permisos excesivos, roles o alcance inesperado. Solo se admiten permisos directos sobre USUARIO y ROL.");
            foreach (var permiso in match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries))
            {
                var permitido = match.Groups[2].Value.Equals("USUARIO", StringComparison.OrdinalIgnoreCase) ? usuario : rol;
                permitido.Add(permiso.ToUpperInvariant());
            }
        }
        if (!usuario.SetEquals(["SELECT", "INSERT", "UPDATE"]) || !rol.SetEquals(["SELECT"])) throw new ConfiguracionException("Se requiere SELECT/INSERT/UPDATE en USUARIO y SELECT en ROL, sin permisos adicionales. No se modificaron permisos.");
    }
    app = UsuariosBootstrap.Crear(["--contentRoot", Path.Combine(raiz.FullName, "src", "PuntoMarket.Usuarios.Api"), "--environment", "LocalDemo"], b => {
        b.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:MySqlConnection"] = cadena.ConnectionString,
            ["Jwt:Key"] = clave, ["Jwt:Issuer"] = "PuntoMarket.DemoLocal", ["Jwt:Audience"] = "PuntoMarket.Ventas.DemoLocal", ["Jwt:ExpireMinutes"] = "5"
        });
        b.Logging.ClearProviders();
    });
    await app.StartAsync();
    Console.WriteLine("Usuarios activo en http://127.0.0.1:5003; Swagger: /swagger. JWT HS256, TLS y permisos mínimos comprobados. SMTP no validado; no se ejecutaron escrituras.");
    await app.WaitForShutdownAsync();
}
catch (ConfiguracionException e) { Console.Error.WriteLine(e.Message); Environment.ExitCode = 1; }
catch (Exception e) { Console.Error.WriteLine("No se pudo iniciar Usuarios Demo. Tipo: " + e.GetType().Name + ". Revise configuración privada y MySQL; detalles sensibles omitidos."); Environment.ExitCode = 1; }
finally { if (app is not null) await app.DisposeAsync(); }
sealed class ConfiguracionException(string mensaje) : Exception(mensaje);
