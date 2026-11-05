using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace PuntoMarket.Usuarios.Api.Documentation;
public static class DocumentacionUsuarios
{
    public static IServiceCollection AgregarDocumentacionUsuarios(this IServiceCollection servicios)
    {
        servicios.AddEndpointsApiExplorer();
        servicios.AddSwaggerGen(o => {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "PuntoMarket.Usuarios", Version = "v1", Description = "Login, contraseñas y gestión de usuarios. Try it out puede modificar datos y enviar correos reales. No introducir claves de firma. La cookie Razor no autentica esta API." });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
            o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] });
            o.OperationFilter<SeguridadUsuariosFilter>();
        });
        return servicios;
    }
}
public sealed class SeguridadUsuariosFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        var metadatos = contexto.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadatos.OfType<IAllowAnonymous>().Any()) { operacion.Security = []; operacion.Description = "Acceso público. No requiere JWT."; }
        else operacion.Description = metadatos.OfType<IAuthorizeData>().Any(a => a.Roles == "Administrador") ? "Requiere JWT válido y rol Administrador." : "Requiere JWT válido.";
    }
}
