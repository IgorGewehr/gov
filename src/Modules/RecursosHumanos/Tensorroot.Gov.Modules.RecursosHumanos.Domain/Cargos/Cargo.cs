using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

/// <summary>Identificador forte do agregado <see cref="Cargo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CargoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CargoId"/>.</returns>
    public static CargoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Cargo publico: posicao na estrutura de pessoal do ente publico (efetivo, comissionado ou
/// temporario). Define vencimento, lotacao e regime previdenciario associado (efetivo → RPPS;
/// demais → RGPS — EC 103/2019), controla quantitativo de vagas (provimento/vacancia) e sujeita-se
/// ao teto remuneratorio (CF art. 37, XI). Raiz de agregado, nasce valida via <see cref="Criar"/>.
/// </summary>
public sealed class Cargo : AggregateRoot<CargoId>, IMustHaveTenant
{
    /// <summary>Quantidade minima de vagas autorizadas que todo cargo possui ao nascer.</summary>
    public const int VagasMinimas = 1;

    private Cargo()
    {
    }

    private Cargo(
        CargoId id,
        Guid tenantId,
        string denominacao,
        TipoCargo tipo,
        Vencimento vencimento,
        Lotacao lotacao,
        int quantidadeVagas,
        string leiCriacao,
        PlanoDeCargosId? planoDeCargosId)
        : base(id)
    {
        TenantId = tenantId;
        Denominacao = denominacao;
        Tipo = tipo;
        Vencimento = vencimento;
        Lotacao = lotacao;
        Regime = DerivarRegime(tipo);
        QuantidadeVagas = quantidadeVagas;
        VagasOcupadas = 0;
        LeiCriacao = leiCriacao;
        Situacao = SituacaoCargo.Ativo;
        PlanoDeCargosId = planoDeCargosId;
        RaiseDomainEvent(new CargoCriado(id, denominacao, tipo));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Denominacao legal do cargo.</summary>
    public string Denominacao { get; private set; } = default!;

    /// <summary>Tipo (natureza) do cargo: efetivo, comissionado ou temporario.</summary>
    public TipoCargo Tipo { get; private set; }

    /// <summary>Remuneracao-base do cargo.</summary>
    public Vencimento Vencimento { get; private set; } = default!;

    /// <summary>Lotacao/estabelecimento de exercicio.</summary>
    public Lotacao Lotacao { get; private set; } = default!;

    /// <summary>Regime previdenciario, derivado do <see cref="Tipo"/> (efetivo → RPPS; demais → RGPS).</summary>
    public RegimePrevidenciario Regime { get; private set; }

    /// <summary>Quantitativo de vagas autorizadas em lei.</summary>
    public int QuantidadeVagas { get; private set; }

    /// <summary>Vagas atualmente providas (ocupadas).</summary>
    public int VagasOcupadas { get; private set; }

    /// <summary>Lei que criou o cargo (referencia normativa).</summary>
    public string LeiCriacao { get; private set; } = default!;

    /// <summary>Situacao (estado) atual do cargo.</summary>
    public SituacaoCargo Situacao { get; private set; }

    /// <summary>Plano de cargos a que pertence (opcional).</summary>
    public PlanoDeCargosId? PlanoDeCargosId { get; private set; }

    /// <summary>Numero de vagas ainda disponiveis para provimento.</summary>
    public int VagasDisponiveis => QuantidadeVagas - VagasOcupadas;

    /// <summary>Cria um novo cargo publico na estrutura de pessoal.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="denominacao">Denominacao legal do cargo.</param>
    /// <param name="tipo">Tipo (efetivo/comissionado/temporario).</param>
    /// <param name="vencimento">Remuneracao-base.</param>
    /// <param name="lotacao">Lotacao/estabelecimento de exercicio.</param>
    /// <param name="quantidadeVagas">Vagas autorizadas (maior ou igual a 1).</param>
    /// <param name="leiCriacao">Lei de criacao do cargo.</param>
    /// <param name="planoDeCargosId">Plano de cargos a que pertence (opcional).</param>
    /// <returns>Novo <see cref="Cargo"/> em situacao <see cref="SituacaoCargo.Ativo"/>.</returns>
    /// <exception cref="ArgumentException">Se a denominacao ou a lei de criacao forem vazias, ou se as vagas forem menores que 1.</exception>
    /// <exception cref="ArgumentNullException">Se o vencimento ou a lotacao forem nulos.</exception>
    public static Cargo Criar(
        Guid tenantId,
        string denominacao,
        TipoCargo tipo,
        Vencimento vencimento,
        Lotacao lotacao,
        int quantidadeVagas,
        string leiCriacao,
        PlanoDeCargosId? planoDeCargosId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(denominacao);
        ArgumentNullException.ThrowIfNull(vencimento);
        ArgumentNullException.ThrowIfNull(lotacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(leiCriacao);
        if (quantidadeVagas < VagasMinimas)
        {
            throw new ArgumentException($"Quantidade de vagas deve ser ao menos {VagasMinimas}.", nameof(quantidadeVagas));
        }

        return new Cargo(
            CargoId.New(),
            tenantId,
            denominacao.Trim(),
            tipo,
            vencimento,
            lotacao,
            quantidadeVagas,
            leiCriacao.Trim(),
            planoDeCargosId);
    }

    /// <summary>Prove uma vaga do cargo (nomeacao).</summary>
    /// <exception cref="InvalidOperationException">Se o cargo estiver extinto ou nao houver vaga disponivel.</exception>
    public void Prover()
    {
        GarantirNaoExtinto();
        if (VagasOcupadas >= QuantidadeVagas)
        {
            throw new InvalidOperationException("Nao ha vaga disponivel: as vagas ocupadas atingiram o quantitativo autorizado.");
        }

        VagasOcupadas++;
        Situacao = SituacaoCargo.Ativo;
        RaiseDomainEvent(new CargoProvido(Id));
    }

    /// <summary>Libera uma vaga do cargo (vacancia).</summary>
    /// <exception cref="InvalidOperationException">Se o cargo estiver extinto ou nao houver ocupante.</exception>
    public void Vagar()
    {
        GarantirNaoExtinto();
        if (VagasOcupadas <= 0)
        {
            throw new InvalidOperationException("Nao ha ocupante a liberar: vagas ocupadas igual a zero.");
        }

        VagasOcupadas--;
        if (VagasOcupadas == 0)
        {
            Situacao = SituacaoCargo.Vago;
            RaiseDomainEvent(new CargoVago(Id));
        }
    }

    /// <summary>Altera o vencimento-base do cargo (sujeito a abate-teto na folha — CF art. 37, XI).</summary>
    /// <param name="novoVencimento">Novo vencimento.</param>
    /// <exception cref="ArgumentNullException">Se o novo vencimento for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se o cargo estiver extinto.</exception>
    public void AlterarVencimento(Vencimento novoVencimento)
    {
        ArgumentNullException.ThrowIfNull(novoVencimento);
        GarantirNaoExtinto();
        Vencimento = novoVencimento;
        RaiseDomainEvent(new VencimentoAlterado(Id, novoVencimento.Valor));
    }

    /// <summary>Altera o quantitativo de vagas autorizadas em lei.</summary>
    /// <param name="novaQuantidade">Nova quantidade de vagas (maior ou igual as vagas ocupadas).</param>
    /// <exception cref="ArgumentException">Se a nova quantidade for menor que 1.</exception>
    /// <exception cref="InvalidOperationException">Se o cargo estiver extinto ou a nova quantidade for inferior as vagas ocupadas.</exception>
    public void AlterarQuantidadeVagas(int novaQuantidade)
    {
        GarantirNaoExtinto();
        if (novaQuantidade < VagasMinimas)
        {
            throw new ArgumentException($"Quantidade de vagas deve ser ao menos {VagasMinimas}.", nameof(novaQuantidade));
        }

        if (novaQuantidade < VagasOcupadas)
        {
            throw new InvalidOperationException($"Nova quantidade de vagas ({novaQuantidade}) nao pode ser inferior as vagas ocupadas ({VagasOcupadas}).");
        }

        QuantidadeVagas = novaQuantidade;
    }

    /// <summary>Extingue o cargo por lei (estado terminal).</summary>
    /// <param name="leiExtincao">Lei que extingue o cargo.</param>
    /// <exception cref="ArgumentException">Se a lei de extincao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o cargo ja estiver extinto ou houver vagas ocupadas.</exception>
    public void Extinguir(string leiExtincao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leiExtincao);
        GarantirNaoExtinto();
        if (VagasOcupadas != 0)
        {
            throw new InvalidOperationException("A extincao exige que nao haja servidor em exercicio (VagasOcupadas == 0).");
        }

        Situacao = SituacaoCargo.Extinto;
        RaiseDomainEvent(new CargoExtinto(Id, leiExtincao.Trim()));
    }

    private static RegimePrevidenciario DerivarRegime(TipoCargo tipo)
        => tipo == TipoCargo.Efetivo ? RegimePrevidenciario.Rpps : RegimePrevidenciario.Rgps;

    private void GarantirNaoExtinto()
    {
        if (Situacao == SituacaoCargo.Extinto)
        {
            throw new InvalidOperationException("Cargo extinto e terminal: nao admite provimento, vacancia, alteracao de vencimento/vagas ou nova extincao.");
        }
    }
}
