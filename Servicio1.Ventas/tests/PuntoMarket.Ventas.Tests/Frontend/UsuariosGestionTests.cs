using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PuntoMarket.Ventas.Frontend;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;
public sealed class UsuariosGestionTests
{
    [Theory]
    [InlineData("Administrador",200)] [InlineData("Cajero",403)]
    public async Task Listado_y_alta_solo_para_Administrador(string rol,int esperado)
    {
        await using var h=await Host.Crear(rol);using var c=h.Cliente();await h.Login(c);
        var index=await c.GetAsync("/Users");Assert.Equal((HttpStatusCode)esperado,index.StatusCode);
        var crear=await c.GetAsync("/Users/Crear");Assert.Equal((HttpStatusCode)esperado,crear.StatusCode);
        var html=await index.Content.ReadAsStringAsync();
        if(rol=="Administrador") {
            Assert.Contains("href=\"/Users\"",html);Assert.Contains("pendiente@example.invalid",html);Assert.Contains("PENDIENTE (activaci",html);
            Assert.Contains(h.Api.Llamadas,x=>x.Ruta=="/api/usuarios");Assert.Contains(h.Api.Llamadas,x=>x.Ruta=="/api/usuarios/roles");
            var alta=await crear.Content.ReadAsStringAsync();Assert.Contains("Administrador",alta);Assert.Contains("Cajero",alta);Assert.DoesNotContain("type=\"password\"",alta);
        } else {
            Assert.Empty(h.Api.Llamadas);
            var pagina=await c.GetStringAsync("/Sales/Historial");Assert.DoesNotContain("href=\"/Users\"",pagina);
            var token=Campo(await c.GetStringAsync("/Account/Login"),"__RequestVerificationToken");
            var post=await c.PostAsync("/Users?handler=Reenviar",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",token},{"id","3"},{"role","Administrador"}}));
            Assert.Equal(HttpStatusCode.Forbidden,post.StatusCode);Assert.Empty(h.Api.Llamadas);
        }
        Assert.DoesNotContain(h.Jwt,html);Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
    }
    [Fact]
    public async Task Alta_valida_solo_envia_DTO_sin_password_y_sin_duplicar_POST()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var datos=Alta(await c.GetStringAsync("/Users/Crear"));
        var r=await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(datos));Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        Assert.Equal("/Users",r.Headers.Location!.OriginalString);
        var enviada=h.Api.Llamadas.Single(x=>x.Metodo==HttpMethod.Post);
        using var json=JsonDocument.Parse(enviada.Cuerpo!);
        Assert.Equal(new[]{"apellido","correo","idRol","nombre"},json.RootElement.EnumerateObject().Select(x=>x.Name).Order().ToArray());
        Assert.Equal(2,json.RootElement.GetProperty("idRol").GetInt32());Assert.Equal(h.Jwt,enviada.Token);
        var repetir=await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(datos));Assert.Equal(HttpStatusCode.Conflict,repetir.StatusCode);
        Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Post);
    }
    [Theory]
    [InlineData("Input.Correo","no-correo")] [InlineData("Input.Nombre","")] [InlineData("Input.IdRol","99")]
    public async Task Alta_invalida_no_escribe(string campo,string valor)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var d=Alta(await c.GetStringAsync("/Users/Crear"));d[campo]=valor;
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(d))).StatusCode);
        Assert.DoesNotContain(h.Api.Llamadas,x=>x.Metodo!=HttpMethod.Get);
    }
    [Fact]
    public async Task Todas_las_escrituras_exigen_antiforgery()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        foreach(var ruta in new[]{"/Users/Crear","/Users?handler=Baja","/Users?handler=Reenviar"}) {
            Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync(ruta,new FormUrlEncodedContent(new Dictionary<string,string>{{"id","3"},{"confirmar","true"}}))).StatusCode);
        }
        Assert.Empty(h.Api.Llamadas);
    }
    [Fact]
    public async Task Baja_exige_confirmacion_y_utiliza_DELETE_una_vez()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var pagina=await c.GetStringAsync("/Users?handler=ConfirmarBaja&id=2");Assert.Contains("Confirmar baja l",pagina);
        Assert.DoesNotContain(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Delete);
        var csrf=Campo(pagina,"__RequestVerificationToken");
        var sinConfirmar=await c.PostAsync("/Users?handler=Baja",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"id","2"}}));
        Assert.Equal(HttpStatusCode.BadRequest,sinConfirmar.StatusCode);
        var r=await c.PostAsync("/Users?handler=Baja",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"id","2"},{"confirmar","true"}}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Delete);
        Assert.Contains(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Delete&&x.Ruta=="/api/usuarios/2");
    }
    [Theory]
    [InlineData(1)] [InlineData(5)]
    public async Task No_permite_autobaja_ni_baja_inactiva(int id)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var csrf=Campo(await c.GetStringAsync("/Users"),"__RequestVerificationToken");
        var r=await c.PostAsync("/Users?handler=Baja",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"id",id.ToString()},{"confirmar","true"}}));
        Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.DoesNotContain(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Delete);
    }
    [Theory]
    [InlineData(3,302)] [InlineData(4,302)] [InlineData(2,409)] [InlineData(5,409)]
    public async Task Reenvio_solo_para_cuentas_pendientes(int id,int esperado)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var csrf=Campo(await c.GetStringAsync("/Users"),"__RequestVerificationToken");
        var r=await c.PostAsync("/Users?handler=Reenviar",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"id",id.ToString()}}));
        Assert.Equal((HttpStatusCode)esperado,r.StatusCode);
        Assert.Equal(esperado==302?1:0,h.Api.Llamadas.Count(x=>x.Metodo==HttpMethod.Post));
    }
    [Theory]
    [InlineData(400,400)] [InlineData(401,401)] [InlineData(403,403)] [InlineData(409,409)] [InlineData(500,503)]
    public async Task Alta_traduce_errores_y_no_afirma_envio_de_correo(int remoto,int esperado)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var d=Alta(await c.GetStringAsync("/Users/Crear"));h.Api.CodigoEscritura=remoto;
        var r=await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(d));Assert.Equal((HttpStatusCode)esperado,r.StatusCode);
        var html=await r.Content.ReadAsStringAsync();Assert.DoesNotContain("Se envió",html);Assert.DoesNotContain(ApiRemota.Privado,html);Assert.DoesNotContain(h.Jwt,html);
        Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Post);Assert.DoesNotContain(ApiRemota.Privado,h.Logs.Texto);Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
        var sesionActiva=(await c.GetStringAsync("/Account/Login")).Contains("Sesión activa:");
        Assert.Equal(remoto!=401,sesionActiva);
        if(remoto==500) {
            Assert.Contains("puede haberse",WebUtility.HtmlDecode(html).ToLowerInvariant());Assert.Contains("disabled=",html);
            var nueva=Alta(await c.GetStringAsync("/Users/Crear"));
            Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(nueva))).StatusCode);
            Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Post);
            Assert.Contains("nuevo@example.invalid",await c.GetStringAsync("/Users"));
        }
    }
    [Fact]
    public async Task Fallo_SMTP_reenvio_no_se_reintenta_ni_muestra_exito()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var csrf=Campo(await c.GetStringAsync("/Users"),"__RequestVerificationToken");h.Api.CodigoEscritura=500;
        var r=await c.PostAsync("/Users?handler=Reenviar",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"id","3"}}));
        Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);var html=await r.Content.ReadAsStringAsync();
        Assert.Contains("Puede haberse guardado",WebUtility.HtmlDecode(html));Assert.DoesNotContain(ApiRemota.Privado,html);Assert.DoesNotContain("alert-success",html);
        Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Post);
    }
    [Fact]
    public async Task Listado_401_y_roles_403_conservan_estados_sin_escrituras()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);h.Api.CodigoLectura=403;
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/Users/Crear")).StatusCode);
        h.Api.CodigoLectura=401;Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/Users")).StatusCode);
        Assert.DoesNotContain(h.Api.Llamadas,x=>x.Metodo!=HttpMethod.Get);
    }
    [Theory]
    [InlineData("red")] [InlineData("timeout")]
    public async Task Fallo_de_conexion_o_timeout_bloquea_reenvio_del_alta(string fallo)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        var datos=Alta(await c.GetStringAsync("/Users/Crear"));h.Api.Fallo=fallo;
        var r=await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(datos));Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);
        var html=await r.Content.ReadAsStringAsync();Assert.DoesNotContain(ApiRemota.Privado,html);Assert.Contains("disabled=",html);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync("/Users/Crear",new FormUrlEncodedContent(datos))).StatusCode);
        Assert.Single(h.Api.Llamadas,x=>x.Metodo==HttpMethod.Post);
    }
    [Fact]
    public void Control_de_altas_es_por_sesion_y_evitar_doble_envio_es_atomico()
    {
        using var cache=new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var control=new ControlAltaUsuarios(cache);var vence=DateTimeOffset.UtcNow.AddMinutes(5);
        var solicitud=control.Nueva("sesion-a",vence);
        Assert.False(control.Iniciar("sesion-b",solicitud,"a@example.invalid",vence));
        var aceptadas=0;
        Parallel.For(0,20,_=>{if(control.Iniciar("sesion-a",solicitud,"a@example.invalid",vence))Interlocked.Increment(ref aceptadas);});
        Assert.Equal(1,aceptadas);
        Assert.False(control.Iniciar("sesion-a",control.Nueva("sesion-a",vence)," A@example.invalid ",vence));
        Assert.True(control.Iniciar("sesion-b",control.Nueva("sesion-b",vence),"a@example.invalid",vence));
        control.PermitirCorreccion("sesion-a","a@example.invalid");
        Assert.True(control.Iniciar("sesion-a",control.Nueva("sesion-a",vence),"a@example.invalid",vence));
    }
    private static Dictionary<string,string> Alta(string html)=>new(){{"__RequestVerificationToken",Campo(html,"__RequestVerificationToken")},{"Solicitud",Campo(html,"Solicitud")},{"Input.Nombre","Nuevo"},{"Input.Apellido","Usuario"},{"Input.Correo","nuevo@example.invalid"},{"Input.IdRol","2"}};
    private static string Campo(string html,string campo)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\""+Regex.Escape(campo)+"\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    private sealed class LoginFalso(string jwt) : IUsuariosApiClient { public Task<string> IniciarSesion(string correo,string password,CancellationToken ct)=>Task.FromResult(jwt); }
    private sealed class ApiRemota {
        public const string Privado="DetalleSMTPPrivadoNoPublicar";
        public int CodigoEscritura {get;set;}=200;public int CodigoLectura {get;set;}=200;
        public bool AltaPersistida {get;set;} public string? Fallo {get;set;}
        public List<(HttpMethod Metodo,string Ruta,string? Token,string? Cuerpo)> Llamadas {get;}=[];
    }
    private sealed class Handler(ApiRemota api) : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) {
            var path=r.RequestUri!.AbsolutePath;
            api.Llamadas.Add((r.Method,path,r.Headers.Authorization?.Parameter,r.Content is null?null:await r.Content.ReadAsStringAsync(ct)));
            Assert.Equal("Bearer",r.Headers.Authorization?.Scheme);Assert.Empty(r.RequestUri.Query);
            var escritura=r.Method!=HttpMethod.Get;
            if(escritura&&path=="/api/usuarios")api.AltaPersistida=true; // Simula persistencia previa al fallo SMTP, solo fixture.
            if(escritura&&api.Fallo=="red")throw new HttpRequestException(ApiRemota.Privado);
            if(escritura&&api.Fallo=="timeout")throw new TaskCanceledException(ApiRemota.Privado);
            var codigo=escritura?api.CodigoEscritura:api.CodigoLectura;
            if(codigo!=200)return Respuesta(codigo,ApiRemota.Privado);
            if(path=="/api/usuarios/roles")return Respuesta(200,"[{\"id\":1,\"nombre\":\"Administrador\"},{\"id\":2,\"nombre\":\"Cajero\"}]");
            if(r.Method==HttpMethod.Delete)return new(HttpStatusCode.NoContent);
            if(r.Method==HttpMethod.Post)return Respuesta(path=="/api/usuarios"?201:200,path=="/api/usuarios"?"{\"id\":6}":"{\"reenviado\":true}");
            var usuarios=new List<object>{U(1,"admin@example.invalid","ACTIVO","Administrador"),U(2,"cajero@example.invalid","ACTIVO"),U(3,"pendiente@example.invalid","PENDIENTE"),U(4,"expirado@example.invalid","Expirado",expirado:true),U(5,"inactivo@example.invalid","INACTIVO")};
            if(api.AltaPersistida)usuarios.Add(U(6,"nuevo@example.invalid","PENDIENTE"));
            return Respuesta(200,JsonSerializer.Serialize(new{usuarios}));
        }
        private static object U(int id,string correo,string estado,string rol="Cajero",bool expirado=false)=>new{id,nombre="Fixture",apellido="Usuario",correo,username="fixture"+id,rol,estado,fechaCreacion=new DateTime(2026,1,1),tokenActivacionExpira=(DateTime?)null,fechaActivacion=(DateTime?)null,estaExpirado=expirado};
        private static HttpResponseMessage Respuesta(int codigo,string body)=>new((HttpStatusCode)codigo){Content=new StringContent(body,Encoding.UTF8,"application/json")};
    }
    private sealed class Logs : ILoggerProvider {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _lineas=new();public string Texto=>string.Join("\n",_lineas);
        public ILogger CreateLogger(string nombre)=>new Logger(_lineas);public void Dispose(){}
        private sealed class Logger(System.Collections.Concurrent.ConcurrentQueue<string> lineas) : ILogger {
            public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;public bool IsEnabled(LogLevel level)=>true;
            public void Log<TState>(LogLevel level,EventId id,TState state,Exception? e,Func<TState,Exception?,string> f)=>lineas.Enqueue(f(state,e));
        }
    }
    private sealed class Host(WebApplication app,string jwt,ApiRemota api,Logs logs) : IAsyncDisposable {
        public string Jwt=>jwt;public ApiRemota Api=>api;public Logs Logs=>logs;
        public static async Task<Host> Crear(string rol="Administrador") {
            var clave=Convert.ToHexString(RandomNumberGenerator.GetBytes(48));var jwt=SesionRealPrueba.Token(clave,rol);var api=new ApiRemota();var logs=new Logs();
            var raiz=new DirectoryInfo(AppContext.BaseDirectory);while(raiz is not null&&!Directory.Exists(Path.Combine(raiz.FullName,"frontend","PuntoMarket.Ventas.Frontend")))raiz=raiz.Parent;
            var app=FrontendBootstrap.Crear(["--urls","http://127.0.0.1:0","--environment","Development","--contentRoot",Path.Combine(raiz!.FullName,"frontend","PuntoMarket.Ventas.Frontend")],b=>{b.Logging.ClearProviders();b.Logging.AddProvider(logs);},s=>{
                s.RemoveAll<IUsuariosApiClient>();s.AddSingleton<IUsuariosApiClient>(new LoginFalso(jwt));
                s.RemoveAll<ValidadorTokenUsuarios>();s.AddSingleton(new ValidadorTokenUsuarios(()=>clave,TimeProvider.System));
                s.AddHttpClient<IUsuariosGestionApiClient,UsuariosGestionApiClient>().ConfigurePrimaryHttpMessageHandler(()=>new Handler(api));
            });await app.StartAsync();return new(app,jwt,api,logs);
        }
        public HttpClient Cliente()=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=new(app.Urls.Single())};
        public async Task Login(HttpClient c) {
            var csrf=Campo(await c.GetStringAsync("/Account/Login"),"__RequestVerificationToken");
            var r=await c.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"Input.Correo","fixture@example.invalid"},{"Input.Contrasena","SoloFixtureNoReal!123"}}));Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        }
        public async ValueTask DisposeAsync(){await app.StopAsync();await app.DisposeAsync();}
    }
}
