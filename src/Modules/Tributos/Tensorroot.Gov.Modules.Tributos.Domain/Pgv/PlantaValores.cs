using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>Identificador forte do agregado <see cref="PlantaValores"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlantaValoresId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlantaValoresId"/>.</returns>
    public static PlantaValoresId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Categoria do fator de correção aplicável ao valor venal (ver M6-DESIGN §1.2).</summary>
public enum TipoFatorPgv
{
    /// <summary>Fator do padrão construtivo (multiplica o VUC).</summary>
    PadraoConstrutivo = 1,

    /// <summary>Fator de depreciação por faixa de idade da construção.</summary>
    Depreciacao = 2,

    /// <summary>Fator de uso/localização (residencial, comercial, etc.).</summary>
    Uso = 3,
}

/// <summary>
/// Planta Genérica de Valores (PGV): instrumento de LEI MUNICIPAL que fixa, por exercício, o valor
/// unitário do m² de terreno (VUT) e de construção (VUC) por zona fiscal, mais os fatores de
/// correção (padrão, depreciação, uso). Versionada por <see cref="Exercicio"/>: o motor de cálculo
/// SEMPRE lê a PGV vigente no exercício do fato gerador, nunca defaults numéricos.
/// Súmula 160/STJ: decreto só corrige por índice oficial; majoração real exige lei — por isso a
/// versão é por exercício. Ver M6-DESIGN §0/§1.2.
/// </summary>
public sealed class PlantaValores : AggregateRoot<PlantaValoresId>, IMustHaveTenant
{
    private readonly List<ValorZona> _zonas = [];
    private readonly List<FatorPgv> _fatores = [];

    private PlantaValores()
    {
    }

    private PlantaValores(PlantaValoresId id, Guid tenantId, int exercicio, string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        FundamentoLegal = fundamentoLegal;
        Vigente = false;
        RaiseDomainEvent(new PlantaValoresCriada(id, tenantId, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício fiscal de vigência (ano).</summary>
    public int Exercicio { get; private set; }

    /// <summary>
    /// Fundamento legal (lei/decreto municipal que institui a PGV deste exercício).
    /// // TODO(validar-oficial): preencher com a lei da PGV de Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Indica se a planta está vigente (publicada e apta a ser usada pelo motor).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Valores unitários (VUT/VUC) por zona fiscal.</summary>
    public IReadOnlyCollection<ValorZona> Zonas => _zonas;

    /// <summary>Fatores de correção (padrão, depreciação, uso).</summary>
    public IReadOnlyCollection<FatorPgv> Fatores => _fatores;

    /// <summary>Cria uma PGV para um exercício (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício fiscal (ano ≥ 1900).</param>
    /// <param name="fundamentoLegal">Lei/decreto municipal que a institui.</param>
    /// <returns>Nova <see cref="PlantaValores"/>.</returns>
    public static PlantaValores Criar(Guid tenantId, int exercicio, string fundamentoLegal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new PlantaValores(PlantaValoresId.New(), tenantId, exercicio, fundamentoLegal.Trim());
    }

    /// <summary>Define (cria ou substitui) o valor unitário de uma zona fiscal.</summary>
    /// <param name="zonaFiscal">Código da zona fiscal.</param>
    /// <param name="valorM2Terreno">Valor unitário do m² de terreno (VUT) em R$.</param>
    /// <param name="valorM2Construcao">Valor unitário do m² de construção (VUC) em R$.</param>
    public void DefinirZona(string zonaFiscal, decimal valorM2Terreno, decimal valorM2Construcao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zonaFiscal);
        ArgumentOutOfRangeException.ThrowIfNegative(valorM2Terreno);
        ArgumentOutOfRangeException.ThrowIfNegative(valorM2Construcao);
        GarantirEditavel();

        var chave = zonaFiscal.Trim().ToUpperInvariant();
        _zonas.RemoveAll(z => z.ZonaFiscal == chave);
        _zonas.Add(ValorZona.Criar(Id, chave, valorM2Terreno, valorM2Construcao));
    }

    /// <summary>Define (cria ou substitui) um fator de correção identificado por categoria e chave.</summary>
    /// <param name="tipo">Categoria do fator.</param>
    /// <param name="chave">Chave do fator (ex.: código do padrão, faixa de idade, uso).</param>
    /// <param name="multiplicador">Multiplicador (ex.: 1.20 = +20%).</param>
    public void DefinirFator(TipoFatorPgv tipo, string chave, decimal multiplicador)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiplicador);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de fator inválido.");
        }

        GarantirEditavel();
        var k = chave.Trim().ToUpperInvariant();
        _fatores.RemoveAll(f => f.Tipo == tipo && f.Chave == k);
        _fatores.Add(FatorPgv.Criar(Id, tipo, k, multiplicador));
    }

    /// <summary>Publica a PGV, tornando-a vigente e imutável (apta para o motor de cálculo).</summary>
    /// <exception cref="InvalidOperationException">Se não houver ao menos uma zona definida.</exception>
    public void Publicar()
    {
        if (_zonas.Count == 0)
        {
            throw new InvalidOperationException("A PGV exige ao menos uma zona fiscal definida antes de publicar.");
        }

        Vigente = true;
        RaiseDomainEvent(new PlantaValoresPublicada(Id, TenantId, Exercicio));
    }

    /// <summary>Obtém o valor unitário de uma zona fiscal.</summary>
    /// <param name="zonaFiscal">Código da zona fiscal.</param>
    /// <returns>O valor da zona, ou <c>null</c> se não definido.</returns>
    public ValorZona? ObterZona(string zonaFiscal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zonaFiscal);
        var chave = zonaFiscal.Trim().ToUpperInvariant();
        return _zonas.FirstOrDefault(z => z.ZonaFiscal == chave);
    }

    /// <summary>
    /// Obtém o multiplicador de um fator; retorna 1 (neutro) quando a chave não está parametrizada,
    /// para que o motor seja determinístico mesmo com a PGV ainda incompleta.
    /// </summary>
    /// <param name="tipo">Categoria do fator.</param>
    /// <param name="chave">Chave do fator.</param>
    /// <returns>O multiplicador, ou 1 se ausente.</returns>
    public decimal ObterFator(TipoFatorPgv tipo, string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            return 1m;
        }

        var k = chave.Trim().ToUpperInvariant();
        return _fatores.FirstOrDefault(f => f.Tipo == tipo && f.Chave == k)?.Multiplicador ?? 1m;
    }

    private void GarantirEditavel()
    {
        if (Vigente)
        {
            throw new InvalidOperationException("A PGV já está vigente e não pode ser alterada (Súmula 160/STJ — crie nova versão por exercício).");
        }
    }
}
