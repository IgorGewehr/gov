namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>Registro em conselho de classe (entrada de comando, opcional).</summary>
/// <param name="Tipo">Tipo do conselho (1=CRM, 2=COREN, 3=CRO, ...).</param>
/// <param name="Uf">UF do registro (2 caracteres).</param>
/// <param name="Numero">Numero do registro.</param>
public sealed record RegistroConselhoDto(int Tipo, string Uf, string Numero);

/// <summary>Item da lista de profissionais (navegabilidade). CPF NAO e devolvido (LGPD).</summary>
/// <param name="Id">Identificador do profissional.</param>
/// <param name="Nome">Nome do profissional.</param>
/// <param name="Conselho">Registro de conselho resumido (ex.: "CRM/RS 12345"), quando houver.</param>
/// <param name="QtdVinculosAtivos">Quantidade de vinculos CNES/CBO ativos.</param>
/// <param name="Situacao">Situacao cadastral (descricao).</param>
public sealed record ProfissionalItemLista(
    Guid Id,
    string Nome,
    string? Conselho,
    int QtdVinculosAtivos,
    string Situacao);

/// <summary>Vinculo CNES/CBO na ficha do profissional.</summary>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do vinculo.</param>
/// <param name="Cbo">Ocupacao (CBO).</param>
/// <param name="DataInicio">Data de inicio.</param>
/// <param name="DataFim">Data de encerramento (nula se vigente).</param>
public sealed record VinculoCnesDto(
    Guid EstabelecimentoId,
    string Cbo,
    DateOnly DataInicio,
    DateOnly? DataFim);

/// <summary>Ficha completa do profissional (com vinculos). CPF mascarado (LGPD).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
/// <param name="CpfMascarado">CPF formatado/mascarado para exibicao.</param>
/// <param name="Cns">CNS do profissional (quando houver).</param>
/// <param name="Conselho">Registro de conselho resumido (quando houver).</param>
/// <param name="TemCrmAtivo">Indica se possui CRM ativo (habilita teleconsulta — I-9).</param>
/// <param name="Situacao">Situacao (descricao).</param>
/// <param name="Vinculos">Vinculos CNES/CBO.</param>
public sealed record ProfissionalDetalhe(
    Guid Id,
    string Nome,
    string CpfMascarado,
    string? Cns,
    string? Conselho,
    bool TemCrmAtivo,
    string Situacao,
    IReadOnlyList<VinculoCnesDto> Vinculos);
