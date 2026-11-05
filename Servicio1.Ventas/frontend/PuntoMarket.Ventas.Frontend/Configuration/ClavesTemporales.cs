using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace PuntoMarket.Ventas.Frontend.Configuration;

// Anillo efimero en memoria para antiforgery y tickets de sesion local.
internal sealed class ClavesTemporales : IXmlRepository
{
    private readonly ConcurrentBag<XElement> elementos = [];
    public IReadOnlyCollection<XElement> GetAllElements() => elementos.Select(e => new XElement(e)).ToArray();
    public void StoreElement(XElement element, string friendlyName) => elementos.Add(new XElement(element));
}
