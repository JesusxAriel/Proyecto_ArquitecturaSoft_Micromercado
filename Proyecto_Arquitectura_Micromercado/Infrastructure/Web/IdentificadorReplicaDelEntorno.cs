using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public sealed class IdentificadorReplicaDelEntorno : IIdentificadorReplica
{
    public string Valor { get; } =
        Environment.GetEnvironmentVariable("REPLICA_ID") ?? Environment.MachineName;
}
