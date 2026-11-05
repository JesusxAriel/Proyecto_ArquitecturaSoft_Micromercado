using MySql.Data.MySqlClient;
// Únicamente SELECT y SHOW por TCP Windows. Nunca ejecuta Preparar-CuentaDemo.sql.
try
{
    var raiz = new DirectoryInfo(AppContext.BaseDirectory);
    while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName,"Servicio1.Ventas"))) raiz=raiz.Parent;
    if (raiz is null) throw new InvalidOperationException();
    if (args.Contains("--usuarios", StringComparer.Ordinal))
    {
        var rutaUsuarios=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"puntomarket-demo-secrets","usuarios-demo","usuarios-connection.txt");
        var cs=new MySqlConnectionStringBuilder(await File.ReadAllTextAsync(rutaUsuarios));
        if(cs.Server!="127.0.0.1" || cs.Port!=3308 || cs.Database!="puntomarket_ventas_demo_local" || cs.UserID!="usuarios_demo" ||
           cs.SslMode is not (MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull)) throw new InvalidOperationException();
        await using var cn=new MySqlConnection(cs.ConnectionString);
        await cn.OpenAsync();
        await using(var cmd=new MySqlCommand("SELECT CURRENT_USER(),DATABASE(),CURRENT_ROLE()",cn))
        await using(var rd=await cmd.ExecuteReaderAsync())
        {
            if(!await rd.ReadAsync() || rd.GetString(0)!="usuarios_demo@172.20.0.1" || rd.GetString(1)!="puntomarket_ventas_demo_local" || rd.GetString(2)!="NONE") throw new InvalidOperationException();
            Console.WriteLine("Autenticación Windows: usuarios_demo@172.20.0.1; base correcta; sin roles activos.");
        }
        var tls=false;
        await using(var cmd=new MySqlCommand("SHOW SESSION STATUS WHERE Variable_name IN ('Ssl_version','Ssl_cipher')",cn))
        await using(var rd=await cmd.ExecuteReaderAsync())
        { while(await rd.ReadAsync()) { var valor=rd.GetString(1); if(rd.GetString(0)=="Ssl_cipher") tls=valor.Length>0; Console.WriteLine(rd.GetString(0)+": "+valor); } }
        if(!tls) throw new InvalidOperationException();
        foreach(var tabla in new[]{"USUARIO","ROL"})
        {
            await using var cmd=new MySqlCommand("SELECT COUNT(*) FROM `"+tabla+"`",cn);
            _=await cmd.ExecuteScalarAsync();
            Console.WriteLine("Lectura "+tabla+": OK (sin mostrar registros).");
        }
        var u=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var r=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using(var cmd=new MySqlCommand("SHOW GRANTS FOR CURRENT_USER",cn))
        await using(var rd=await cmd.ExecuteReaderAsync())
        {
            while(await rd.ReadAsync())
            {
                var grant=rd.GetString(0);
                if(grant=="GRANT USAGE ON *.* TO `usuarios_demo`@`172.20.0.1`") continue;
                var m=System.Text.RegularExpressions.Regex.Match(grant,@"^GRANT ([A-Z, ]+) ON `puntomarket_ventas_demo_local`\.`(USUARIO|ROL)` TO `usuarios_demo`@`172\.20\.0\.1`$");
                if(!m.Success) throw new InvalidOperationException();
                foreach(var permiso in m.Groups[1].Value.Split(',',StringSplitOptions.TrimEntries)) (m.Groups[2].Value=="USUARIO"?u:r).Add(permiso);
            }
        }
        if(!u.SetEquals(["SELECT","INSERT","UPDATE"])||!r.SetEquals(["SELECT"])) throw new InvalidOperationException();
        Console.WriteLine("Grants propios exactos: USUARIO SELECT/INSERT/UPDATE; ROL SELECT; sin privilegios administrativos ni GRANT OPTION.");
        return;
    }
    var env=File.ReadAllLines(Path.Combine(raiz.FullName,"Servicio1.Ventas","database","demo-env",".env"))
        .Where(l=>!string.IsNullOrWhiteSpace(l)&&!l.TrimStart().StartsWith('#')).Select(l=>l.Split('=',2)).ToDictionary(p=>p[0],p=>p[1]);
    var archivo=Path.GetFullPath(env["VENTAS_DEMO_PASSWORD_FILE"]);
    var privado=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"puntomarket-demo-secrets")+Path.DirectorySeparatorChar;
    if (!archivo.StartsWith(privado,StringComparison.OrdinalIgnoreCase)||env["VENTAS_DEMO_USER"]!="ventas_demo") throw new InvalidOperationException();
    var cadena=new MySqlConnectionStringBuilder { Server="127.0.0.1",Port=3308,Database="puntomarket_ventas_demo_local",UserID=env["VENTAS_DEMO_USER"],Password=File.ReadAllText(archivo),SslMode=MySqlSslMode.Required,ConnectionTimeout=10 };
    await using var conexion=new MySqlConnection(cadena.ConnectionString);
    await conexion.OpenAsync();
    await using(var comando=new MySqlCommand("SELECT SUBSTRING_INDEX(USER(),'@',-1), DATABASE()",conexion))
    await using(var lector=await comando.ExecuteReaderAsync())
    { while(await lector.ReadAsync()) Console.WriteLine("Origen Windows visto por MySQL: "+lector.GetString(0)+"; base: "+lector.GetString(1)); }
    await using(var comando=new MySqlCommand("SHOW SESSION STATUS WHERE Variable_name IN ('Ssl_version','Ssl_cipher')",conexion))
    await using(var lector=await comando.ExecuteReaderAsync())
    { while(await lector.ReadAsync()) Console.WriteLine(lector.GetString(0)+": "+lector.GetString(1)); }
}
catch(Exception e) { Console.Error.WriteLine("Verificación de conexión no completada. Tipo: "+e.GetType().Name+(e is MySqlException m ? "; código: "+m.Number : "")+". Detalles privados omitidos."); Environment.ExitCode=1; }
