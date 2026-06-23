using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Item do catalogo de medicamentos da rede municipal (REMUME): principio ativo, concentracao,
/// forma farmaceutica e classe de controle especial (Portaria 344/1998 — base do SNGPC). E master
/// data local, raiz de agregado propria do BC Saude (nao referencia o ItemEstoque de Patrimonio —
/// medicamento tem regras proprias de controle/receita). Nasce valido via <see cref="Cadastrar"/>.
/// </summary>
public sealed class Medicamento : AggregateRoot<MedicamentoId>, IMustHaveTenant
{
    private Medicamento()
    {
    }

    private Medicamento(
        MedicamentoId id,
        Guid tenantId,
        string principioAtivo,
        string apresentacao,
        string concentracao,
        FormaFarmaceutica forma,
        UnidadeMedidaMedicamento unidade,
        TipoControleSngpc controle,
        string? codigoCatmat)
        : base(id)
    {
        TenantId = tenantId;
        PrincipioAtivo = principioAtivo;
        Apresentacao = apresentacao;
        Concentracao = concentracao;
        Forma = forma;
        Unidade = unidade;
        Controle = controle;
        CodigoCatmat = codigoCatmat;
        Ativo = true;
        RaiseDomainEvent(new MedicamentoCadastrado(id, principioAtivo));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Principio ativo (DCB/DCI) — base da prescricao por nome generico (Lei 9.787/1999).</summary>
    public string PrincipioAtivo { get; private set; } = default!;

    /// <summary>Apresentacao comercial/descritiva (ex.: "comprimido revestido 500 mg, caixa c/ 20").</summary>
    public string Apresentacao { get; private set; } = default!;

    /// <summary>Concentracao/dosagem (ex.: "500 mg", "250 mg/5 mL").</summary>
    public string Concentracao { get; private set; } = default!;

    /// <summary>Forma farmaceutica.</summary>
    public FormaFarmaceutica Forma { get; private set; }

    /// <summary>Unidade de medida do estoque/dispensacao.</summary>
    public UnidadeMedidaMedicamento Unidade { get; private set; }

    /// <summary>Classe de controle especial (Portaria 344/1998) — define exigencia de receita/escrituracao.</summary>
    public TipoControleSngpc Controle { get; private set; }

    /// <summary>Codigo CATMAT (catalogo de materiais do Comprasnet), quando aplicavel.</summary>
    public string? CodigoCatmat { get; private set; }

    /// <summary>Indica se o item esta ativo no catalogo (pode ser dispensado/receber entrada).</summary>
    public bool Ativo { get; private set; }

    /// <summary>Indica se o medicamento exige retencao/escrituracao de receita controlada (Portaria 344/1998).</summary>
    public bool ExigeReceitaControlada => Controle != TipoControleSngpc.SemControle;

    /// <summary>
    /// Cadastra um item no catalogo de medicamentos. Nasce ativo. Emite <see cref="MedicamentoCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="principioAtivo">Principio ativo (DCB/DCI).</param>
    /// <param name="apresentacao">Apresentacao descritiva.</param>
    /// <param name="concentracao">Concentracao/dosagem.</param>
    /// <param name="forma">Forma farmaceutica.</param>
    /// <param name="unidade">Unidade de medida do estoque.</param>
    /// <param name="controle">Classe de controle especial.</param>
    /// <param name="codigoCatmat">Codigo CATMAT (opcional).</param>
    /// <returns>Novo <see cref="Medicamento"/>.</returns>
    /// <exception cref="ArgumentException">Se principio ativo, apresentacao ou concentracao forem vazios.</exception>
    public static Medicamento Cadastrar(
        Guid tenantId,
        string principioAtivo,
        string apresentacao,
        string concentracao,
        FormaFarmaceutica forma,
        UnidadeMedidaMedicamento unidade,
        TipoControleSngpc controle,
        string? codigoCatmat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principioAtivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(apresentacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(concentracao);

        return new Medicamento(
            MedicamentoId.New(),
            tenantId,
            principioAtivo.Trim(),
            apresentacao.Trim(),
            concentracao.Trim(),
            forma,
            unidade,
            controle,
            string.IsNullOrWhiteSpace(codigoCatmat) ? null : codigoCatmat.Trim());
    }

    /// <summary>Inativa o item no catalogo (descontinuado). Idempotente.</summary>
    public void Inativar() => Ativo = false;

    /// <summary>Reativa o item no catalogo. Idempotente.</summary>
    public void Reativar() => Ativo = true;
}
