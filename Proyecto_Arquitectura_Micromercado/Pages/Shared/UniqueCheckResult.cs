using Microsoft.AspNetCore.Mvc;

namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

// Respuesta común de los endpoints GET "?handler=CheckUnique" de cada listado. La consume el
// script de validación en vivo de site.js ({ "duplicate": bool, "message": string|null }).
public sealed record UniqueCheckResult(bool Duplicate, string? Message)
{
    public static JsonResult ToJson(bool duplicate, string message) =>
        new(new UniqueCheckResult(duplicate, duplicate ? message : null));
}
