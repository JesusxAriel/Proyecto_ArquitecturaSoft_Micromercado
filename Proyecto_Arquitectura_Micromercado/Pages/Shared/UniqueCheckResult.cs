using Microsoft.AspNetCore.Mvc;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

public sealed record UniqueCheckResult(bool Duplicate, string? Message)
{
    public static JsonResult ToJson(bool duplicate, string message) =>
        new(new UniqueCheckResult(duplicate, duplicate ? message : null));
}
