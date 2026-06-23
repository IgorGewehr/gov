using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

/// <summary>
/// Fotografia (snapshot) contábil de um bem do acervo no momento da abertura do inventário —
/// dado de entrada para <see cref="Inventario.CarregarSnapshotContabil"/>. Congela descrição,
/// localização e valor para a conciliação, sem navegar ao agregado <c>BemPatrimonial</c> ao vivo.
/// </summary>
/// <param name="BemPatrimonialId">Bem do acervo.</param>
/// <param name="NumeroTombamento">Número de tombo (nulo se não tombado).</param>
/// <param name="Descricao">Descrição do bem no momento do snapshot.</param>
/// <param name="LocalizacaoEsperada">Localização esperada (último responsável/localização conhecida).</param>
/// <param name="ValorContabil">Valor contábil congelado.</param>
public sealed record SnapshotBem(
    BemPatrimonialId BemPatrimonialId,
    string? NumeroTombamento,
    string Descricao,
    string? LocalizacaoEsperada,
    ValorMonetario ValorContabil);
