using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;

/// <summary>Resumo de um contribuinte para o picker do balcão (P1). Documento mascarado (LGPD).</summary>
/// <param name="Id">Identificador do contribuinte.</param>
/// <param name="Nome">Nome ou razão social.</param>
/// <param name="TipoPessoa">Tipo de pessoa (Fisica/Juridica).</param>
/// <param name="DocumentoMascarado">CPF/CNPJ parcialmente mascarado.</param>
/// <param name="InscricaoMunicipal">Inscrição municipal, se houver.</param>
public sealed record ContribuinteResumo(
    Guid Id,
    string Nome,
    string TipoPessoa,
    string DocumentoMascarado,
    string? InscricaoMunicipal);

/// <summary>Busca contribuintes por nome ou CPF/CNPJ para o picker do balcão (P1).</summary>
/// <param name="Termo">Termo de busca (nome ou documento).</param>
public sealed record BuscarContribuintesQuery(string? Termo)
    : IQuery<IReadOnlyList<ContribuinteResumo>>;

/// <summary>Handler da busca de contribuintes (picker do balcão).</summary>
public sealed class BuscarContribuintesHandler(IContribuinteRepository contribuintes)
    : IQueryHandler<BuscarContribuintesQuery, IReadOnlyList<ContribuinteResumo>>
{
    // Teto de resultados do picker (busca incremental — não é listagem paginada completa).
    private const int LimitePicker = 20;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContribuinteResumo>> Handle(
        BuscarContribuintesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await contribuintes
            .BuscarAsync(request.Termo, LimitePicker, cancellationToken)
            .ConfigureAwait(false);

        return encontrados
            .Select(contribuinte => new ContribuinteResumo(
                contribuinte.Id.Value,
                contribuinte.Nome,
                contribuinte.TipoPessoa.ToString(),
                MascararDocumento(contribuinte.Documento),
                contribuinte.InscricaoMunicipal))
            .ToList();
    }

    // Máscara LGPD: preserva os 3 primeiros e os 2 últimos dígitos (suficiente para conferência no balcão).
    private static string MascararDocumento(string documento)
    {
        if (string.IsNullOrEmpty(documento) || documento.Length <= 5)
        {
            return new string('*', documento?.Length ?? 0);
        }

        return string.Concat(
            documento.AsSpan(0, 3),
            new string('*', documento.Length - 5),
            documento.AsSpan(documento.Length - 2));
    }
}
