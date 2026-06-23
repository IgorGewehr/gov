using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>Endereco do estabelecimento sujeito a VISA (transporte).</summary>
/// <param name="Logradouro">Logradouro.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">UF (2 letras).</param>
/// <param name="Cep">CEP.</param>
public sealed record EnderecoVisaDto(string Logradouro, string Bairro, string Municipio, string Uf, string Cep);

/// <summary>Resumo de um estabelecimento sujeito a VISA (listagem/leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Documento">Documento do responsavel (somente digitos).</param>
/// <param name="EhPessoaJuridica">Verdadeiro se CNPJ; falso se CPF.</param>
/// <param name="RazaoSocial">Razao social/nome.</param>
/// <param name="Ramo">Ramo de atividade.</param>
/// <param name="Risco">Grau de risco sanitario.</param>
/// <param name="Situacao">Situacao cadastral.</param>
/// <param name="Municipio">Municipio do endereco.</param>
public sealed record EstabelecimentoFiscalizavelDto(
    Guid Id,
    string Documento,
    bool EhPessoaJuridica,
    string RazaoSocial,
    RamoVisa Ramo,
    GrauRiscoSanitario Risco,
    SituacaoEstabelecimentoVisa Situacao,
    string Municipio);

/// <summary>Item do roteiro de inspecao (leitura).</summary>
/// <param name="Requisito">Requisito sanitario.</param>
/// <param name="Conformidade">Conformidade aferida.</param>
/// <param name="Observacao">Observacao do fiscal.</param>
public sealed record ItemInspecaoDto(string Requisito, ConformidadeItem Conformidade, string? Observacao);

/// <summary>Inspecao/vistoria sanitaria (leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EstabelecimentoId">Estabelecimento inspecionado.</param>
/// <param name="DataInspecao">Data da vistoria.</param>
/// <param name="FiscalId">Fiscal responsavel (opcional).</param>
/// <param name="Roteiro">Roteiro aplicado.</param>
/// <param name="Situacao">Situacao da inspecao.</param>
/// <param name="Resultado">Resultado consolidado (se concluida).</param>
/// <param name="Pendencias">Quantidade de pendencias (itens nao conformes).</param>
/// <param name="Itens">Itens do roteiro.</param>
public sealed record InspecaoDto(
    Guid Id,
    Guid EstabelecimentoId,
    DateOnly DataInspecao,
    Guid? FiscalId,
    string? Roteiro,
    SituacaoInspecao Situacao,
    ResultadoInspecao? Resultado,
    int Pendencias,
    IReadOnlyList<ItemInspecaoDto> Itens);

/// <summary>Auto (infracao/intimacao) da VISA (leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EstabelecimentoId">Estabelecimento autuado.</param>
/// <param name="InspecaoId">Inspecao fundante.</param>
/// <param name="Tipo">Tipo do auto.</param>
/// <param name="Numero">Numero do auto.</param>
/// <param name="DataLavratura">Data de lavratura.</param>
/// <param name="PrazoFinal">Prazo final.</param>
/// <param name="ValorMulta">Valor da multa (se houver).</param>
/// <param name="Situacao">Situacao no processo.</param>
public sealed record AutoVisaDto(
    Guid Id,
    Guid EstabelecimentoId,
    Guid InspecaoId,
    TipoAutoVisa Tipo,
    string Numero,
    DateOnly DataLavratura,
    DateOnly PrazoFinal,
    decimal? ValorMulta,
    SituacaoAutoVisa Situacao);

/// <summary>Licenca/alvara sanitario (leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EstabelecimentoId">Estabelecimento licenciado.</param>
/// <param name="Numero">Numero do alvara.</param>
/// <param name="EmitidaEm">Data de emissao.</param>
/// <param name="ValidadeAte">Data de validade.</param>
/// <param name="Situacao">Situacao da licenca.</param>
public sealed record LicencaSanitariaDto(
    Guid Id,
    Guid EstabelecimentoId,
    string Numero,
    DateOnly EmitidaEm,
    DateOnly ValidadeAte,
    SituacaoLicenca Situacao);

/// <summary>Mapeadores de agregados de Vigilancia para DTO (sem vazar o dominio para a borda).</summary>
internal static class VigilanciaMapper
{
    public static EstabelecimentoFiscalizavelDto ParaDto(this EstabelecimentoFiscalizavel e) => new(
        e.Id.Value,
        e.Documento.Digitos,
        e.Documento.EhPessoaJuridica,
        e.RazaoSocial,
        e.Ramo,
        e.Risco,
        e.Situacao,
        e.Endereco.Municipio);

    public static InspecaoDto ParaDto(this Inspecao i) => new(
        i.Id.Value,
        i.EstabelecimentoFiscalizavelId.Value,
        i.DataInspecao,
        i.FiscalId?.Value,
        i.Roteiro,
        i.Situacao,
        i.Resultado,
        i.QuantidadePendencias(),
        [.. i.Itens.Select(it => new ItemInspecaoDto(it.Requisito, it.Conformidade, it.Observacao))]);

    public static AutoVisaDto ParaDto(this AutoVisa a) => new(
        a.Id.Value,
        a.EstabelecimentoFiscalizavelId.Value,
        a.InspecaoId.Value,
        a.Tipo,
        a.Numero,
        a.DataLavratura,
        a.PrazoFinal,
        a.ValorMulta,
        a.Situacao);

    public static LicencaSanitariaDto ParaDto(this LicencaSanitaria l) => new(
        l.Id.Value,
        l.EstabelecimentoFiscalizavelId.Value,
        l.Numero,
        l.EmitidaEm,
        l.ValidadeAte,
        l.Situacao);
}
