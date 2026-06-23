using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;

/// <summary>
/// Estabelecimento/unidade de saude (UBS, UPA, hospital, CAPS, etc.) identificado de forma univoca
/// pelo CNES. Master data LOCAL do Bounded Context Saude que materializa o que antes era um GUID
/// solto referenciado pelo <see cref="Domain.Atendimento.Atendimento"/>. Raiz de agregado: nasce
/// valida via <see cref="Cadastrar"/> e protege as invariantes de CNES valido, unicidade por tenant
/// e bloqueio de vinculo quando inativo.
/// <para>
/// O <see cref="EstabelecimentoId"/> e o MESMO tipo forte ja referenciado pelo atendimento
/// (<c>Domain.Atendimento.EstabelecimentoId</c>) — referencia cross-aggregate por Id, sem navegacao.
/// </para>
/// </summary>
public sealed class Estabelecimento : AggregateRoot<EstabelecimentoId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome do estabelecimento.</summary>
    public const int ComprimentoNome = 200;

    private Estabelecimento()
    {
    }

    private Estabelecimento(
        EstabelecimentoId id,
        Guid tenantId,
        CodigoCnes cnes,
        string nome,
        TipoEstabelecimento tipo,
        Endereco endereco)
        : base(id)
    {
        TenantId = tenantId;
        Cnes = cnes;
        Nome = nome;
        Tipo = tipo;
        Endereco = endereco;
        Situacao = SituacaoEstabelecimento.Ativo;
        RaiseDomainEvent(new EstabelecimentoCadastrado(id, cnes.Valor));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo CNES (chave de negocio, imutavel apos o cadastro).</summary>
    public CodigoCnes Cnes { get; private set; }

    /// <summary>Nome do estabelecimento (ex.: "UBS Central").</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Tipo do estabelecimento (UBS, UPA, hospital, ...).</summary>
    public TipoEstabelecimento Tipo { get; private set; }

    /// <summary>Endereco do estabelecimento.</summary>
    public Endereco Endereco { get; private set; }

    /// <summary>Situacao cadastral atual.</summary>
    public SituacaoEstabelecimento Situacao { get; private set; }

    /// <summary>Indica se o estabelecimento esta ativo (habilita novos vinculos/atendimentos).</summary>
    public bool EstaAtivo => Situacao == SituacaoEstabelecimento.Ativo;

    /// <summary>
    /// Cadastra um novo estabelecimento de saude no acervo local. Nasce <see cref="SituacaoEstabelecimento.Ativo"/>.
    /// Emite <see cref="EstabelecimentoCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cnes">Codigo CNES (chave de negocio, 7 digitos).</param>
    /// <param name="nome">Nome do estabelecimento (obrigatorio).</param>
    /// <param name="tipo">Tipo do estabelecimento.</param>
    /// <param name="endereco">Endereco do estabelecimento.</param>
    /// <returns>Novo <see cref="Estabelecimento"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio/exceder o limite ou o tipo for invalido.</exception>
    public static Estabelecimento Cadastrar(
        Guid tenantId,
        CodigoCnes cnes,
        string nome,
        TipoEstabelecimento tipo,
        Endereco endereco)
    {
        var normalizado = NormalizarNome(nome);
        GarantirTipoValido(tipo);
        return new Estabelecimento(EstabelecimentoId.New(), tenantId, cnes, normalizado, tipo, endereco);
    }

    /// <summary>Atualiza os dados cadastrais do estabelecimento ativo (CNES e imutavel).</summary>
    /// <param name="nome">Novo nome.</param>
    /// <param name="tipo">Novo tipo.</param>
    /// <param name="endereco">Novo endereco.</param>
    /// <exception cref="ArgumentException">Se o nome for vazio/exceder o limite ou o tipo for invalido.</exception>
    /// <exception cref="InvalidOperationException">Se o estabelecimento estiver inativo.</exception>
    public void AtualizarDados(string nome, TipoEstabelecimento tipo, Endereco endereco)
    {
        GarantirAtivo();
        Nome = NormalizarNome(nome);
        GarantirTipoValido(tipo);
        Tipo = tipo;
        Endereco = endereco;
        RaiseDomainEvent(new EstabelecimentoAtualizado(Id));
    }

    /// <summary>Inativa o estabelecimento (encerramento/suspensao). Idempotente. Emite <see cref="EstabelecimentoInativado"/>.</summary>
    public void Inativar()
    {
        if (Situacao == SituacaoEstabelecimento.Inativo)
        {
            return;
        }

        Situacao = SituacaoEstabelecimento.Inativo;
        RaiseDomainEvent(new EstabelecimentoInativado(Id));
    }

    /// <summary>Reativa um estabelecimento inativado. Idempotente. Emite <see cref="EstabelecimentoReativado"/>.</summary>
    public void Reativar()
    {
        if (Situacao == SituacaoEstabelecimento.Ativo)
        {
            return;
        }

        Situacao = SituacaoEstabelecimento.Ativo;
        RaiseDomainEvent(new EstabelecimentoReativado(Id));
    }

    private static string NormalizarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var normalizado = nome.Trim();
        if (normalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome do estabelecimento excede {ComprimentoNome} caracteres.", nameof(nome));
        }

        return normalizado;
    }

    private static void GarantirTipoValido(TipoEstabelecimento tipo)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException($"Tipo de estabelecimento invalido: {tipo}.", nameof(tipo));
        }
    }

    private void GarantirAtivo()
    {
        if (Situacao != SituacaoEstabelecimento.Ativo)
        {
            throw new InvalidOperationException(
                $"Estabelecimento inativo nao admite alteracoes. Situacao atual: {Situacao}.");
        }
    }
}
