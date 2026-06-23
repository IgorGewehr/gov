using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>
/// Resolve o <see cref="IColetorRep"/> (driver) da marca/fabricante de um REP. Cada driver vive na
/// Infrastructure (ACL); esta fabrica seleciona o certo por <see cref="MarcaRep"/> sem acoplar os
/// handlers as implementacoes concretas.
/// </summary>
public interface IColetorRepFactory
{
    /// <summary>Obtem o driver da marca informada.</summary>
    /// <param name="marca">Marca/fabricante do REP.</param>
    /// <returns>O <see cref="IColetorRep"/> correspondente.</returns>
    /// <exception cref="InvalidOperationException">Se nao houver driver registrado para a marca.</exception>
    IColetorRep Resolver(MarcaRep marca);
}
