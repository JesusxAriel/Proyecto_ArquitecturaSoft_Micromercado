namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

// Código que se asignaría a un nombre ahora mismo. Code es null si todavía no se puede generar;
// Message explica por qué cuando no quedan combinaciones disponibles.
public sealed record CategoryCodePreview(string? Code, string? Message);
