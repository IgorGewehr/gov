using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;

/// <summary>Identificador forte do agregado <see cref="Comissao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ComissaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ComissaoId"/>.</returns>
    public static ComissaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Comissao da Camara (permanente ou temporaria) como entidade de primeira classe: nome, tipo,
/// composicao (membros efetivos/suplentes vinculados a vereadores) e presidencia. Substitui a
/// referencia por texto cru hoje usada em pareceres (CF/88 art. 58; Regimento Interno).
/// </summary>
public sealed class Comissao : AggregateRoot<ComissaoId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome da comissao.</summary>
    public const int NomeMaximo = 200;

    private readonly List<MembroComissao> _membros = [];

    private Comissao()
    {
    }

    private Comissao(ComissaoId id, Guid tenantId, string nome, TipoComissao tipo)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Tipo = tipo;
        Situacao = SituacaoComissao.Ativa;
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome da comissao (ex.: "Constituicao, Justica e Redacao").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Natureza (permanente/temporaria).</summary>
    public TipoComissao Tipo { get; private set; }

    /// <summary>Situacao da comissao.</summary>
    public SituacaoComissao Situacao { get; private set; }

    /// <summary>Composicao (membros efetivos/suplentes).</summary>
    public IReadOnlyList<MembroComissao> Membros => _membros;

    /// <summary>Membro presidente da comissao (se houver).</summary>
    public MembroComissao? Presidente => _membros.Find(m => m.Cargo == CargoComissao.Presidente);

    /// <summary>Indica se a comissao esta extinta (terminal).</summary>
    public bool Terminal => Situacao == SituacaoComissao.Extinta;

    /// <summary>Cria uma comissao ativa (C-1).</summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="nome">Nome (obrigatorio).</param>
    /// <param name="tipo">Natureza.</param>
    /// <returns>Nova <see cref="Comissao"/> ativa.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio ou o tipo invalido.</exception>
    public static Comissao Criar(Guid tenantId, string nome, TipoComissao tipo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de comissao invalido.", nameof(tipo));
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > NomeMaximo)
        {
            throw new ArgumentException($"Nome excede {NomeMaximo} caracteres.", nameof(nome));
        }

        return new Comissao(ComissaoId.New(), tenantId, nomeNormalizado, tipo);
    }

    /// <summary>
    /// Designa um membro (idempotente por vereador — segunda designacao do mesmo vereador atualiza
    /// papel/cargo). Garante no maximo um presidente (C-2, C-3).
    /// </summary>
    /// <param name="vereadorId">Vereador a designar.</param>
    /// <param name="papel">Papel (efetivo/suplente).</param>
    /// <param name="cargo">Cargo de direcao.</param>
    /// <returns>O membro designado/atualizado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se a comissao estiver extinta ou ja houver outro presidente.
    /// </exception>
    public MembroComissao DesignarMembro(VereadorId vereadorId, PapelMembro papel, CargoComissao cargo)
    {
        if (Terminal)
        {
            throw new InvalidOperationException("Comissao extinta nao admite composicao.");
        }

        if (cargo == CargoComissao.Presidente && Presidente is { } atual && atual.VereadorId != vereadorId)
        {
            throw new InvalidOperationException("Comissao ja possui presidente.");
        }

        var existente = _membros.Find(m => m.VereadorId == vereadorId);
        if (existente is not null)
        {
            if (!Enum.IsDefined(cargo))
            {
                throw new ArgumentException("Cargo de comissao invalido.", nameof(cargo));
            }

            existente.DefinirCargo(cargo);
            return existente;
        }

        var membro = MembroComissao.Designar(vereadorId, papel, cargo);
        _membros.Add(membro);
        return membro;
    }

    /// <summary>Remove um membro da composicao (C-4).</summary>
    /// <param name="vereadorId">Vereador a remover.</param>
    /// <exception cref="InvalidOperationException">Se a comissao estiver extinta.</exception>
    public void RemoverMembro(VereadorId vereadorId)
    {
        if (Terminal)
        {
            throw new InvalidOperationException("Comissao extinta nao admite alteracao de composicao.");
        }

        _membros.RemoveAll(m => m.VereadorId == vereadorId);
    }

    /// <summary>Extingue a comissao (terminal) — C-5.</summary>
    public void Extinguir() => Situacao = SituacaoComissao.Extinta;
}
