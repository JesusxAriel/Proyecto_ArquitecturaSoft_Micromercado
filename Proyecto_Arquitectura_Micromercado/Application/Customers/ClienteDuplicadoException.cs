namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public sealed class ClienteDuplicadoException() : ArgumentException(
    "Ya existe un cliente activo con ese CI/NIT.");
