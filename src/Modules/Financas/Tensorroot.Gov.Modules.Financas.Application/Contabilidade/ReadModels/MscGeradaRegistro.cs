using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

/// <summary>
/// Marca (registro de controle) de que a MSC de uma competência já foi gerada e publicada — garante a
/// idempotência por <c>(TenantId, Exercicio, Mes, TipoMatriz)</c>: reexecutar <c>GerarMscCommand</c> para
/// a mesma competência não republica o evento de integração. Persistido no schema <c>financas</c>.
/// </summary>
public sealed class MscGeradaRegistro : IMustHaveTenant
{
    /// <summary>Construtor exigido pelo materializador do EF Core.</summary>
    public MscGeradaRegistro()
    {
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Exercício.</summary>
    public int Exercicio { get; set; }

    /// <summary>Mês da competência (1-12; 13 = Encerramento).</summary>
    public int Mes { get; set; }

    /// <summary>Tipo da matriz (1 = Agregada, 2 = Encerramento).</summary>
    public int TipoMatriz { get; set; }

    /// <summary>Identificador do evento de integração publicado.</summary>
    public Guid EventId { get; set; }

    /// <summary>Quantidade de linhas geradas na MSC.</summary>
    public int QuantidadeLinhas { get; set; }

    /// <summary>Data/hora (UTC) da geração.</summary>
    public DateTime GeradaEmUtc { get; set; }
}
