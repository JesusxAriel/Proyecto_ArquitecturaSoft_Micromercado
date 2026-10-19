using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

internal sealed class RegistroEspia : ILogger<IndexModel>, ILogger<HistorialModel>
{
    public List<LogLevel> Niveles { get; } = [];
    public List<string> Entradas { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Niveles.Add(logLevel);
        Entradas.Add(formatter(state, exception));
    }
}
