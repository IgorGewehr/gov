using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

/// <summary>Identificador forte de um <see cref="Credenciado"/> (inscricao no credenciamento).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CredenciadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CredenciadoId"/>.</returns>
    public static CredenciadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Inscricao de um interessado no credenciamento (Lei 14.133/2021, art. 79): registro de adesao ao
/// chamamento publico permanente, com analise documental/habilitacao e, uma vez deferida, ingresso no
/// rol de credenciados aptos a serem contratados. Como o chamamento e permanentemente aberto, a inscricao
/// pode ser protocolada a qualquer tempo enquanto vigente o edital. Entidade filha do agregado
/// <see cref="Credenciamento"/>; vincula-se a um Fornecedor ja cadastrado.
/// </summary>
public sealed class Credenciado : Entity<CredenciadoId>
{
    private Credenciado()
    {
    }

    private Credenciado(CredenciadoId id, Guid fornecedorId, DateOnly dataInscricao)
        : base(id)
    {
        FornecedorId = fornecedorId;
        DataInscricao = dataInscricao;
        Situacao = SituacaoCredenciado.EmAnalise;
    }

    /// <summary>Fornecedor (pessoa fisica/juridica) interessado, ja cadastrado no modulo.</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Data de protocolo da inscricao (ingresso a qualquer tempo — art. 79, par. unico).</summary>
    public DateOnly DataInscricao { get; private set; }

    /// <summary>Situacao atual da inscricao no ciclo de credenciamento.</summary>
    public SituacaoCredenciado Situacao { get; private set; }

    /// <summary>Data de deferimento (habilitacao) do credenciamento, quando ocorrido.</summary>
    public DateOnly? DataCredenciamento { get; private set; }

    /// <summary>Data de descredenciamento, quando ocorrido (terminal).</summary>
    public DateOnly? DataDescredenciamento { get; private set; }

    /// <summary>Motivacao do ultimo ato de indeferimento/suspensao/descredenciamento (trilha administrativa).</summary>
    public string? Motivo { get; private set; }

    /// <summary>Protocola a inscricao de um interessado (nasce <c>EmAnalise</c>).</summary>
    /// <param name="fornecedorId">Fornecedor interessado.</param>
    /// <param name="dataInscricao">Data de protocolo.</param>
    /// <returns>Nova inscricao em analise.</returns>
    /// <exception cref="ArgumentException">Fornecedor vazio.</exception>
    public static Credenciado Inscrever(Guid fornecedorId, DateOnly dataInscricao)
    {
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentException("Fornecedor obrigatorio para a inscricao no credenciamento.", nameof(fornecedorId));
        }

        return new Credenciado(CredenciadoId.New(), fornecedorId, dataInscricao);
    }

    /// <summary>
    /// Defere a inscricao apos analise documental: o interessado passa a <c>Credenciado</c> (apto a
    /// contratacao). Fail-closed: interessado com sancao impeditiva vigente nao pode ser credenciado
    /// (art. 14/156 — aferido na borda).
    /// </summary>
    /// <param name="data">Data do deferimento.</param>
    /// <param name="fornecedorImpedido">Indica sancao impeditiva vigente do fornecedor (aferido na borda).</param>
    /// <exception cref="InvalidOperationException">Inscricao fora de analise ou fornecedor impedido.</exception>
    public void Deferir(DateOnly data, bool fornecedorImpedido)
    {
        if (Situacao != SituacaoCredenciado.EmAnalise)
        {
            throw new InvalidOperationException($"O deferimento exige inscricao EmAnalise. Situacao atual: {Situacao}.");
        }

        if (fornecedorImpedido)
        {
            throw new InvalidOperationException(
                "Interessado com sancao impeditiva vigente (impedimento/inidoneidade) nao pode ser credenciado (art. 14/156 Lei 14.133/2021).");
        }

        Situacao = SituacaoCredenciado.Credenciado;
        DataCredenciamento = data;
        Motivo = null;
    }

    /// <summary>Indefere a inscricao por nao atendimento das condicoes do edital.</summary>
    /// <param name="motivo">Motivacao do indeferimento.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Inscricao fora de analise.</exception>
    public void Indeferir(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoCredenciado.EmAnalise)
        {
            throw new InvalidOperationException($"O indeferimento exige inscricao EmAnalise. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciado.Indeferido;
        Motivo = motivo.Trim();
    }

    /// <summary>Suspende temporariamente o credenciamento por descumprimento sanavel (ato motivado).</summary>
    /// <param name="motivo">Motivacao da suspensao.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Credenciado nao esta apto.</exception>
    public void Suspender(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoCredenciado.Credenciado)
        {
            throw new InvalidOperationException($"A suspensao exige credenciado apto. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciado.Suspenso;
        Motivo = motivo.Trim();
    }

    /// <summary>Restabelece o credenciamento suspenso (saneado o descumprimento).</summary>
    /// <exception cref="InvalidOperationException">Credenciado nao esta suspenso.</exception>
    public void Reabilitar()
    {
        if (Situacao != SituacaoCredenciado.Suspenso)
        {
            throw new InvalidOperationException($"A reabilitacao exige credenciado Suspenso. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciado.Credenciado;
        Motivo = null;
    }

    /// <summary>Descredencia o interessado (terminal): a pedido, por descumprimento ou sancao impeditiva.</summary>
    /// <param name="data">Data do descredenciamento.</param>
    /// <param name="motivo">Motivacao do ato.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Inscricao ja terminal (indeferida/descredenciada).</exception>
    public void Descredenciar(DateOnly data, string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoCredenciado.Indeferido or SituacaoCredenciado.Descredenciado)
        {
            throw new InvalidOperationException($"Inscricao em estado terminal nao pode ser descredenciada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoCredenciado.Descredenciado;
        DataDescredenciamento = data;
        Motivo = motivo.Trim();
    }

    /// <summary>Indica se a inscricao esta apta (credenciado vigente) na data de referencia.</summary>
    /// <returns><c>true</c> quando <see cref="Situacao"/> e <c>Credenciado</c>.</returns>
    public bool EstaApto() => Situacao == SituacaoCredenciado.Credenciado;
}
