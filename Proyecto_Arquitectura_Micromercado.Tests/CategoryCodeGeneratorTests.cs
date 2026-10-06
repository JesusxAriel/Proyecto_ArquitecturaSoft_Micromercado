using Proyecto_Arquitectura_Micromercado.Application.Categories;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CategoryCodeGeneratorTests
{
    private static HashSet<string> Taken(params string[] codes) =>
        new(codes, StringComparer.OrdinalIgnoreCase);

    [Theory]
    [InlineData("Lácteos", "CAT-LAC")]
    [InlineData("Jugos de Frutas", "CAT-JDF")]
    [InlineData("Limpieza del Hogar", "CAT-LDH")]
    [InlineData("Cuidado Personal", "CAT-CPU")]
    [InlineData("Bebidas y Gaseosas", "CAT-BYG")]
    [InlineData("Panadería y Repostería de la Casa", "CAT-PYR")]
    [InlineData("  ñandú  ", "CAT-NAN")]
    [InlineData("Snacks & 2 Galletas", "CAT-SGN")]
    public void Genera_la_base_sin_tildes_espacios_ni_numeros(string name, string expected)
    {
        Assert.Equal(expected, CategoryCodeGenerator.Generate(name, Taken()));
    }

    [Fact]
    public void Es_deterministico()
    {
        var taken = Taken("CAT-LAC");

        Assert.Equal(
            CategoryCodeGenerator.Generate("Lacticinios", taken),
            CategoryCodeGenerator.Generate("Lacticinios", taken));
    }

    [Fact]
    public void Colision_usa_primera_letra_y_consonantes_siguientes()
    {
        // "Lacticinios" daría CAT-LAC igual que "Lácteos": se usa L + consonantes C, T.
        Assert.Equal("CAT-LCT", CategoryCodeGenerator.Generate("Lacticinios", Taken("CAT-LAC")));
    }

    [Fact]
    public void Colision_en_cadena_avanza_a_la_siguiente_combinacion()
    {
        var taken = Taken("CAT-LAC", "CAT-LCT", "CAT-LCC");

        Assert.Equal("CAT-LCN", CategoryCodeGenerator.Generate("Lacticinios", taken));
    }

    [Fact]
    public void Comparacion_de_codigos_ocupados_no_distingue_mayusculas()
    {
        Assert.Equal("CAT-LCT", CategoryCodeGenerator.Generate("Lacticinios", Taken("cat-lac")));
    }

    [Fact]
    public void Nunca_agrega_numeros_ni_mas_caracteres()
    {
        var taken = Taken("CAT-LAC");

        for (var i = 0; i < 30; i++)
        {
            var code = CategoryCodeGenerator.Generate("Lacticinios", taken);

            Assert.NotNull(code);
            Assert.Matches("^CAT-[A-Z]{3}$", code);
            Assert.True(taken.Add(code!), "Se repitió un código ya ocupado.");
        }
    }

    [Fact]
    public void Cuando_se_agotan_las_combinaciones_devuelve_null()
    {
        // "Sal" solo admite una combinación de 3 letras.
        Assert.Equal("CAT-SAL", CategoryCodeGenerator.Generate("Sal", Taken()));
        Assert.Null(CategoryCodeGenerator.Generate("Sal", Taken("CAT-SAL")));
    }

    [Fact]
    public void Agota_todas_las_combinaciones_de_un_nombre_corto()
    {
        // "Pasta" -> subsecuencias de 3 letras de P,A,S,T,A: PAS, PAT, PAA, PST, PSA, PTA, AST, ASA, ATA, STA.
        var taken = Taken();
        var generated = new List<string>();

        string? code;
        while ((code = CategoryCodeGenerator.Generate("Pasta", taken)) is not null)
        {
            generated.Add(code);
            taken.Add(code);
        }

        Assert.Equal(10, generated.Count);
        Assert.Equal(generated.Count, generated.Distinct().Count());
        Assert.All(generated, c => Assert.Matches("^CAT-[A-Z]{3}$", c));
    }

    [Theory]
    [InlineData("Té", false)]
    [InlineData("12 34", false)]
    [InlineData("", false)]
    [InlineData("Sal", true)]
    [InlineData("A B C", true)]
    public void Detecta_nombres_sin_letras_suficientes(string name, bool expected)
    {
        Assert.Equal(expected, CategoryCodeGenerator.HasEnoughLetters(name));
        Assert.Equal(expected, CategoryCodeGenerator.Generate(name, Taken()) is not null);
    }
}
