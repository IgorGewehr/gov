using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>
/// Plano Plurianual (PPA — CF 165 §1º): plano de quatro anos com Programas → Ações → Metas.
/// Toda a árvore é UM agregado (consistência transacional da compatibilidade interna).
/// Só o PPA <see cref="SituacaoPpa.Vigente"/> é referenciável por LDO/LOA.
/// </summary>
public sealed class PlanoPlurianual : AggregateRoot<PpaId>, IMustHaveTenant
{
    /// <summary>Duração do quadriênio do PPA (CF 165 §1º): 4 anos.</summary>
    public const int DuracaoAnos = 4;

    private readonly List<Programa> _programas = [];

    private PlanoPlurianual()
    {
    }

    private PlanoPlurianual(PpaId id, Guid tenantId, int anoInicio, string numeroLei, int anoLei)
        : base(id)
    {
        TenantId = tenantId;
        AnoInicio = anoInicio;
        AnoFim = anoInicio + DuracaoAnos - 1;
        NumeroLei = numeroLei;
        AnoLei = anoLei;
        Situacao = SituacaoPpa.Elaboracao;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Primeiro ano do quadriênio.</summary>
    public int AnoInicio { get; private set; }

    /// <summary>Último ano do quadriênio (= AnoInicio + 3).</summary>
    public int AnoFim { get; private set; }

    /// <summary>Número da lei do PPA.</summary>
    public string NumeroLei { get; private set; } = default!;

    /// <summary>Ano da lei do PPA.</summary>
    public int AnoLei { get; private set; }

    /// <summary>Situação (máquina de estados).</summary>
    public SituacaoPpa Situacao { get; private set; }

    /// <summary>Programas do PPA.</summary>
    public IReadOnlyCollection<Programa> Programas => _programas.AsReadOnly();

    /// <summary>Cria um PPA em elaboração para o quadriênio que começa em <paramref name="anoInicio"/>.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="anoInicio">Primeiro ano do quadriênio.</param>
    /// <param name="numeroLei">Número/identificação da lei.</param>
    /// <param name="anoLei">Ano da lei.</param>
    /// <returns>Novo <see cref="PlanoPlurianual"/>.</returns>
    /// <exception cref="ArgumentException">Se o ano de início for inválido.</exception>
    public static PlanoPlurianual Criar(Guid tenantId, int anoInicio, string numeroLei, int anoLei)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLei);
        if (anoInicio < DotacaoOrcamentaria.ExercicioMinimo)
        {
            throw new ArgumentException($"AnoInicio deve ser maior ou igual a {DotacaoOrcamentaria.ExercicioMinimo}.", nameof(anoInicio));
        }

