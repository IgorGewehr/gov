using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Repositório das regras de classificação setorial (<see cref="FonteRecursoVinculado"/>).</summary>
public interface IFonteRecursoVinculadoRepository
{
    /// <summary>Adiciona uma regra de classificação.</summary>
    /// <param name="regra">Regra a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(FonteRecursoVinculado regra, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as regras vigentes em uma data de referência (vigência ≤ referência), tenant-scoped.
    /// </summary>
    /// <param name="referencia">Data de referência (início de vigência ≤ referência).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Regras vigentes do tenant.</returns>
    Task<IReadOnlyList<FonteRecursoVinculado>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken);
}

/// <summary>Repositório dos prazos do <see cref="CalendarioFederal"/>.</summary>
public interface ICalendarioFederalRepository
{
    /// <summary>Adiciona um prazo do calendário.</summary>
    /// <param name="prazo">Prazo a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(CalendarioFederal prazo, CancellationToken cancellationToken);

    /// <summary>Lista os prazos do exercício (tenant-scoped), opcionalmente filtrando por chave.</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="chave">Filtro opcional por chave do prazo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Prazos do exercício.</returns>
    Task<IReadOnlyList<CalendarioFederal>> ListarPorExercicioAsync(int exercicio, string? chave, CancellationToken cancellationToken);
}

/// <summary>Repositório/projeção das linhas de execução fiscal (<see cref="LinhaExecucaoFiscal"/>).</summary>
public interface ILinhaExecucaoFiscalRepository
{
    /// <summary>Adiciona uma linha de execução fiscal (idempotente por <c>OrigemHash</c> no chamador).</summary>
    /// <param name="linha">Linha a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(LinhaExecucaoFiscal linha, CancellationToken cancellationToken);

    /// <summary>Indica se já existe linha com o hash de origem informado (idempotência).</summary>
    /// <param name="origemHash">Hash de origem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já projetada.</returns>
    Task<bool> ExisteAsync(string origemHash, CancellationToken cancellationToken);

    /// <summary>Lista as linhas de execução do exercício (tenant-scoped).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas do exercício.</returns>
    Task<IReadOnlyList<LinhaExecucaoFiscal>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório dos pareceres dos conselhos (<see cref="ParecerConselho"/>).</summary>
public interface IParecerConselhoRepository
{
    /// <summary>Adiciona um parecer de conselho.</summary>
    /// <param name="parecer">Parecer a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(ParecerConselho parecer, CancellationToken cancellationToken);

    /// <summary>Lista os pareceres do exercício (tenant-scoped).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pareceres do exercício.</returns>
    Task<IReadOnlyList<ParecerConselho>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
