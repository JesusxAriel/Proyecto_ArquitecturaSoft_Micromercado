namespace Proyecto_Arquitectura_Micromercado.Domain.Stock;

public class StockInsuficienteException : InvalidOperationException
{
    public StockInsuficienteException()
        : base("Stock insuficiente para uno de los productos.")
    {
    }

    public StockInsuficienteException(string message)
        : base(message)
    {
    }
}
