using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Configuration;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;

public sealed class UsuariosEtapa1Tests
{
    private readonly string _clave = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
    private readonly Reloj _reloj = new();
    private ValidadorTokenUsuarios Validador() => new(() => _clave, _reloj);
    private string Token(string? clave = null, string issuer = "PuntoMarket.DemoLocal", string audience = "PuntoMarket.Ventas.DemoLocal", string algoritmo = SecurityAlgorithms.HmacSha256,
        string rol = "Cajero", string cambiar = "false", DateTimeOffset? nbf = null, DateTimeOffset? exp = null, bool duplicarRol = false, bool omitirNbf = false, bool omitirExp = false)
    {
        var claims = new List<Claim> { new("sub","2"), new("unique_name","cajero1"), new("role",rol), new("debeCambiarPassword",cambiar) };
        if (duplicarRol) claims.Add(new("role","Administrador"));
        var jwt = new JwtSecurityToken(issuer,audience,claims,
            (nbf ?? _reloj.Ahora.AddSeconds(-1)).UtcDateTime,(exp ?? _reloj.Ahora.AddMinutes(5)).UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave ?? _clave)),algoritmo));
        if (omitirNbf) jwt.Payload.Remove("nbf");
        if (omitirExp) jwt.Payload.Remove("exp");
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
    [Theory]
    [InlineData("Cajero",false)] [InlineData("Administrador",true)]
    public void Valida_y_extrae_identidad_solo_despues_de_la_firma(string rol, bool cambiar)
    {
        var identidad = Validador().Validar(Token(rol:rol,cambiar:cambiar.ToString()));
        Assert.Equal(2,identidad.Id); Assert.Equal("cajero1",identidad.Usuario); Assert.Equal(rol,identidad.Rol);
        Assert.Equal(cambiar,identidad.DebeCambiarPassword); Assert.Equal(_reloj.Ahora.AddMinutes(5),identidad.VenceEn);
    }
    [Theory]
    [InlineData("firma")] [InlineData("issuer")] [InlineData("audience")] [InlineData("algoritmo")]
    [InlineData("expirado")] [InlineData("futuro")] [InlineData("sin-nbf")] [InlineData("sin-exp")]
    [InlineData("rol")] [InlineData("booleano")] [InlineData("duplicado")] [InlineData("malformado")]
    public void Rechaza_tokens_no_confiables_sin_publicar_su_contenido(string caso)
    {
        var token = caso switch {
            "firma" => Token(clave:Convert.ToHexString(RandomNumberGenerator.GetBytes(48))),
            "issuer" => Token(issuer:"otro"), "audience" => Token(audience:"otra"),
            "algoritmo" => Token(algoritmo:SecurityAlgorithms.HmacSha512),
            "expirado" => Token(nbf:_reloj.Ahora.AddMinutes(-10), exp:_reloj.Ahora.AddSeconds(-1)),
            "futuro" => Token(nbf:_reloj.Ahora.AddMinutes(1)),
            "sin-nbf" => Token(omitirNbf:true), "sin-exp" => Token(omitirExp:true),
            "rol" => Token(rol:"Observador"), "booleano" => Token(cambiar:"desconocido"),
            "duplicado" => Token(duplicarRol:true), _ => "no-es-jwt"
        };
        var error = Assert.Throws<TokenUsuariosInvalidoException>(() => Validador().Validar(token));
        Assert.DoesNotContain(token,error.ToString()); Assert.Null(error.InnerException);
    }
    [Fact]
    public void Sesiones_aisladas_invalidacion_y_vencimiento_exacto_sin_renovar_JWT()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sesiones = new SesionesUsuario(cache,Validador(),_reloj);
        var token = Token();
        var uno=sesiones.Crear(token); var dos=sesiones.Crear(Token(rol:"Administrador"));
        Assert.Matches("^[0-9A-F]{64}$",uno); Assert.NotEqual(uno,dos);
        Assert.Equal(token,sesiones.Token(uno)); Assert.Equal("Administrador",sesiones.Identidad(dos)!.Rol);
        _reloj.Ahora=_reloj.Ahora.AddMinutes(4);
        Assert.Equal(token,sesiones.Token(uno)); // Conserva exactamente el token, nunca lo firma/renueva.
        sesiones.Cerrar(dos); Assert.False(sesiones.Existe(dos)); Assert.True(sesiones.Existe(uno));
        _reloj.Ahora=_reloj.Ahora.AddMinutes(1);
        Assert.Null(sesiones.Token(uno)); Assert.Null(sesiones.Identidad(uno)); Assert.False(sesiones.Existe(uno));
        Assert.Throws<TokenUsuariosInvalidoException>(()=>sesiones.Crear(token));
    }
    [Fact]
    public async Task Registrar_dependencias_no_lee_clave_y_utiliza_unico_esquema_real()
    {
        var servicios = new ServiceCollection();
        servicios.AgregarPreparacionUsuarios();
        using var sp=servicios.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IUsuariosApiClient>());
        Assert.NotNull(sp.GetRequiredService<ValidadorTokenUsuarios>());
        Assert.NotNull(sp.GetRequiredService<SesionesUsuario>());
        var esquemas=await sp.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().GetAllSchemesAsync();
        Assert.Single(esquemas); Assert.Equal(AccesoUsuariosPreparado.Esquema,esquemas.Single().Name);
    }
    [Fact]
    public async Task Cliente_envia_credenciales_solo_en_POST_y_devuelve_token()
    {
        var token=Token();
        using var http = new HttpClient(new Handler(async (r,ct)=> {
            Assert.Equal(HttpMethod.Post,r.Method); Assert.Equal("/api/autenticacion/login",r.RequestUri!.AbsolutePath);
            Assert.Equal("",r.RequestUri.Query); Assert.Null(r.Headers.Authorization);
            var texto=await r.Content!.ReadAsStringAsync(ct);
            using var json=System.Text.Json.JsonDocument.Parse(texto);
            Assert.Equal("prueba@example.invalid",json.RootElement.GetProperty("correo").GetString());
            Assert.Equal("solo-simulado",json.RootElement.GetProperty("contrasena").GetString());
            return Respuesta(200,"{\"token\":\""+token+"\"}");
        })) { BaseAddress=new("http://127.0.0.1:5003/") };
        Assert.Equal(token,await new UsuariosApiClient(http).IniciarSesion("prueba@example.invalid","solo-simulado",default));
    }
    [Theory]
    [InlineData(400,400)] [InlineData(401,401)] [InlineData(500,503)] [InlineData(302,503)] [InlineData(403,503)]
    public async Task Cliente_traduce_errores_sin_publicar_cuerpos(int codigo,int esperado)
    {
        using var http=Http((_,_)=>Task.FromResult(Respuesta(codigo,"contenido-privado-simulado")));
        var error=await Assert.ThrowsAsync<UsuariosApiException>(()=>new UsuariosApiClient(http).IniciarSesion("a@b.invalid","solo-simulado",default));
        Assert.Equal(esperado,error.Estado); Assert.DoesNotContain("contenido-privado",error.ToString()); Assert.DoesNotContain("solo-simulado",error.ToString());
    }
    [Theory]
    [InlineData("{")] [InlineData("{}")] [InlineData("[]")] [InlineData("{\"token\":123}")] [InlineData("{\"token\":\" \"}")]
    public async Task Cliente_rechaza_respuesta_200_invalida(string contenido)
    {
        using var http=Http((_,_)=>Task.FromResult(Respuesta(200,contenido)));
        var error=await Assert.ThrowsAsync<UsuariosApiException>(()=>new UsuariosApiClient(http).IniciarSesion("a@b.invalid","solo-simulado",default));
        Assert.Equal(503,error.Estado);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Cliente_traduce_conexion_y_timeout(bool timeout)
    {
        using var http=Http((_,_)=>throw (timeout ? new TaskCanceledException() : new HttpRequestException()));
        var error=await Assert.ThrowsAsync<UsuariosApiException>(()=>new UsuariosApiClient(http).IniciarSesion("a@b.invalid","solo-simulado",default));
        Assert.Equal(503,error.Estado);
    }
    [Fact]
    public async Task Cliente_respeta_cancelacion_del_solicitante()
    {
        using var http=Http((_,ct)=> { ct.ThrowIfCancellationRequested(); return Task.FromResult(Respuesta(200,"{}")); });
        using var cts=new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new UsuariosApiClient(http).IniciarSesion("a@b.invalid","solo-simulado",cts.Token));
    }
    private static HttpResponseMessage Respuesta(int codigo,string contenido) => new((HttpStatusCode)codigo) { Content=new StringContent(contenido,Encoding.UTF8,"application/json") };
    private static HttpClient Http(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> responder) => new(new Handler(responder)) { BaseAddress=new("http://127.0.0.1:5003/") };
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> responder) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>responder(request,ct);
    }
    private sealed class Reloj : TimeProvider {
        public DateTimeOffset Ahora {get;set;}=new(2030,1,1,12,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Ahora;
    }
}
