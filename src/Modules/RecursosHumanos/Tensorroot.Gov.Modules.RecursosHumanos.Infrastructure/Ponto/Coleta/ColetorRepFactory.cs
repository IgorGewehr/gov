using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Ponto.Coleta;

/// <summary>
/// Fabrica que resolve o <see cref="IColetorRep"/> da marca, indexando os drivers registrados no DI
/// pela sua <see cref="IColetorRep.Marca"/>. Novos fabricantes entram apenas registrando mais um driver
/// (sem tocar nos handlers). // TODO(prod: SDK proprietario) por marca.
/// </summary>
public sealed class ColetorRepFactory : IColetorRepFactory
{
    private readonly Dictionary<MarcaRep, IColetorRep> _porMarca;

    /// <summary>Indexa os drivers de coleta disponiveis por marca.</summary>
    /// <param name="coletores">Drivers registrados no container.</param>
    public ColetorRepFactory(IEnumerable<IColetorRep> coletores)
    {
        ArgumentNullException.ThrowIfNull(coletores);
        // Ultimo registro vence em caso de marca repetida (permite sobrepor o simulado pelo real em prod).
        var mapa = new Dictionary<MarcaRep, IColetorRep>();
        foreach (var coletor in coletores)
        {
            mapa[coletor.Marca] = coletor;
        }

        _porMarca = mapa;
    }

    /// <inheritdoc />
    public IColetorRep Resolver(MarcaRep marca)
        => _porMarca.TryGetValue(marca, out var coletor)
            ? coletor
            : throw new InvalidOperationException(
                $"Nenhum driver de coleta registrado para a marca '{marca}'. // TODO(prod: SDK proprietario).");
}
