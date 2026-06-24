using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Portarias;

/// <summary>Resumo de leitura de uma portaria (linha de lista/navegabilidade).</summary>
/// <param name="Id">Identificador da portaria.</param>
/// <param name="Numero">Numeracao oficial formatada (NNN/AAAA).</param>
/// <param name="Exercicio">Exercicio (ano) da numeracao.</param>
/// <param name="Sequencial">Sequencial dentro do exercicio.</param>
/// <param name="Tipo">Natureza do ato.</param>
/// <param name="DataAto">Data do ato.</param>
/// <param name="Ementa">Ementa/resumo do ato.</param>
/// <param name="ServidorId">Servidor vinculado, quando aplicavel.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record PortariaResumo(
    Guid Id,
    string Numero,
    int Exercicio,
    int Sequencial,
    TipoPortaria Tipo,
    DateOnly DataAto,
    string Ementa,
    Guid? ServidorId,
    SituacaoPortaria Situacao);

/// <summary>Detalhe de leitura completo de uma portaria (inclui o texto integral).</summary>
/// <param name="Id">Identificador da portaria.</param>
/// <param name="Numero">Numeracao oficial formatada (NNN/AAAA).</param>
/// <param name="Exercicio">Exercicio (ano) da numeracao.</param>
/// <param name="Sequencial">Sequencial dentro do exercicio.</param>
/// <param name="Tipo">Natureza do ato.</param>
/// <param name="DataAto">Data do ato.</param>
/// <param name="Ementa">Ementa/resumo do ato.</param>
/// <param name="Texto">Texto integral do ato.</param>
/// <param name="ServidorId">Servidor vinculado, quando aplicavel.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="MotivoRevogacao">Fundamento da revogacao, quando revogada.</param>
public sealed record PortariaDetalhe(
    Guid Id,
    string Numero,
    int Exercicio,
    int Sequencial,
    TipoPortaria Tipo,
    DateOnly DataAto,
    string Ementa,
    string Texto,
    Guid? ServidorId,
    SituacaoPortaria Situacao,
    string? MotivoRevogacao);

/// <summary>Projecoes (somente leitura) do agregado <see cref="Portaria"/>.</summary>
public static class ProjetarPortaria
{
    /// <summary>Projeta uma portaria para o resumo de lista.</summary>
    /// <param name="portaria">Agregado de origem.</param>
    /// <returns>Resumo projetado.</returns>
    public static PortariaResumo ParaResumo(Portaria portaria)
    {
        ArgumentNullException.ThrowIfNull(portaria);
        return new PortariaResumo(
            portaria.Id.Value,
            portaria.Numero.Formatado,
            portaria.Exercicio,
            portaria.Sequencial,
            portaria.Tipo,
            portaria.DataAto,
            portaria.Ementa,
            portaria.ServidorId?.Value,
            portaria.Situacao);
    }

    /// <summary>Projeta uma portaria para o detalhe completo.</summary>
    /// <param name="portaria">Agregado de origem.</param>
    /// <returns>Detalhe projetado.</returns>
    public static PortariaDetalhe ParaDetalhe(Portaria portaria)
    {
        ArgumentNullException.ThrowIfNull(portaria);
        return new PortariaDetalhe(
            portaria.Id.Value,
            portaria.Numero.Formatado,
            portaria.Exercicio,
            portaria.Sequencial,
            portaria.Tipo,
            portaria.DataAto,
            portaria.Ementa,
            portaria.Texto,
            portaria.ServidorId?.Value,
            portaria.Situacao,
            portaria.MotivoRevogacao);
    }
}
