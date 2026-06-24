using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Integracoes;

/// <summary>
/// Implementacao PADRAO (fallback) de <see cref="IConsultaReceitaEmEscopoDedicado"/> que retorna
/// <c>null</c> — ou seja, "Financas nao detem a receita do exercicio para este tenant". Mantem o modulo
/// Legislativo auto-suficiente e construivel isoladamente: a apuracao do art. 29-A sempre funciona pelo
/// caminho da base INFORMADA (entrada auditada), que e a regra para a Camara (tenant distinto do Executivo).
/// <para>
/// // TODO(M10): substituir/registrar no ApiHost uma implementacao real de
/// <see cref="IConsultaReceitaEmEscopoDedicado"/> que abra um escopo de DI dedicado (reaplicando o tenant
/// corrente via <c>TenantOverride</c>) e resolva <see cref="IConsultaReceitaParaLimiteLegislativo"/> de
/// Financas — espelhando <c>ConsultaContratoEmEscopoDedicado</c>. Requer tambem que
/// Financas.Infrastructure implemente <see cref="IConsultaReceitaParaLimiteLegislativo"/> sobre a sua
/// contabilidade/arrecadacao. Ambos fora do escopo de edicao desta entrega (so Legislativo + Contracts).
/// </para>
/// </summary>
public sealed class ConsultaReceitaEmEscopoDedicadoPadrao : IConsultaReceitaEmEscopoDedicado
{
    /// <inheritdoc />
    public Task<ReceitaArt29ADto?> ConsultarBaseAsync(int exercicio, CancellationToken cancellationToken)
        => Task.FromResult<ReceitaArt29ADto?>(null);
}
