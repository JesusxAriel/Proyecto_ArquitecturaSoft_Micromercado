namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Abstracto del patrón Factory Method.
    //
    // Cumple los dos roles que el GoF le asigna al Creator:
    //   1. Declara el método fábrica CrearRepositorio() como hook abstracto, para que
    //      cada Creador Concreto decida qué implementación construir.
    //   2. Aporta una operación propia, ObtenerRepositorio(), que consume ese hook.
    //      Sin esta operación el Creator sería una simple fábrica, no un Factory Method.
    //
    // ObtenerRepositorio() memoiza el resultado: dentro del mismo Creador, todas las
    // llamadas devuelven la misma instancia. Esto es lo que permite respetar el ciclo
    // de vida Scoped registrado en Program.cs, porque el Creador también es Scoped:
    // un Creador por petición HTTP => un repositorio por petición HTTP.
    public abstract class CreatorRepositorio<TRepositorio> where TRepositorio : class
    {
        private TRepositorio? repositorio;

        // Método fábrica (hook): lo implementa cada Creador Concreto.
        // Es protected porque sólo la operación del Creator debe invocarlo; los clientes
        // usan ObtenerRepositorio() y así nunca pueden saltearse la memoización.
        protected abstract TRepositorio CrearRepositorio();

        // Operación del Creator que usa el producto del método fábrica.
        public TRepositorio ObtenerRepositorio()
        {
            return repositorio ??= CrearRepositorio();
        }
    }
}
