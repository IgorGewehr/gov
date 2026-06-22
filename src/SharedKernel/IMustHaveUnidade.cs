namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Contrato opcional (ABAC do recurso) para toda entidade de negocio "dona" de uma Unidade
/// Organizacional (UO). Espelha <see cref="IMustHaveTenant"/>: assim como o filtro de tenant
/// e aplicado por reflexao sobre <see cref="IMustHaveTenant"/>, o filtro de leitura por UO
/// (introduzido em M1) sera aplicado por reflexao sobre este marcador, restringindo leituras
/// as UOs do escopo efetivo do sujeito (derivado das atribuicoes de papel).
/// </summary>
/// <remarks>
/// Diferente de <see cref="IMustHaveTenant"/>, este contrato NAO e obrigatorio em toda entidade:
/// so as entidades fiscais/organizacionais (Financas, Patrimonio, RH, ...) o adotam, onde a
/// dimensao organizacional importa para autorizacao e prestacao de contas (UO/UG).
/// </remarks>
public interface IMustHaveUnidade
{
    /// <summary>Identificador da Unidade Organizacional (UO) dona do registro.</summary>
    Guid UnidadeId { get; }
}
