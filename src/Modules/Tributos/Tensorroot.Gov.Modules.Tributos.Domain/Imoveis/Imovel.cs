using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

/// <summary>Identificador forte do agregado <see cref="Imovel"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ImovelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ImovelId"/>.</returns>
    public static ImovelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Imóvel urbano do cadastro imobiliário municipal (BCI). Raiz de agregado rica: nasce válida,
/// protege suas invariantes (identificação, endereço, características) e vincula-se ao
/// <see cref="Contribuinte"/> existente como sujeito passivo (proprietário/possuidor). É a origem
/// do lançamento anual de IPTU. Ver M6-DESIGN §1.1.
/// </summary>
public sealed class Imovel : AggregateRoot<ImovelId>, IMustHaveTenant
{
    private Imovel()
    {
    }

    private Imovel(
        ImovelId id,
        Guid tenantId,
        ContribuinteId proprietarioId,
        IdentificacaoImovel identificacao,
        EnderecoImovel endereco,
        CaracteristicasImovel caracteristicas)
        : base(id)
    {
        TenantId = tenantId;
        ProprietarioId = proprietarioId;
        Identificacao = identificacao;
        Endereco = endereco;
        Caracteristicas = caracteristicas;
        Ativo = true;
        RaiseDomainEvent(new ImovelCadastrado(id, tenantId, proprietarioId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte sujeito passivo (proprietário/possuidor).</summary>
    public ContribuinteId ProprietarioId { get; private set; }

    /// <summary>Identificação cadastral (inscrição municipal, CIB, matrícula RGI).</summary>
    public IdentificacaoImovel Identificacao { get; private set; } = default!;

    /// <summary>Endereço/localização cadastral.</summary>
    public EnderecoImovel Endereco { get; private set; } = default!;

    /// <summary>Características físicas (BCI).</summary>
    public CaracteristicasImovel Caracteristicas { get; private set; } = default!;

    /// <summary>Indica se o imóvel está ativo no cadastro (cadastro vigente).</summary>
    public bool Ativo { get; private set; }

    /// <summary>Cadastra um imóvel urbano no cadastro imobiliário.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="proprietarioId">Contribuinte proprietário/possuidor.</param>
    /// <param name="identificacao">Identificação cadastral.</param>
    /// <param name="endereco">Endereço/localização.</param>
    /// <param name="caracteristicas">Características físicas (BCI).</param>
    /// <returns>Novo <see cref="Imovel"/> ativo.</returns>
    public static Imovel Cadastrar(
        Guid tenantId,
        ContribuinteId proprietarioId,
        IdentificacaoImovel identificacao,
        EnderecoImovel endereco,
        CaracteristicasImovel caracteristicas)
    {
        ArgumentNullException.ThrowIfNull(identificacao);
        ArgumentNullException.ThrowIfNull(endereco);
        ArgumentNullException.ThrowIfNull(caracteristicas);
        if (proprietarioId.Value == Guid.Empty)
        {
            throw new ArgumentException("O imóvel deve ter um proprietário (contribuinte).", nameof(proprietarioId));
        }

        return new Imovel(ImovelId.New(), tenantId, proprietarioId, identificacao, endereco, caracteristicas);
    }

    /// <summary>Atualiza as características físicas do imóvel (revisão cadastral/BCI).</summary>
    /// <param name="caracteristicas">Novas características.</param>
    public void AtualizarCaracteristicas(CaracteristicasImovel caracteristicas)
    {
        ArgumentNullException.ThrowIfNull(caracteristicas);
        GarantirAtivo();
        Caracteristicas = caracteristicas;
        RaiseDomainEvent(new ImovelAtualizado(Id, TenantId));
    }

    /// <summary>Atualiza o endereço/localização cadastral.</summary>
    /// <param name="endereco">Novo endereço.</param>
    public void AtualizarEndereco(EnderecoImovel endereco)
    {
        ArgumentNullException.ThrowIfNull(endereco);
        GarantirAtivo();
        Endereco = endereco;
        RaiseDomainEvent(new ImovelAtualizado(Id, TenantId));
    }

    /// <summary>Transfere a titularidade do imóvel para outro contribuinte.</summary>
    /// <param name="novoProprietarioId">Novo proprietário/possuidor.</param>
    public void TransferirProprietario(ContribuinteId novoProprietarioId)
    {
        GarantirAtivo();
        if (novoProprietarioId.Value == Guid.Empty)
        {
            throw new ArgumentException("O novo proprietário é obrigatório.", nameof(novoProprietarioId));
        }

        ProprietarioId = novoProprietarioId;
        RaiseDomainEvent(new ImovelAtualizado(Id, TenantId));
    }

    /// <summary>Inativa o imóvel no cadastro (ex.: remembramento/baixa).</summary>
    public void Inativar()
    {
        if (!Ativo)
        {
            return;
        }

        Ativo = false;
        RaiseDomainEvent(new ImovelAtualizado(Id, TenantId));
    }

    private void GarantirAtivo()
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("O imóvel está inativo no cadastro.");
        }
    }
}
