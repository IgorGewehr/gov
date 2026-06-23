namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>Endereco do estabelecimento (entrada de comando).</summary>
/// <param name="Logradouro">Logradouro (obrigatorio).</param>
/// <param name="Numero">Numero/complemento.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">Unidade da federacao (2 caracteres).</param>
/// <param name="Cep">CEP (somente digitos).</param>
public sealed record EnderecoEstabelecimentoDto(
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep);

/// <summary>Item da lista de estabelecimentos (navegabilidade).</summary>
/// <param name="Id">Identificador do estabelecimento.</param>
/// <param name="Cnes">Codigo CNES (7 digitos).</param>
/// <param name="Nome">Nome do estabelecimento.</param>
/// <param name="Tipo">Tipo (descricao).</param>
/// <param name="Municipio">Municipio do endereco.</param>
/// <param name="Uf">UF do endereco.</param>
/// <param name="Situacao">Situacao cadastral (descricao).</param>
public sealed record EstabelecimentoItemLista(
    Guid Id,
    string Cnes,
    string Nome,
    string Tipo,
    string Municipio,
    string Uf,
    string Situacao);

/// <summary>Ficha completa do estabelecimento.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Cnes">Codigo CNES.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Tipo">Tipo (descricao).</param>
/// <param name="Logradouro">Logradouro.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">UF.</param>
/// <param name="Cep">CEP.</param>
/// <param name="Situacao">Situacao (descricao).</param>
public sealed record EstabelecimentoDetalhe(
    Guid Id,
    string Cnes,
    string Nome,
    string Tipo,
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep,
    string Situacao);