        return new PlanoPlurianual(PpaId.New(), tenantId, anoInicio, numeroLei.Trim(), anoLei);
    }

    /// <summary>Adiciona um programa ao PPA (somente em elaboração; código único no PPA).</summary>
    /// <returns>Identificador do programa criado.</returns>
    public ProgramaId AdicionarPrograma(
        string codigo,
        string nome,
        string objetivo,
        string publicoAlvo,
        string indicador,
        decimal indicadorLinhaBase,
        decimal indicadorMeta)
    {
        GarantirEditavel(nameof(AdicionarPrograma));
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        if (_programas.Exists(p => string.Equals(p.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Programa com codigo {codigo} ja existe no PPA.");
        }

        var programa = Programa.Criar(Id, codigo, nome, objetivo, publicoAlvo, indicador, indicadorLinhaBase, indicadorMeta);
        _programas.Add(programa);
        return programa.Id;
    }

    /// <summary>Adiciona uma ação a um programa do PPA.</summary>
    /// <returns>Identificador da ação criada.</returns>
    /// <exception cref="InvalidOperationException">Se o programa não existir.</exception>
    public AcaoPpaId AdicionarAcao(
        ProgramaId programaId,
        string codigo,
        string nome,
        TipoAcao tipo,
        string funcionalProgramatica,
        string produto,
        string unidadeMedida)
    {
        GarantirEditavel(nameof(AdicionarAcao));
        var programa = _programas.Find(p => p.Id == programaId)
            ?? throw new InvalidOperationException("Programa nao encontrado no PPA.");

        var acao = programa.AdicionarAcao(codigo, nome, tipo, funcionalProgramatica, produto, unidadeMedida);
        return acao.Id;
    }

    /// <summary>Define (cria/atualiza) uma meta física/financeira de uma ação num ano do quadriênio.</summary>
    /// <exception cref="InvalidOperationException">Se a ação não existir.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o ano estiver fora do quadriênio.</exception>
    public void DefinirMeta(
        AcaoPpaId acaoId,
        int ano,
        decimal metaFisica,
        string unidadeMedida,
        ValorMonetario metaFinanceira,
        string regiao)
    {
        GarantirEditavel(nameof(DefinirMeta));
        ArgumentNullException.ThrowIfNull(metaFinanceira);
        if (ano < AnoInicio || ano > AnoFim)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), $"Ano {ano} fora do quadrienio [{AnoInicio}, {AnoFim}].");
        }

        var acao = LocalizarAcao(acaoId) ?? throw new InvalidOperationException("Acao nao encontrada no PPA.");
        acao.DefinirMeta(ano, metaFisica, unidadeMedida, metaFinanceira, regiao);
    }

    /// <summary>Coloca o PPA em tramitação no Legislativo (congela a edição).</summary>
    /// <exception cref="TransicaoPlanejamentoInvalidaException">Se não estiver em elaboração ou sem ação com meta.</exception>
    public void ColocarEmTramitacao()
    {
        if (Situacao != SituacaoPpa.Elaboracao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(PlanoPlurianual), Situacao.ToString(), nameof(ColocarEmTramitacao));
        }

        if (!_programas.Exists(p => p.PossuiAcaoComMeta()))
        {
            throw new InvalidOperationException("PPA exige ao menos um programa com acao e meta para tramitar.");
        }

        Situacao = SituacaoPpa.EmTramitacao;
    }

    /// <summary>Coloca o PPA em vigor (lei sancionada) — torna-o referenciável por LDO/LOA.</summary>
    /// <param name="numeroLei">Número da lei sancionada.</param>
    /// <param name="anoLei">Ano da lei sancionada.</param>
    /// <exception cref="TransicaoPlanejamentoInvalidaException">Se não estiver em tramitação.</exception>
    public void Vigorar(string numeroLei, int anoLei)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLei);
        if (Situacao != SituacaoPpa.EmTramitacao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(PlanoPlurianual), Situacao.ToString(), nameof(Vigorar));
        }

        NumeroLei = numeroLei.Trim();
        AnoLei = anoLei;
        Situacao = SituacaoPpa.Vigente;
        RaiseDomainEvent(new PpaVigente(Id, AnoInicio, AnoFim));
    }

    /// <summary>Encerra o PPA ao fim do quadriênio.</summary>
    public void Encerrar()
    {
        if (Situacao != SituacaoPpa.Vigente)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(PlanoPlurianual), Situacao.ToString(), nameof(Encerrar));
        }

        Situacao = SituacaoPpa.Encerrado;
    }

    /// <summary>Marca o PPA como revisado (substituído por nova versão — lei de revisão própria).</summary>
    public void MarcarRevisado()
    {
        if (Situacao != SituacaoPpa.Vigente)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(PlanoPlurianual), Situacao.ToString(), nameof(MarcarRevisado));
        }

        Situacao = SituacaoPpa.Revisado;
    }

    /// <summary>
    /// Query de domínio: indica se a ação existe no PPA e o PPA está vigente.
    /// Base da validação de compatibilidade LOA ⊆ PPA (usada pela LOA/LDO).
    /// </summary>
    /// <param name="acaoId">Ação a verificar.</param>
    /// <returns><c>true</c> se a ação existe e o PPA está vigente.</returns>
    public bool ContemAcaoVigente(AcaoPpaId acaoId)
        => Situacao == SituacaoPpa.Vigente && LocalizarAcao(acaoId) is not null;

    /// <summary>Indica se o quadriênio do PPA cobre o exercício informado.</summary>
    /// <param name="exercicio">Exercício a verificar.</param>
    /// <returns><c>true</c> se o exercício está dentro do quadriênio.</returns>
    public bool CobreExercicio(int exercicio) => exercicio >= AnoInicio && exercicio <= AnoFim;

    private AcaoPpa? LocalizarAcao(AcaoPpaId acaoId)
    {
        foreach (var programa in _programas)
        {
            var acao = programa.LocalizarAcao(acaoId);
            if (acao is not null)
            {
                return acao;
            }
        }

        return null;
    }

    private void GarantirEditavel(string operacao)
    {
        if (Situacao != SituacaoPpa.Elaboracao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(PlanoPlurianual), Situacao.ToString(), operacao);
        }
    }
}
