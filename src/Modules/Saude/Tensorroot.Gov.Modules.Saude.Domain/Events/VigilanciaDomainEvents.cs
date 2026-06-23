using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Estabelecimento sujeito a VISA cadastrado no acervo de fiscalizacao.</summary>
/// <param name="EstabelecimentoFiscalizavelId">Identificador do estabelecimento.</param>
/// <param name="Documento">CNPJ/CPF (somente digitos) do responsavel.</param>
/// <param name="Ramo">Ramo de atividade sujeito a VISA.</param>
public sealed record EstabelecimentoFiscalizavelCadastrado(
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId,
    string Documento,
    RamoVisa Ramo) : IDomainEvent;

/// <summary>Estabelecimento interditado por ato da VISA (cautelar/definitivo).</summary>
/// <param name="EstabelecimentoFiscalizavelId">Identificador do estabelecimento.</param>
/// <param name="Motivo">Motivo da interdicao.</param>
public sealed record EstabelecimentoVisaInterditado(
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId,
    string Motivo) : IDomainEvent;

/// <summary>Inspecao/vistoria sanitaria aberta para um estabelecimento.</summary>
/// <param name="InspecaoId">Identificador da inspecao.</param>
/// <param name="EstabelecimentoFiscalizavelId">Estabelecimento inspecionado.</param>
public sealed record InspecaoAberta(
    InspecaoId InspecaoId,
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId) : IDomainEvent;

/// <summary>Inspecao concluida com resultado consolidado.</summary>
/// <param name="InspecaoId">Identificador da inspecao.</param>
/// <param name="EstabelecimentoFiscalizavelId">Estabelecimento inspecionado.</param>
/// <param name="Resultado">Resultado consolidado da inspecao.</param>
public sealed record InspecaoConcluida(
    InspecaoId InspecaoId,
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId,
    ResultadoInspecao Resultado) : IDomainEvent;

/// <summary>Auto (infracao/intimacao) lavrado a partir de uma inspecao.</summary>
/// <param name="AutoVisaId">Identificador do auto.</param>
/// <param name="EstabelecimentoFiscalizavelId">Estabelecimento autuado.</param>
/// <param name="Tipo">Tipo do auto.</param>
/// <param name="PrazoFinal">Prazo final (defesa/regularizacao).</param>
public sealed record AutoVisaLavrado(
    AutoVisaId AutoVisaId,
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId,
    TipoAutoVisa Tipo,
    DateOnly PrazoFinal) : IDomainEvent;

/// <summary>Auto julgado (deferido/indeferido) ou intimacao regularizada — encerra o prazo.</summary>
/// <param name="AutoVisaId">Identificador do auto.</param>
/// <param name="Situacao">Situacao final do auto.</param>
public sealed record AutoVisaJulgado(
    AutoVisaId AutoVisaId,
    SituacaoAutoVisa Situacao) : IDomainEvent;

/// <summary>Licenca/alvara sanitario emitido para um estabelecimento.</summary>
/// <param name="LicencaSanitariaId">Identificador da licenca.</param>
/// <param name="EstabelecimentoFiscalizavelId">Estabelecimento licenciado.</param>
/// <param name="Numero">Numero do alvara.</param>
/// <param name="ValidadeAte">Data de validade.</param>
public sealed record LicencaSanitariaEmitida(
    LicencaSanitariaId LicencaSanitariaId,
    EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId,
    string Numero,
    DateOnly ValidadeAte) : IDomainEvent;

/// <summary>Licenca/alvara sanitario cassado (apos auto de penalidade ou ato da VISA).</summary>
/// <param name="LicencaSanitariaId">Identificador da licenca.</param>
/// <param name="Motivo">Motivo da cassacao.</param>
public sealed record LicencaSanitariaCassada(
    LicencaSanitariaId LicencaSanitariaId,
    string Motivo) : IDomainEvent;
