using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Identificador forte do agregado <see cref="RepConfigurado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RepConfiguradoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RepConfiguradoId"/>.</returns>
    public static RepConfiguradoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Cadastro de um Registrador Eletronico de Ponto (REP) do parque do ente — multi-tenant. Identifica o
/// equipamento (marca/fabricante, nº de serie, modo de coleta e endereco/credencial-ref), o tipo
/// regulatorio (REP-C/A/P) e o ULTIMO NSR DE EQUIPAMENTO ja coletado, para que a proxima coleta seja
/// INCREMENTAL (a partir desse NSR). Credenciais reais NUNCA aqui: apenas uma REFERENCIA ao segredo no
/// Azure Key Vault (CLAUDE.md §5/§6). Raiz de agregado.
/// </summary>
public sealed class RepConfigurado : AggregateRoot<RepConfiguradoId>, IMustHaveTenant
{
    private RepConfigurado()
    {
    }

    private RepConfigurado(
        RepConfiguradoId id,
        Guid tenantId,
        string identificacaoEquipamento,
        MarcaRep marca,
        ModoColeta modos,
        TipoRep tipo,
        string? enderecoOuReferencia,
        string? referenciaCredencialCofre)
        : base(id)
    {
        TenantId = tenantId;
        IdentificacaoEquipamento = identificacaoEquipamento;
        Marca = marca;
        Modos = modos;
        Tipo = tipo;
        EnderecoOuReferencia = enderecoOuReferencia;
        ReferenciaCredencialCofre = referenciaCredencialCofre;
        Ativo = true;
        UltimoNsrColetado = 0;
    }

    /// <summary>Tenant (ente publico) dono do equipamento.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificacao do equipamento (nº de serie/fabricante) para a trilha de origem.</summary>
    public string IdentificacaoEquipamento { get; private set; } = default!;

    /// <summary>Marca/fabricante (define o <c>IColetorRep</c> que coleta o AFD).</summary>
    public MarcaRep Marca { get; private set; }

    /// <summary>Modos de coleta suportados (arquivo/TCP-SDK/REST-cloud).</summary>
    public ModoColeta Modos { get; private set; }

    /// <summary>Tipo regulatorio do REP (REP-C/A/P) — origem das marcacoes que ele produz.</summary>
    public TipoRep Tipo { get; private set; }

    /// <summary>Endereco de rede (host/porta) ou referencia logica do equipamento; opcional.</summary>
    public string? EnderecoOuReferencia { get; private set; }

    /// <summary>Referencia (nome do segredo) da credencial no Azure Key Vault; nunca a credencial em si.</summary>
    public string? ReferenciaCredencialCofre { get; private set; }

    /// <summary>Maior NSR de EQUIPAMENTO ja coletado/ingerido (0 = nada coletado). Coleta incremental.</summary>
    public long UltimoNsrColetado { get; private set; }

    /// <summary>Indica se o REP esta ativo para coleta automatica pelo worker.</summary>
    public bool Ativo { get; private set; }

    /// <summary>Registra um novo REP no parque do tenant.</summary>
    /// <param name="tenantId">Tenant dono do equipamento.</param>
    /// <param name="identificacaoEquipamento">Nº de serie/fabricante (nao vazio).</param>
    /// <param name="marca">Marca/fabricante.</param>
    /// <param name="modos">Modos de coleta suportados.</param>
    /// <param name="tipo">Tipo regulatorio (REP-C/A/P).</param>
    /// <param name="enderecoOuReferencia">Host/porta ou referencia logica; opcional.</param>
    /// <param name="referenciaCredencialCofre">Nome do segredo no Key Vault; opcional.</param>
    /// <returns>Novo <see cref="RepConfigurado"/>.</returns>
    /// <exception cref="ArgumentException">Se a identificacao for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se marca/modo/tipo forem invalidos.</exception>
    public static RepConfigurado Registrar(
        Guid tenantId,
        string identificacaoEquipamento,
        MarcaRep marca,
        ModoColeta modos,
        TipoRep tipo,
        string? enderecoOuReferencia = null,
        string? referenciaCredencialCofre = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificacaoEquipamento);
        if (!Enum.IsDefined(marca))
        {
            throw new ArgumentOutOfRangeException(nameof(marca), "Marca de REP invalida.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo de REP invalido.");
        }

        return new RepConfigurado(
            RepConfiguradoId.New(),
            tenantId,
            identificacaoEquipamento.Trim(),
            marca,
            modos,
            tipo,
            enderecoOuReferencia?.Trim(),
            referenciaCredencialCofre?.Trim());
    }

    /// <summary>
    /// Avanca o ULTIMO NSR DE EQUIPAMENTO coletado, de forma MONOTONICA (so cresce): reimportar um AFD
    /// antigo nunca retrocede o marcador. Garante que a proxima coleta peca apenas o que falta.
    /// </summary>
    /// <param name="maiorNsrIngerido">Maior NSR de equipamento ingerido no lote atual.</param>
    public void AvancarUltimoNsrColetado(long maiorNsrIngerido)
    {
        if (maiorNsrIngerido > UltimoNsrColetado)
        {
            UltimoNsrColetado = maiorNsrIngerido;
        }
    }

    /// <summary>Desativa o REP para coleta automatica (sem remover o cadastro/historico).</summary>
    public void Desativar() => Ativo = false;

    /// <summary>Reativa o REP para coleta automatica.</summary>
    public void Reativar() => Ativo = true;
}
