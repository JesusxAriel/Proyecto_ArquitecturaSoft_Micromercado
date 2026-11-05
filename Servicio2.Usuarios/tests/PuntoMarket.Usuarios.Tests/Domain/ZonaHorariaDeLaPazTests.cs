using PuntoMarket.Usuarios.Domain.Services;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class ZonaHorariaDeLaPazTests
{
    [Fact]
    public void La_hora_local_de_La_Paz_esta_cuatro_horas_antes_que_UTC()
    {
        var utc = new DateTime(2026, 11, 5, 9, 12, 0, DateTimeKind.Utc);

        var local = ZonaHorariaDeLaPaz.ALocal(utc);

        Assert.Equal(new DateTime(2026, 11, 5, 5, 12, 0), local);
    }

    [Fact]
    public void Convertir_a_local_y_volver_a_UTC_conserva_el_instante()
    {
        var utc = new DateTime(2026, 1, 1, 2, 30, 0, DateTimeKind.Utc);

        var ida = ZonaHorariaDeLaPaz.AUtc(ZonaHorariaDeLaPaz.ALocal(utc));

        Assert.Equal(utc, ida);
        Assert.Equal(DateTimeKind.Utc, ida.Kind);
    }

    [Fact]
    public void El_vencimiento_se_compara_en_la_misma_zona_en_que_se_guarda()
    {
        var ahoraUtc = new DateTime(2026, 11, 5, 9, 12, 0, DateTimeKind.Utc);
        var venceUtc = ahoraUtc.AddMinutes(10);

        var vencimientoGuardado = ZonaHorariaDeLaPaz.ALocal(venceUtc);
        var ahoraLocal = ZonaHorariaDeLaPaz.ALocal(ahoraUtc);

        Assert.True(vencimientoGuardado > ahoraLocal);
        Assert.True(vencimientoGuardado <= ZonaHorariaDeLaPaz.ALocal(ahoraUtc.AddMinutes(10)));
    }
}
