namespace Proyecto_Arquitectura_Micromercado.Pages.Shared;

// Datos del parcial _SearchBox (buscador de los listados paginados).
public sealed record SearchBoxModel(string Label, string Placeholder, string? Query, int PageSize);
