using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuntoMarket.Ventas.Frontend;
using Xunit;

namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class ActivacionFrontendTests
{
    private const string Token = "TokenDeActivacionSoloFixtureNoReal";
    private const string Password = "ClaveSoloFixture!123";
    private static string Campo(string html, string nombre) => WebUtility.HtmlDecode(Regex.Match(html,
        "name=\"" + nombre + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    private static Dictionary<string,string> Formulario(string html, string nueva = Password, string confirmar = Password) => new() {
        ["__RequestVerificationToken"] = Campo(html, "__RequestVerificationToken"),
        ["NuevaContrasena"] = nueva, ["ConfirmarContrasena"] = confirmar
    };
    private static async Task<string> Abrir(HttpClient c)
    {
        var entrada = await c.GetAsync("/Users/Activar?token=" + Token);
        Assert.Equal(HttpStatusCode.Redirect, entrada.StatusCode);
        Assert.Equal("/Users/Activar", entrada.Headers.Location!.OriginalString);
        Assert.DoesNotContain(Token, string.Join(" ", entrada.Headers));
        // La respuesta primero elimina la referencia anterior y luego fija la nueva.
        var correlacion = Microsoft.Net.Http.Headers.SetCookieHeaderValue
            .ParseList(entrada.Headers.GetValues("Set-Cookie").ToList())
            .Single(x => x.Name.Value == "PuntoMarket.Activacion" && x.Value.Length > 0).ToString();
        Assert.Contains("httponly", correlacion.ToLowerInvariant());
        Assert.Contains("samesite=lax", correlacion.ToLowerInvariant());
        Assert.Contains("path=/Users/Activar", correlacion);
        Assert.Contains("max-age=900", correlacion);
        var pagina = await c.GetAsync(entrada.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Equal("no-referrer", pagina.Headers.GetValues("Referrer-Policy").Single());
        var html = await pagina.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Token, html);
        Assert.DoesNotContain("name=\"token\"", html, StringComparison.OrdinalIgnoreCase);
        return html;
    }

    [Fact]
    public async Task Anonimo_activa_por_servidor_sin_JWT_y_confirma_sin_iniciar_sesion()
    {
        await using var host = await Host.Crear(); using var c = host.Cliente();
        var html = await Abrir(c);
        var r = await c.PostAsync("/Users/Activar", new FormUrlEncodedContent(Formulario(html)));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var final = await c.GetStringAsync(r.Headers.Location);
        Assert.Contains("Su cuenta se activó correctamente", final);
        Assert.Contains("/Account/Login", final);
        Assert.DoesNotContain(Password, final);
        Assert.DoesNotContain(Token, final);
        Assert.Equal(1, host.Api.Llamadas);
        Assert.DoesNotContain(Token, host.Logs.Texto);
        Assert.DoesNotContain(Password, host.Logs.Texto);
        Assert.Equal(HttpStatusCode.Redirect, (await c.GetAsync("/Users")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/Users/Activar",new FormUrlEncodedContent(Formulario(html)))).StatusCode);
        Assert.Equal(1, host.Api.Llamadas);
    }

    [Theory]
    [InlineData("cort!1A", "cort!1A")]
    [InlineData("sinmayuscula!123", "sinmayuscula!123")]
    [InlineData("SINMINUSCULA!123", "SINMINUSCULA!123")]
    [InlineData("SinNumeros!", "SinNumeros!")]
    [InlineData("SinSimbolos123", "SinSimbolos123")]
    [InlineData(Password, "NoCoincide!123")]
    public async Task Validacion_no_rellena_contrasenas_ni_consume_enlace(string nueva, string confirmar)
    {
        await using var host = await Host.Crear(); using var c = host.Cliente();
        var html = await Abrir(c);
        var r = await c.PostAsync("/Users/Activar", new FormUrlEncodedContent(Formulario(html,nueva,confirmar)));
        Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);
        var error = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain(nueva,error); Assert.DoesNotContain(confirmar,error);
        Assert.DoesNotContain(Token,error); Assert.Equal(0,host.Api.Llamadas);
        Assert.Contains("name=\"NuevaContrasena\"",error);
        var valido = await c.PostAsync("/Users/Activar", new FormUrlEncodedContent(Formulario(error)));
        Assert.Equal(HttpStatusCode.Redirect,valido.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_y_referencia_privada_aislan_el_enlace()
    {
        await using var host = await Host.Crear(); using var a = host.Cliente(); using var b = host.Cliente();
        var html = await Abrir(a);
        var sinCsrf = Formulario(html); sinCsrf.Remove("__RequestVerificationToken");
        Assert.Equal(HttpStatusCode.BadRequest,(await a.PostAsync("/Users/Activar",new FormUrlEncodedContent(sinCsrf))).StatusCode);
        var sinToken = await b.GetAsync("/Users/Activar");
        Assert.Equal(HttpStatusCode.BadRequest,sinToken.StatusCode);
        var propio = await sinToken.Content.ReadAsStringAsync();
        Assert.DoesNotContain("type=\"password\"",propio);
        Assert.Equal(HttpStatusCode.BadRequest,(await b.PostAsync("/Users/Activar",new FormUrlEncodedContent(Formulario(html)))).StatusCode);
        Assert.Equal(0,host.Api.Llamadas);
    }

    [Theory]
    [InlineData(400,400)] // token inválido/expirado: no mostrar cuerpo remoto
    [InlineData(500,503)]
    [InlineData(409,503)]
    [InlineData(200,503)] // JSON de éxito incorrecto
    [InlineData(0,503)] // conexión
    [InlineData(-1,503)] // timeout
    public async Task Errores_seguros_sin_reintentos_ni_confirmacion_falsa(int remoto,int esperado)
    {
        await using var host = await Host.Crear(); using var c = host.Cliente();
        var html = await Abrir(c); host.Api.Codigo = remoto; host.Api.Malformado = remoto == 200;
        var r = await c.PostAsync("/Users/Activar",new FormUrlEncodedContent(Formulario(html)));
        Assert.Equal(esperado,(int)r.StatusCode);
        var error = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Su cuenta se activó correctamente",error);
        Assert.DoesNotContain(Password,error); Assert.DoesNotContain(Token,error);
        Assert.DoesNotContain(Api.Privado,error);
        Assert.DoesNotContain("type=\"password\"",error);
        Assert.Equal(1,host.Api.Llamadas);
        Assert.DoesNotContain(Token,host.Logs.Texto); Assert.DoesNotContain(Password,host.Logs.Texto);
        Assert.DoesNotContain(Api.Privado,host.Logs.Texto);
    }

    [Fact]
    public async Task URL_configurada_de_Docker_se_utiliza_sin_localhost()
    {
        await using var host = await Host.Crear("http://usuarios:8080/"); using var c = host.Cliente();
        var html = await Abrir(c);
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync("/Users/Activar",new FormUrlEncodedContent(Formulario(html)))).StatusCode);
        Assert.Equal("usuarios",host.Api.Destino!.Host);
        Assert.Equal(8080,host.Api.Destino.Port);
    }

    private sealed class Api : HttpMessageHandler
    {
        public const string Privado = "DetalleRemotoPrivado";
        public int Codigo { get; set; } = 200;
        public bool Malformado { get; set; }
        public int Llamadas { get; private set; }
        public Uri? Destino { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)
        {
            Llamadas++; Destino = r.RequestUri;
            Assert.Equal(HttpMethod.Post,r.Method); Assert.Equal("/api/activacion",r.RequestUri!.AbsolutePath);
            Assert.Empty(r.RequestUri.Query); Assert.Null(r.Headers.Authorization);
            using var json = JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal(3,json.RootElement.EnumerateObject().Count());
            Assert.Equal(Token,json.RootElement.GetProperty("token").GetString());
            Assert.Equal(Password,json.RootElement.GetProperty("nuevaContrasena").GetString());
            Assert.Equal(Password,json.RootElement.GetProperty("confirmarContrasena").GetString());
            if(Codigo==0) throw new HttpRequestException(Privado);
            if(Codigo==-1) throw new TaskCanceledException(Privado);
            return new((HttpStatusCode)Codigo) {
                Content = new StringContent(Codigo==200&&!Malformado?"{\"activada\":true}":Privado,Encoding.UTF8,"application/json")
            };
        }
    }
    private sealed class Logs : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> lineas = new();
        public string Texto => string.Join("\n",lineas);
        public ILogger CreateLogger(string categoria) => new Logger(lineas);
        public void Dispose() { }
        private sealed class Logger(ConcurrentQueue<string> lineas) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState:notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level,EventId id,TState state,Exception? e,Func<TState,Exception?,string> formatter) => lineas.Enqueue(formatter(state,e));
        }
    }
    private sealed class Host(WebApplication app,Api api,Logs logs) : IAsyncDisposable
    {
        public Api Api => api; public Logs Logs => logs;
        public static async Task<Host> Crear(string baseUrl="http://127.0.0.1:5003/")
        {
            var raiz = new DirectoryInfo(AppContext.BaseDirectory);
            while(raiz is not null&&!Directory.Exists(Path.Combine(raiz.FullName,"frontend","PuntoMarket.Ventas.Frontend"))) raiz=raiz.Parent;
            var api = new Api(); var logs = new Logs();
            var app = FrontendBootstrap.Crear(["--urls","http://127.0.0.1:0","--environment","Development","--UsuariosApi:BaseUrl",baseUrl,
                "--contentRoot",Path.Combine(raiz!.FullName,"frontend","PuntoMarket.Ventas.Frontend")],b=>{
                    b.Logging.ClearProviders(); b.Logging.AddProvider(logs);
                },s=>s.AddHttpClient<PuntoMarket.Ventas.Frontend.Clients.IActivacionApiClient,PuntoMarket.Ventas.Frontend.Clients.ActivacionApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(()=>api));
            await app.StartAsync(); return new(app,api,logs);
        }
        public HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect=false,CookieContainer=new CookieContainer() }) { BaseAddress=new(app.Urls.Single()) };
        public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
    }
}

