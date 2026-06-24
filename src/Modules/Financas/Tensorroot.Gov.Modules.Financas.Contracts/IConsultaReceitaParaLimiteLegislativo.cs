namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Porta de LEITURA cross-module (CLAUDE.md §2) exposta pelo modulo Financas para o modulo Legislativo:
/// fornece a BASE DE CALCULO do art. 29-A da CF/88 — o somatorio da RECEITA TRIBUTARIA e das
/// TRANSFERENCIAS efetivamente REALIZADAS no exercicio informado (o caput toma o "exercicio anterior").
/// Quem conhece a contabilidade/arrecadacao e Financas; o Legislativo apenas consome a base ja consolidada.
/// <para>
/// SEGURANCA/ISOLAMENTO: a apuracao e SEMPRE do tenant corrente (Global Query Filter do Financas). O
/// Legislativo nunca referencia entidades internas de Financas — recebe so o veredito chapado
/// (<see cref="ReceitaArt29ADto"/>).
/// </para>
/// <para>
/// ATENCAO multi-tenant: Executivo e Camara do mesmo municipio sao tenants DISTINTOS (CNPJs distintos).
/// Esta porta serve quando o mesmo tenant detem a contabilidade municipal (ou via consulta dedicada do
/// ApiHost). Quando a Camara nao detem a receita do Executivo, a base e INFORMADA na apuracao (entrada
/// manual auditada), e esta porta retorna <c>null</c> — o handler do Legislativo trata os dois caminhos.
/// </para>
/// </summary>
public interface IConsultaReceitaParaLimiteLegislativo
{
    /// <summary>
    /// Apura a base do art. 29-A (receita tributaria + transferencias realizadas) do tenant atual no
    /// exercicio informado. Read-only.
    /// </summary>
    /// <param name="exercicio">Exercicio (ano) da arrecadacao a consolidar (o "exercicio anterior" do caput).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A base discriminada, ou <c>null</c> se nao houver receita apurada para o exercicio no tenant.</returns>
    Task<ReceitaArt29ADto?> ConsultarBaseAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// Base de calculo do art. 29-A devolvida por Financas: discrimina a receita tributaria e as
/// transferencias realizadas, mais o exercicio de referencia (para conferencia no Legislativo).
/// </summary>
/// <param name="Exercicio">Exercicio (ano) da arrecadacao consolidada.</param>
/// <param name="ReceitaTributaria">Receita tributaria realizada no exercicio (&gt;= 0).</param>
/// <param name="Transferencias">Transferencias constitucionais/legais realizadas no exercicio (&gt;= 0).</param>
public sealed record ReceitaArt29ADto(int Exercicio, decimal ReceitaTributaria, decimal Transferencias);
