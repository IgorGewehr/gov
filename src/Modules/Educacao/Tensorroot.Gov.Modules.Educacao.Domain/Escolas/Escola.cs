using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Escolas;

/// <summary>Identificador forte do agregado <see cref="Escola"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EscolaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EscolaId"/>.</returns>
    public static EscolaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Unidade escolar da rede municipal de ensino, identificada nacionalmente pelo
/// <see cref="CodigoInep"/> — chave de integracao com o Censo Escolar/EducaCenso (INEP).
/// Reune credenciamento, dependencia administrativa, endereco georreferenciado e
/// infraestrutura, e e a origem dos formularios Escola/Gestor exportados ao INEP.
/// Raiz de agregado; nasce valida via fabrica <see cref="Credenciar"/>.
/// </summary>
public sealed class Escola : AggregateRoot<EscolaId>, IMustHaveTenant
{
    private Escola()
    {
    }

    private Escola(
        EscolaId id,
        Guid tenantId,
        CodigoInep codigoInep,
        string nome,
        DependenciaAdministrativa dependencia,
        Endereco endereco,
        Infraestrutura infraestrutura)
        : base(id)
    {
        TenantId = tenantId;
        CodigoInep = codigoInep;
        Nome = nome;
        DependenciaAdministrativa = dependencia;
        Endereco = endereco;
        Infraestrutura = infraestrutura;
        Situacao = SituacaoEscola.Credenciada;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo INEP unico nacional da escola (chave do Censo).</summary>
    public CodigoInep CodigoInep { get; private set; }

    /// <summary>Nome da unidade escolar.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Dependencia administrativa (Federal/Estadual/Municipal/Privada).</summary>
    public DependenciaAdministrativa DependenciaAdministrativa { get; private set; }

    /// <summary>Endereco e georreferenciamento da escola.</summary>
    public Endereco Endereco { get; private set; }

    /// <summary>Infraestrutura fisica e itens de acessibilidade.</summary>
    public Infraestrutura Infraestrutura { get; private set; }

    /// <summary>Situacao atual da escola no ciclo de operacao.</summary>
    public SituacaoEscola Situacao { get; private set; }

    /// <summary>Indica se a escola esta ativa (credenciada e apta a operar).</summary>
    public bool Ativa => Situacao is SituacaoEscola.Credenciada;

    /// <summary>Indica se a escola foi encerrada (desativada — estado terminal).</summary>
    public bool Encerrada => Situacao is SituacaoEscola.Desativada;

    /// <summary>
    /// Credencia uma nova escola, habilitando-a a operar na rede de ensino (situacao inicial
    /// <see cref="SituacaoEscola.Credenciada"/>). Emite <see cref="EscolaCredenciada"/> (I-1/I-2/I-3/I-4).
    /// </summary>
    /// <param name="tenantId">Tenant (rede de ensino) dono do registro.</param>
    /// <param name="codigoInep">Codigo INEP unico nacional.</param>
    /// <param name="nome">Nome da unidade escolar.</param>
    /// <param name="dependencia">Dependencia administrativa.</param>
    /// <param name="endereco">Endereco e georreferenciamento.</param>
    /// <param name="infraestrutura">Infraestrutura fisica e acessibilidade.</param>
    /// <returns>Nova <see cref="Escola"/> em situacao <see cref="SituacaoEscola.Credenciada"/>.</returns>
    /// <exception cref="ArgumentException">Se o codigo INEP ou o nome forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a dependencia administrativa for invalida.</exception>
    public static Escola Credenciar(
        Guid tenantId,
        CodigoInep codigoInep,
        string nome,
        DependenciaAdministrativa dependencia,
        Endereco endereco,
        Infraestrutura infraestrutura)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoInep.Valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (!Enum.IsDefined(dependencia))
        {
            throw new ArgumentOutOfRangeException(nameof(dependencia), "Dependencia administrativa invalida.");
        }

        var escola = new Escola(
            EscolaId.New(),
            tenantId,
            codigoInep,
            nome.Trim(),
            dependencia,
            endereco,
            infraestrutura);

        escola.RaiseDomainEvent(new EscolaCredenciada(escola.Id, codigoInep));
        return escola;
    }

    /// <summary>
    /// Atualiza os dados cadastrais exigidos pelo EducaCenso (endereco e infraestrutura).
    /// Nao altera a situacao; emite <see cref="DadosCensoAtualizados"/> (I-5).
    /// </summary>
    /// <param name="endereco">Novo endereco/georreferenciamento.</param>
    /// <param name="infraestrutura">Nova infraestrutura.</param>
    /// <exception cref="InvalidOperationException">Se a escola estiver desativada (I-5/I-8).</exception>
    public void AtualizarDadosCenso(Endereco endereco, Infraestrutura infraestrutura)
    {
        if (Encerrada)
        {
            throw new InvalidOperationException(
                $"Escola desativada nao admite atualizacao de dados do Censo. Situacao atual: {Situacao}.");
        }

        Endereco = endereco;
        Infraestrutura = infraestrutura;
        RaiseDomainEvent(new DadosCensoAtualizados(Id));
    }

    /// <summary>Desativa a escola (estado terminal). So permitido a partir de <see cref="SituacaoEscola.Credenciada"/> (I-8).</summary>
    /// <exception cref="InvalidOperationException">Se a escola nao estiver credenciada.</exception>
    public void Desativar()
    {
        if (Situacao != SituacaoEscola.Credenciada)
        {
            throw new InvalidOperationException(
                $"A desativacao exige escola credenciada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoEscola.Desativada;
    }
}
