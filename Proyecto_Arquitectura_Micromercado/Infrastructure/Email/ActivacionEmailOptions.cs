namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Email;

public sealed class ActivacionEmailOptions
{
    public const string SectionName = "Activacion";

    public string BaseUrl { get; set; } = string.Empty;
}
