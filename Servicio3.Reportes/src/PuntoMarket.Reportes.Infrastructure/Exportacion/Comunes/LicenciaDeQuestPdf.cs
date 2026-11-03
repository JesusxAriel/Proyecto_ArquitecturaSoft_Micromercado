using QuestPDF.Infrastructure;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class LicenciaDeQuestPdf
{
    public static void Configurar() => QuestPDF.Settings.License = LicenseType.Community;
}
