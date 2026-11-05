using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Ventas.Frontend;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class AccountUsuariosTests
{
    private const string Password="PasswordSoloSimulado123!";
    [Theory]
    [InlineData("Administrador")] [InlineData("Cajero")]
    public async Task Login_crea_ticket_real_sin_secretos_y_permite_acceso_operativo(string rol)
    {
        await using var h=await Host.Crear(rol:rol);
        using var c=h.Cliente();
        var r=await h.Login(c);
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
        Assert.Equal("/Sales",r.Headers.Location!.OriginalString);
        var ticket=h.Ticket(r);
        Assert.True(ticket.Principal.IsInRole(rol));
        Assert.Equal(h.Reloj.Ahora.AddMinutes(5),ticket.Properties.ExpiresUtc);
        Assert.False(ticket.Properties.AllowRefresh); Assert.False(ticket.Properties.IsPersistent);
        Assert.Empty(ticket.Properties.GetTokens());
        Assert.DoesNotContain(h.Jwt,string.Join(";",ticket.Properties.Items.Select(x=>x.Key+"="+x.Value)));
        Assert.DoesNotContain(Password,string.Join(";",ticket.Properties.Items.Select(x=>x.Key+"="+x.Value)));
        Assert.Contains(r.Headers.GetValues("Set-Cookie"),x=>x.StartsWith(AccesoUsuariosPreparado.Cookie+"=")&&x.Contains("httponly",StringComparison.OrdinalIgnoreCase)&&x.Contains("samesite=strict",StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(h.Jwt,ticket.Principal.Claims.Select(x=>x.Value));
        Assert.DoesNotContain(Password,string.Join(";",r.Headers.GetValues("Set-Cookie")));
        var html=await c.GetStringAsync("/Account/Login");
        Assert.Contains("Sesión activa:",html); Assert.Contains(rol,html);
        Assert.DoesNotContain(Password,html); Assert.DoesNotContain(h.Jwt,html);
        Assert.DoesNotContain("localStorage",html); Assert.DoesNotContain("sessionStorage",html);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/Sales/Historial")).StatusCode);
        Assert.DoesNotContain(Password,h.Logs.Texto); Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
        Assert.Equal(1,h.Api.Llamadas);
    }
    [Theory]
    [InlineData("credenciales",401)] [InlineData("token-invalido",401)] [InlineData("expirado",401)] [InlineData("password-pendiente",403)]
    public async Task Rechaza_login_invalido_sin_cookie_ni_password_republicada(string modo,int esperado)
    {
        await using var h=await Host.Crear(modo:modo); using var c=h.Cliente();
        var r=await h.Login(c); Assert.Equal((HttpStatusCode)esperado,r.StatusCode);
        var html=await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Password,html); Assert.DoesNotContain(h.Jwt,html);
        Assert.DoesNotContain(Password,h.Logs.Texto); Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
        Assert.DoesNotContain(r.Headers.GetValues("Set-Cookie"),x=>x.StartsWith(AccesoUsuariosPreparado.Cookie+"=")&&!x.StartsWith(AccesoUsuariosPreparado.Cookie+"=;"));
        Assert.DoesNotContain("Sesión activa:",await c.GetStringAsync("/Account/Login"));
        if(modo=="password-pendiente") Assert.Contains("Debe cambiar su contraseña",WebUtility.HtmlDecode(html));
    }
    [Fact]
    public async Task Login_invalido_no_invoca_API_ni_rellena_password()
    {
        await using var h=await Host.Crear(); using var c=h.Cliente();
        var r=await h.Login(c,correo:"no-es-correo");
        Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode); Assert.Equal(0,h.Api.Llamadas);
        Assert.DoesNotContain(Password,await r.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData("https://example.invalid/")] [InlineData("//example.invalid/")] [InlineData("/\\example.invalid/")]
    public async Task ReturnUrl_externo_se_rechaza_antes_del_login(string destino)
    {
        await using var h=await Host.Crear(); using var c=h.Cliente();
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/Account/Login?ReturnUrl="+Uri.EscapeDataString(destino))).StatusCode);
        var r=await h.Login(c,retorno:destino);
        Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode); Assert.Equal(0,h.Api.Llamadas);
    }
    [Fact]
    public async Task ReturnUrl_local_se_conserva_y_rol_no_es_seleccionable()
    {
        await using var h=await Host.Crear(rol:"Cajero"); using var c=h.Cliente();
        var r=await h.Login(c,retorno:"/Account/Logout");
        Assert.Equal("/Account/Logout",r.Headers.Location!.OriginalString);
        Assert.True(h.Ticket(r).Principal.IsInRole("Cajero"));
    }
    [Fact]
    public async Task Login_y_logout_exigen_antiforgery_y_logout_invalida_servidor()
    {
        await using var h=await Host.Crear(); using var c=h.Cliente();
        var mal=await c.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Input.Correo","test@example.invalid"},{"Input.Contrasena",Password}}));
        Assert.Equal(HttpStatusCode.BadRequest,mal.StatusCode); Assert.Equal(0,h.Api.Llamadas);
        var login=await h.Login(c); var ticket=h.Ticket(login);
        var id=ticket.Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value;
        var sesiones=h.App.Services.GetRequiredService<SesionesUsuario>(); Assert.True(sesiones.Existe(id));
        var pagina=await c.GetStringAsync("/Account/Logout"); Assert.True(sesiones.Existe(id)); // GET no cierra sesión.
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/Account/Logout",new FormUrlEncodedContent([]))).StatusCode);
        Assert.True(sesiones.Existe(id));
        var r=await c.PostAsync("/Account/Logout",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(pagina)}}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode); Assert.Equal("/Account/Login",r.Headers.Location!.OriginalString);
        Assert.False(sesiones.Existe(id));
        Assert.Contains(r.Headers.GetValues("Set-Cookie"),x=>x.StartsWith(AccesoUsuariosPreparado.Cookie+"=;"));
        Assert.DoesNotContain("Sesión activa:",await c.GetStringAsync("/Account/Login"));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Cookie_se_rechaza_si_sesion_invalida_o_vencida(bool vencer)
    {
        await using var h=await Host.Crear(); using var c=h.Cliente();
        var r=await h.Login(c); var ticket=h.Ticket(r);
        if(vencer) h.Reloj.Ahora=h.Reloj.Ahora.AddMinutes(5);
        else h.App.Services.GetRequiredService<SesionesUsuario>().Cerrar(ticket.Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value);
        Assert.DoesNotContain("Sesión activa:",await c.GetStringAsync("/Account/Login"));
    }
    [Theory]
    [InlineData("sesion")] [InlineData("rol")] [InlineData("expiracion")]
    public async Task Ticket_protegido_incoherente_con_sesion_real_se_rechaza(string modo)
    {
        await using var h=await Host.Crear(); using var c=h.Cliente();
        var r=await h.Login(c); var t=h.Ticket(r);
        var identidad=(ClaimsIdentity)t.Principal.Identity!;
        if(modo=="sesion") { identidad.RemoveClaim(identidad.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!); identidad.AddClaim(new(AccesoUsuariosPreparado.ClaimSesion,new string('A',64))); }
        if(modo=="rol") { identidad.RemoveClaim(identidad.FindFirst(ClaimTypes.Role)!); identidad.AddClaim(new(ClaimTypes.Role,"Observador")); }
        if(modo=="expiracion") t.Properties.ExpiresUtc=h.Reloj.Ahora.AddHours(1);
        var valor=h.Opciones.TicketDataFormat.Protect(t);
        using var otro=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=c.BaseAddress};
        otro.DefaultRequestHeaders.Add("Cookie",AccesoUsuariosPreparado.Cookie+"="+valor);
        Assert.DoesNotContain("Sesión activa:",await otro.GetStringAsync("/Account/Login"));
    }
    [Fact]
    public async Task Logout_no_recurre_a_cookie_Demo_antigua()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        h.Cookies.Add(c.BaseAddress!,new Cookie("PuntoMarket.Demo.Session","cookie-antigua","/"));
        var pagina=await c.GetStringAsync("/Account/Logout");
        await c.PostAsync("/Account/Logout",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(pagina)}}));
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
        Assert.Empty(h.Remoto.Llamadas);
    }

    [Theory]
    [InlineData("Administrador",true)] [InlineData("Cajero",false)]
    public async Task Sesion_real_consume_Ventas_y_aplica_rol_en_Reportes(string rol,bool reportes)
    {
        await using var h=await Host.Crear(rol:rol); using var c=h.Cliente(); await h.Login(c);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
        Assert.All(h.Remoto.Llamadas,x=>Assert.Equal(h.Jwt,x.Token));
        var pagina=await c.GetAsync("/Reportes/Ventas");
        Assert.Equal(reportes?HttpStatusCode.OK:HttpStatusCode.Forbidden,pagina.StatusCode);
        var html=await pagina.Content.ReadAsStringAsync(); Assert.DoesNotContain(h.Jwt,html); Assert.DoesNotContain(Password,html);
        if(reportes) {
            Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/Reportes/Categorias?handler=Excel&Filtro.Top=1")).StatusCode);
            Assert.Contains(h.Remoto.Llamadas,x=>x.Url.Contains("formato=xlsx")&&x.Url.Contains("top=1"));
        }
        Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
    }
    [Fact]
    public async Task Cookie_Demo_antigua_se_ignora_junto_a_Cajero_real_y_no_hay_fallback_al_vencer()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();await h.Login(c);
        h.Cookies.Add(c.BaseAddress!,new Cookie("PuntoMarket.Demo.Session","Administrador-demo-antiguo","/"));
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/Reportes/Ventas")).StatusCode);
        Assert.Empty(h.Remoto.Llamadas);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
        Assert.Equal(h.Jwt,h.Remoto.Llamadas.Single().Token);
        h.Reloj.Ahora=h.Reloj.Ahora.AddMinutes(5);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
        Assert.Single(h.Remoto.Llamadas);
    }

    [Theory]
    [InlineData(401)] [InlineData(403)]
    public async Task Error_remoto_real_invalida_solo_401_y_no_reintenta_POST(int codigo)
    {
        await using var h=await Host.Crear(rol:"Administrador"); using var c=h.Cliente();
        var login=await h.Login(c); var t=h.Ticket(login); var id=t.Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value;
        var html=await c.GetStringAsync("/Sales/Historial"); h.Remoto.Codigo=codigo;
        using var req=new HttpRequestMessage(HttpMethod.Post,"/Sales/Historial?handler=AnularVenta") { Content=new StringContent("{\"idVenta\":1,\"motivo\":\"Solo fixture\"}",Encoding.UTF8,"application/json") };
        req.Headers.Add("RequestVerificationToken",Csrf(html));
        var r=await c.SendAsync(req); Assert.Equal((HttpStatusCode)codigo,r.StatusCode); Assert.Single(h.Remoto.Llamadas);
        Assert.Equal(h.Jwt,h.Remoto.Llamadas.Single().Token);
        Assert.Equal(codigo!=401,h.App.Services.GetRequiredService<SesionesUsuario>().Existe(id));
        Assert.DoesNotContain(h.Jwt,await r.Content.ReadAsStringAsync()); Assert.DoesNotContain(h.Jwt,h.Logs.Texto);
    }
    [Theory]
    [InlineData(401)] [InlineData(403)]
    public async Task Descarga_Reportes_conserva_estado_y_sesion_correspondiente(int codigo)
    {
        await using var h=await Host.Crear(rol:"Administrador"); using var c=h.Cliente();
        var login=await h.Login(c); var id=h.Ticket(login).Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value;
        h.Remoto.Codigo=codigo;
        var r=await c.GetAsync("/Reportes/Ventas?handler=Pdf&Filtro.Tope=25&Filtro.Estado=COMPLETADA");
        Assert.Equal((HttpStatusCode)codigo,r.StatusCode); Assert.Single(h.Remoto.Llamadas);
        Assert.Equal(codigo!=401,h.App.Services.GetRequiredService<SesionesUsuario>().Existe(id));
        Assert.Contains("tope=25",h.Remoto.Llamadas.Single().Url);
        Assert.DoesNotContain(h.Jwt,await r.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Cookies_y_sesiones_de_usuarios_distintos_no_se_invalidan_entre_si()
    {
        await using var h=await Host.Crear();
        using var a=h.Cliente(); var ra=await h.Login(a); var ia=h.Ticket(ra).Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value;
        // Otro navegador/cookie jar independiente.
        using var b=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=a.BaseAddress};
        h.Api.Token=h.TokenParaUsuario(3,"otroCajero");
        var rb=await h.Login(b); var ib=h.Ticket(rb).Principal.FindFirst(AccesoUsuariosPreparado.ClaimSesion)!.Value;
        Assert.NotEqual(ia,ib);
        Assert.NotEqual(h.Ticket(ra).Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value,h.Ticket(rb).Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        h.Remoto.Codigo=401;
        await a.GetAsync("/Sales?handler=Precio&id=1");
        var sesiones=h.App.Services.GetRequiredService<SesionesUsuario>(); Assert.False(sesiones.Existe(ia)); Assert.True(sesiones.Existe(ib));
        h.Remoto.Codigo=200; Assert.Equal(HttpStatusCode.OK,(await b.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
    }
    [Theory]
    [InlineData("Cajero")] [InlineData("Administrador")]
    public async Task Cookie_Demo_antigua_no_autentica_ningun_rol(string rol)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();
        h.Cookies.Add(c.BaseAddress!,new Cookie("PuntoMarket.Demo.Session",rol,"/"));
        var r=await c.GetAsync("/Sales?handler=Precio&id=1");
        Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode);Assert.Empty(h.Remoto.Llamadas);
        Assert.Contains(r.Headers.GetValues("Set-Cookie"),x=>x.StartsWith("PuntoMarket.Demo.Session=;"));
        Assert.Equal("/Account/Login",r.Headers.GetValues("X-PuntoMarket-Login").Single());
    }

    [Fact]
    public async Task Cookie_real_corrupta_no_recurre_a_Demo()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();
        h.Cookies.Add(c.BaseAddress!,new Cookie("PuntoMarket.Demo.Session","antigua","/"));
        h.Cookies.Add(c.BaseAddress!,new Cookie(AccesoUsuariosPreparado.Cookie,"cookie-corrupta","/"));
        var r=await c.GetAsync("/Sales?handler=Precio&id=1");
        Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode);Assert.Empty(h.Remoto.Llamadas);
        Assert.Equal("/Account/Login",r.Headers.GetValues("X-PuntoMarket-Login").Single());
    }
    [Theory]
    [InlineData("")] [InlineData("?handler=Administrador")] [InlineData("?handler=Salir")]
    public async Task Demo_solo_redirige_y_no_inicia_sesion_aun_con_POST_valido(string handler)
    {
        await using var h=await Host.Crear();using var c=h.Cliente();
        var get=await c.GetAsync("/Demo");Assert.Equal(HttpStatusCode.Redirect,get.StatusCode);Assert.Equal("/Account/Login",get.Headers.Location!.OriginalString);
        var html=await c.GetStringAsync("/Account/Login");Assert.DoesNotContain("Entrar como",html);
        var post=await c.PostAsync("/Demo"+handler,new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(html)},{"role","Administrador"},{"id","1"}}));
        Assert.Equal(HttpStatusCode.Redirect,post.StatusCode);Assert.Equal("/Account/Login",post.Headers.Location!.OriginalString);
        Assert.Equal(0,h.Api.Llamadas);Assert.Empty(h.Remoto.Llamadas);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/Sales?handler=Precio&id=1")).StatusCode);
    }
    [Fact]
    public async Task Frontend_solo_registra_esquema_Usuarios_y_no_contiene_emisor_compilado()
    {
        await using var h=await Host.Crear();using var c=h.Cliente();
        var schemes=h.App.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal(AccesoUsuariosPreparado.Esquema,(await schemes.GetDefaultAuthenticateSchemeAsync())!.Name);
        Assert.Single(await schemes.GetAllSchemesAsync());
        var r=await c.GetAsync("/");Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Equal("/Account/Login",new Uri(c.BaseAddress!,r.Headers.Location!).AbsolutePath);
        var externo=await c.GetAsync("/Demo?ReturnUrl=https%3A%2F%2Fexample.invalid");Assert.Equal(HttpStatusCode.BadRequest,externo.StatusCode);
        var raiz=new DirectoryInfo(AppContext.BaseDirectory);while(raiz is not null&&!Directory.Exists(Path.Combine(raiz.FullName,"frontend","PuntoMarket.Ventas.Frontend")))raiz=raiz.Parent;
        foreach(var f in Directory.EnumerateFiles(Path.Combine(raiz!.FullName,"frontend","PuntoMarket.Ventas.Frontend"),"*.cs",SearchOption.AllDirectories).Where(f=>!f.Contains(Path.DirectorySeparatorChar+"obj"+Path.DirectorySeparatorChar)&&!f.Contains(Path.DirectorySeparatorChar+"bin"+Path.DirectorySeparatorChar))) {
            var codigo=File.ReadAllText(f);Assert.DoesNotContain("WriteToken(",codigo);Assert.DoesNotContain("HMACSHA256.HashData",codigo);
        }
    }
    private sealed class Remoto {
        public int Codigo {get;set;}=200;
        public System.Collections.Concurrent.ConcurrentQueue<(string Url,string? Token,HttpMethod Metodo)> Llamadas {get;}=new();
    }
    private sealed class HandlerRemoto(Remoto estado) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            estado.Llamadas.Enqueue((request.RequestUri!.ToString(),request.Headers.Authorization?.Parameter,request.Method));
            var codigo=estado.Codigo;
            if(codigo!=200) return Task.FromResult(new HttpResponseMessage((HttpStatusCode)codigo){Content=new StringContent("respuesta-privada-simulada-no-json")});
            if(request.RequestUri.Query.Contains("formato=xlsx")) {
                var archivo=new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent([1,2,3])};archivo.Content.Headers.ContentType=new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");return Task.FromResult(archivo);
            }
            var json=request.RequestUri.AbsolutePath.StartsWith("/api/reportes/") ?
                "{\"filas\":[],\"totales\":{\"ventasCompletadas\":83,\"ventasAnuladas\":0,\"montoTotalDeCompletadas\":830,\"promedioPorVenta\":10},\"filtros\":{},\"generadoEn\":\"2026-11-01T00:00:00\",\"generadoPor\":\"fixture\"}" : "{\"id\":1,\"precioVenta\":10}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,Encoding.UTF8,"application/json")});
        }
    }
    private static string Csrf(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private sealed class Reloj : TimeProvider { public DateTimeOffset Ahora {get;set;}=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); public override DateTimeOffset GetUtcNow()=>Ahora; }
    private sealed class ApiFalsa(string token,bool rechazar) : IUsuariosApiClient {
        public string Token {get;set;}=token;
        public int Llamadas {get;private set;}
        public Task<string> IniciarSesion(string correo,string contrasena,CancellationToken ct) { Llamadas++; Assert.Equal(Password,contrasena); if(rechazar) throw new UsuariosApiException(401); return Task.FromResult(Token); }
    }
    private sealed class Logs : ILoggerProvider {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _lineas=new();
        public string Texto=>string.Join("\n",_lineas);
        public ILogger CreateLogger(string nombre)=>new Logger(_lineas);
        public void Dispose() { }
        private sealed class Logger(System.Collections.Concurrent.ConcurrentQueue<string> lineas) : ILogger {
            public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
            public bool IsEnabled(LogLevel level)=>true;
            public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)=>lineas.Enqueue(formatter(state,exception));
        }
    }
    private sealed class Host(WebApplication app,Reloj reloj,ApiFalsa api,string jwt,Logs logs,Remoto remoto,string clave) : IAsyncDisposable {
        public string TokenParaUsuario(int id,string nombre)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("PuntoMarket.DemoLocal","PuntoMarket.Ventas.DemoLocal",
            [new("sub",id.ToString()),new("unique_name",nombre),new("role","Cajero"),new("debeCambiarPassword","false")],
            reloj.Ahora.AddSeconds(-1).UtcDateTime,reloj.Ahora.AddMinutes(5).UtcDateTime,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),SecurityAlgorithms.HmacSha256)));
        public Remoto Remoto=>remoto; public WebApplication App=>app; public Reloj Reloj=>reloj; public ApiFalsa Api=>api; public string Jwt=>jwt; public Logs Logs=>logs;
        public CookieContainer Cookies {get;}=new();
        public CookieAuthenticationOptions Opciones=>app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(AccesoUsuariosPreparado.Esquema);
        public static async Task<Host> Crear(string rol="Cajero",string modo="") {
            var reloj=new Reloj(); var clave=Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
            var token= new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("PuntoMarket.DemoLocal","PuntoMarket.Ventas.DemoLocal",
                [new("sub","2"),new("unique_name","cajeroPrueba"),new("role",rol),new("debeCambiarPassword",modo=="password-pendiente"?"true":"false")],
                reloj.Ahora.AddMinutes(-10).UtcDateTime,(modo=="expirado"?reloj.Ahora.AddSeconds(-1):reloj.Ahora.AddMinutes(5)).UtcDateTime,
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),SecurityAlgorithms.HmacSha256)));
            if(modo=="token-invalido") token="token-invalido-simulado";
            var api=new ApiFalsa(token,modo=="credenciales"); var logs=new Logs(); var remoto=new Remoto();
            var raiz=new DirectoryInfo(AppContext.BaseDirectory); while(raiz is not null&&!Directory.Exists(Path.Combine(raiz.FullName,"frontend","PuntoMarket.Ventas.Frontend"))) raiz=raiz.Parent;
            var app=FrontendBootstrap.Crear(["--urls","http://127.0.0.1:0","--environment","Development","--contentRoot",Path.Combine(raiz!.FullName,"frontend","PuntoMarket.Ventas.Frontend")],b=>{
                b.Logging.ClearProviders(); b.Logging.AddProvider(logs);
                b.Configuration.AddInMemoryCollection(new Dictionary<string,string?>{{"DemoLocal:Enabled","true"},{"DemoLocal:Key",clave},{"DemoLocal:AdminEnabled","true"}});
            },s=>{
                s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(reloj);
                s.RemoveAll<ValidadorTokenUsuarios>();s.AddSingleton(new ValidadorTokenUsuarios(()=>clave,reloj));
                s.RemoveAll<IUsuariosApiClient>();s.AddSingleton<IUsuariosApiClient>(api);
                s.AddHttpClient<IVentasApiClient,VentasApiClient>().ConfigurePrimaryHttpMessageHandler(()=>new HandlerRemoto(remoto));
                s.AddHttpClient<IReportesApiClient,ReportesApiClient>().ConfigurePrimaryHttpMessageHandler(()=>new HandlerRemoto(remoto));
            });
            await app.StartAsync(); return new(app,reloj,api,token,logs,remoto,clave);
        }
        public HttpClient Cliente()=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=Cookies}){BaseAddress=new(app.Urls.Single())};
        public async Task<HttpResponseMessage> Login(HttpClient c,string correo="test@example.invalid",string? retorno=null) {
            var html=await c.GetStringAsync("/Account/Login");
            var datos=new Dictionary<string,string>{{"__RequestVerificationToken",Csrf(html)},{"Input.Correo",correo},{"Input.Contrasena",Password},{"Input.Rol","Administrador"}};
            if(retorno is not null) datos["ReturnUrl"]=retorno;
            return await c.PostAsync("/Account/Login",new FormUrlEncodedContent(datos));
        }
        public AuthenticationTicket Ticket(HttpResponseMessage r) {
            var cookie=r.Headers.GetValues("Set-Cookie").Last(x=>x.StartsWith(AccesoUsuariosPreparado.Cookie+"=")&&!x.StartsWith(AccesoUsuariosPreparado.Cookie+"=;"));
            var valor=cookie.Split(';')[0][(AccesoUsuariosPreparado.Cookie.Length+1)..];
            return Opciones.TicketDataFormat.Unprotect(valor)!;
        }
        public async ValueTask DisposeAsync(){await app.StopAsync();await app.DisposeAsync();}
    }
}
