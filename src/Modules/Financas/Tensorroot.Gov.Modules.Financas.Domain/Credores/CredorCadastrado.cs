using Tensorroot.Gov.Modules.Financas.Domain.Credores.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Credores;

/// <summary>Identificador forte do agregado <see cref="CredorCadastrado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CredorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CredorId"/>.</returns>
    public static CredorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação do cadastro do credor.</summary>
public enum SituacaoCredor
{
    /// <summary>Ativo — pode ser referenciado em novos empenhos.</summary>
    Ativo = 1,

    /// <summary>Inativo — não recebe novos empenhos (mantido para histórico).</summary>
    Inativo = 2,
}

/// <summary>
/// Credor/Fornecedor cadastrado do ente (pessoa física ou jurídica): o registro persistente do
/// beneficiário de empenhos/pagamentos, com dados bancários para crédito (CNAB240/PIX), situação e
/// histórico. Distinto do Value Object <see cref="Credor"/> embutido no empenho — aqui é o agregado de
/// cadastro que consolida o relacionamento financeiro do ente com o credor (extrato consolidado). O
/// documento (CPF/CNPJ) normalizado é a chave de negócio única por tenant.
/// </summary>
public sealed class CredorCadastrado : AggregateRoot<CredorId>, IMustHaveTenant
{
    private CredorCadastrado()
    {
    }

    private CredorCadastrado(
        CredorId id,
        Guid tenantId,
        string nome,
        TipoPessoa tipo,
        string documento,
        ContaBancaria? dadosBancarios)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Tipo = tipo;
        Documento = documento;
        DadosBancarios = dadosBancarios;
        Situacao = SituacaoCredor.Ativo;
        RaiseDomainEvent(new CredorRegistrado(id, tenantId, documento));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome/razão social.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Tipo de pessoa (física/jurídica).</summary>
    public TipoPessoa Tipo { get; private set; }

    /// <summary>Documento (CPF/CNPJ) normalizado, sem máscara — chave de negócio por tenant.</summary>
    public string Documento { get; private set; } = default!;

    /// <summary>Dados bancários para crédito (opcional; obrigatório para remessa CNAB).</summary>
    public ContaBancaria? DadosBancarios { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoCredor Situacao { get; private set; }

    /// <summary>Converte o cadastro no Value Object <see cref="Credor"/> usado pelo empenho.</summary>
    /// <returns>O credor como Value Object.</returns>
    public Credor ComoValueObject() => Credor.De(Nome, Tipo, Documento);

    /// <summary>Cadastra um novo credor.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome/razão social.</param>
    /// <param name="tipo">Tipo de pessoa.</param>
    /// <param name="documento">Documento (CPF/CNPJ) com ou sem máscara — validado conforme o tipo.</param>
    /// <param name="dadosBancarios">Dados bancários (opcional).</param>
    /// <returns>Novo <see cref="CredorCadastrado"/>.</returns>
    /// <exception cref="ArgumentException">Se nome vazio ou documento inválido para o tipo.</exception>
    public static CredorCadastrado Cadastrar(
        Guid tenantId,
        string nome,
        TipoPessoa tipo,
        string documento,
        ContaBancaria? dadosBancarios = null)
    {
        // Reaproveita a validação canônica de documento do Value Object Credor (CPF/CNPJ por tipo).
        var vo = Credor.De(nome, tipo, documento);
        return new CredorCadastrado(CredorId.New(), tenantId, vo.Nome, vo.Tipo, vo.Documento, dadosBancarios);
    }

    /// <summary>Atualiza o nome/razão social.</summary>
    /// <param name="nome">Novo nome.</param>
    /// <exception cref="ArgumentException">Se vazio.</exception>
    public void AlterarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        Nome = nome.Trim();
    }

    /// <summary>Define/atualiza os dados bancários do credor.</summary>
    /// <param name="dadosBancarios">Conta bancária para crédito (ou <c>null</c> para remover).</param>
    public void DefinirDadosBancarios(ContaBancaria? dadosBancarios) => DadosBancarios = dadosBancarios;

    /// <summary>Inativa o credor (não recebe novos empenhos; histórico preservado).</summary>
    /// <exception cref="InvalidOperationException">Se já inativo.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoCredor.Inativo)
        {
            throw new InvalidOperationException("Credor ja esta inativo.");
        }

        Situacao = SituacaoCredor.Inativo;
    }

    /// <summary>Reativa o credor.</summary>
    /// <exception cref="InvalidOperationException">Se já ativo.</exception>
    public void Reativar()
    {
        if (Situacao == SituacaoCredor.Ativo)
        {
            throw new InvalidOperationException("Credor ja esta ativo.");
        }

        Situacao = SituacaoCredor.Ativo;
    }

    /// <summary>Garante que o credor está ativo (para vincular a novos empenhos).</summary>
    /// <exception cref="InvalidOperationException">Se inativo.</exception>
    public void GarantirAtivo()
    {
        if (Situacao != SituacaoCredor.Ativo)
        {
            throw new InvalidOperationException($"Credor {Nome} ({Documento}) esta inativo; nao pode receber empenho.");
        }
    }
}
