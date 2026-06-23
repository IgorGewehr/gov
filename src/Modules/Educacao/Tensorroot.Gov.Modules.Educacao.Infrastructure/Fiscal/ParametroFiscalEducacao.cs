using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

/// <summary>
/// Percentual/valor fiscal de Educação <b>versionado por tenant+vigência</b> (nunca hardcoded — CLAUDE.md
/// §16). Ex.: a chave <c>mde.percentual-minimo</c> guarda o mínimo MDE (default legal 0,25 — CF art. 212);
/// <c>fundeb.piso-remuneracao</c> guarda o piso de 70% (EC 108/2020). A Lei Orgânica municipal pode fixar
/// maior. Mora na Infrastructure (é um registro de configuração, não invariante de domínio). Espelha o
/// <c>ParametroFiscalSaude</c> da Saúde.
/// </summary>
public sealed class ParametroFiscalEducacao : IMustHaveTenant
{
    private ParametroFiscalEducacao()
    {
    }

    private ParametroFiscalEducacao(Guid id, Guid tenantId, string chave, DateOnly vigenciaInicio, decimal valor)
    {
        Id = id;
        TenantId = tenantId;
        Chave = chave;
        VigenciaInicio = vigenciaInicio;
        Valor = valor;
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (município) dono do parâmetro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave do parâmetro (ex.: <c>mde.percentual-minimo</c>).</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Início de vigência (inclusive).</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Valor vigente (ex.: 0,25 para 25%).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Chave do percentual mínimo de aplicação em MDE (CF art. 212).</summary>
    public const string ChavePercentualMinimoMde = "mde.percentual-minimo";

    /// <summary>Chave do piso de aplicação do FUNDEB na remuneração dos profissionais (EC 108/2020).</summary>
    public const string ChavePisoRemuneracaoFundeb = "fundeb.piso-remuneracao";

    /// <summary>Cria um parâmetro fiscal versionado.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="chave">Chave do parâmetro.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="valor">Valor vigente.</param>
    /// <returns>Novo <see cref="ParametroFiscalEducacao"/>.</returns>
    public static ParametroFiscalEducacao Criar(Guid tenantId, string chave, DateOnly vigenciaInicio, decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        return new ParametroFiscalEducacao(Guid.NewGuid(), tenantId, chave.Trim(), vigenciaInicio, valor);
    }
}

/// <summary>
/// <b>E-2 (read model).</b> Total da remuneração dos profissionais da educação básica de um exercício
/// (numerador do piso de 70% do FUNDEB), por <c>(Tenant, Exercicio)</c>. Alimentado pela folha do RH
/// (Contracts) ou informado como parâmetro pelo município. Mora na Infrastructure — é projeção de leitura,
/// não invariante de domínio. Idempotente por exercício (um registro por tenant+exercício).
/// </summary>
public sealed class RemuneracaoMagisterioExercicio : IMustHaveTenant
{
    private RemuneracaoMagisterioExercicio()
    {
    }

    private RemuneracaoMagisterioExercicio(Guid id, Guid tenantId, int exercicio, decimal remuneracaoProfissionais)
    {
        Id = id;
        TenantId = tenantId;
        Exercicio = exercicio;
        RemuneracaoProfissionais = remuneracaoProfissionais;
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (município) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício de referência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Total pago a profissionais da educação básica no exercício (numerador dos 70%).</summary>
    public decimal RemuneracaoProfissionais { get; private set; }

    /// <summary>Cria o registro de remuneração de um exercício.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="remuneracaoProfissionais">Total pago (&gt;= 0).</param>
    /// <returns>Novo <see cref="RemuneracaoMagisterioExercicio"/>.</returns>
    public static RemuneracaoMagisterioExercicio Criar(Guid tenantId, int exercicio, decimal remuneracaoProfissionais)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remuneracaoProfissionais);
        return new RemuneracaoMagisterioExercicio(Guid.NewGuid(), tenantId, exercicio, remuneracaoProfissionais);
    }

    /// <summary>Atualiza o total (cruzamento idempotente: reprocessar atualiza o mesmo exercício).</summary>
    /// <param name="remuneracaoProfissionais">Novo total (&gt;= 0).</param>
    public void Atualizar(decimal remuneracaoProfissionais)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remuneracaoProfissionais);
        RemuneracaoProfissionais = remuneracaoProfissionais;
    }
}
