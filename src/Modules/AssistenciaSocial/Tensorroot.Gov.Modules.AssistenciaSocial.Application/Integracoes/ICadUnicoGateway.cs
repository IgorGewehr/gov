namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;

/// <summary>
/// Resultado, traduzido para o dominio, da consulta de um NIS no CadUnico (MDS): folha resumo,
/// composicao familiar e renda declarada. Read model federal — somente leitura (I-9).
/// </summary>
/// <param name="NisLocalizado">Indica se o NIS foi localizado e e valido no CadUnico.</param>
/// <param name="RendaFamiliarDeclarada">Renda familiar declarada na base federal.</param>
/// <param name="QuantidadeMembros">Quantidade de membros na composicao federal.</param>
/// <param name="DataUltimaAtualizacao">Data da ultima atualizacao do cadastro na base federal.</param>
public sealed record ResultadoCadUnico(
    bool NisLocalizado,
    decimal RendaFamiliarDeclarada,
    int QuantidadeMembros,
    DateOnly DataUltimaAtualizacao);

/// <summary>
/// Anti-Corruption Layer (ACL) de consulta ao CadUnico/MDS por NIS. A base federal e a fonte
/// autoritativa e jamais e sobrescrita por este modulo (I-9); o gateway apenas projeta um read
/// model. A implementacao (Infrastructure) usa <c>HttpClient</c> tipado + Polly (retry, circuit
/// breaker, timeout) e e idempotente. Definido localmente como ACL do modulo enquanto a
/// integracao concreta nao e implementada na camada de Infraestrutura.
/// </summary>
public interface ICadUnicoGateway
{
    /// <summary>Consulta a folha resumo federal de um NIS.</summary>
    /// <param name="nis">NIS (somente digitos) a consultar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resultado traduzido para o dominio; <see cref="ResultadoCadUnico.NisLocalizado"/> falso quando ausente.</returns>
    Task<ResultadoCadUnico> ConsultarPorNisAsync(string nis, CancellationToken cancellationToken);
}
