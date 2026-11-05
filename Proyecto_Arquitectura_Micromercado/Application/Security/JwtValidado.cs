using System.Security.Claims;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public sealed record JwtValidado(ClaimsPrincipal Principal, DateTimeOffset ExpiraUtc);
