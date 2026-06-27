using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// Dados do empregador/orgao publico para o S-1000 (origem: configuracao do tenant / provider, NAO ha
/// agregado de empregador no RH). // TODO(validar-oficial: campos exatos do XSD S-1000 e EFR obrigatorio
/// para ente publico).
/// </summary>
/// <param name="TpInsc">Tipo de inscricao (CNPJ para ente publico).</param>
/// <param name="NrInsc">Inscricao do ente (CNPJ).</param>
/// <param name="NomeRazao">Nome/razao social do ente.</param>
/// <param name="ClassTrib">Classificacao tributaria (Tabela 08). // TODO(validar-oficial).</param>
/// <param name="NrInscEfr">CNPJ do Ente Federado Responsavel (EFR) — obrigatorio p/ ente publico; nulo se n/a.</param>
/// <param name="InicioValidade">Inicio de validade (<c>AAAA-MM</c>).</param>
public sealed record InsumoS1000(
    TipoInscricao TpInsc,
    string NrInsc,
    string NomeRazao,
    string ClassTrib,
    string? NrInscEfr,
    string InicioValidade);

/// <summary>
/// Dados de um estabelecimento/unidade para o S-1005. // TODO(validar-oficial: campos GILRAT/FAP que se
/// aplicam a orgao publico RPPS e estrutura exata do XSD S-1005).
/// </summary>
/// <param name="TpInsc">Tipo de inscricao do estabelecimento.</param>
/// <param name="NrInsc">Inscricao do estabelecimento (CNPJ/CAEPF/CNO).</param>
/// <param name="InicioValidade">Inicio de validade (<c>AAAA-MM</c>).</param>
/// <param name="CnaePrep">CNAE preponderante.</param>
/// <param name="AliqGilrat">Aliquota GILRAT (fracao). // TODO(validar-oficial: aplicabilidade a ente publico).</param>
public sealed record InsumoS1005(
    TipoInscricao TpInsc,
    string NrInsc,
    string InicioValidade,
    string CnaePrep,
    decimal? AliqGilrat);

/// <summary>
/// Snapshot de um servidor para o S-2200 (admissao). Origem: agregado <see cref="Servidores.Servidor"/>.
/// // TODO(validar-oficial: roteamento S-2200 x S-2300 por codCateg; campos exatos do XSD S-2200).
/// </summary>
/// <param name="CpfTrab">CPF do trabalhador (14? 11 digitos — sem mascara).</param>
/// <param name="NomeTrab">Nome civil.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="DataAdmissao">Data de admissao (nomeacao/posse — // TODO(validar-oficial: dtAdm = qual marco)).</param>
/// <param name="CodCateg">Categoria do trabalhador (Tabela 01). // TODO(validar-oficial: codCateg real do municipio).</param>
/// <param name="TpRegTrab">Tipo de regime trabalhista (S-1.3 <c>vinculo/tpRegTrab</c>, irmao de tpRegPrev): 1=CLT, 2=Estatutario/regimes proprios (Tabela TS_tpRegTrab do XSD S-1.3).</param>
/// <param name="TpRegPrev">Regime previdenciario (1=RGPS, 2=RPPS, 3=Exterior, 4=SPSMFA).</param>
/// <param name="CodCargo">Codigo do cargo.</param>
/// <param name="VrSalFx">Valor do salario fixo/vencimento.</param>
/// <param name="Empregador">Inscricao do empregador (<c>ideEmpregador</c>) — obrigatoria no S-1.3.</param>
/// <param name="TpProv">Tipo de provimento do estatutario (Tabela 14; ex.: 1=nomeacao em cargo efetivo). // TODO(validar-oficial: dominio).</param>
/// <param name="DataExercicio">Data de inicio de exercicio (<c>infoEstatutario/dtExercicio</c>) — OBRIGATORIA no XSD S-1.3 (sem ela o evento e rejeitado).</param>
public sealed record InsumoS2200(
    string CpfTrab,
    string NomeTrab,
    DateOnly DataNascimento,
    string Matricula,
    DateOnly DataAdmissao,
    string CodCateg,
    int TpRegTrab,
    int TpRegPrev,
    string CodCargo,
    decimal VrSalFx,
    InscricaoEmpregador Empregador,
    string TpProv,
    DateOnly DataExercicio);

/// <summary>
/// Inscricao do empregador/declarante para o grupo <c>ideEmpregador</c> presente em todos os eventos do
/// S-1.3 (S-1200/S-1210/S-1299/S-2200/S-2299...). Origem: <c>ParametrosESocial</c> do tenant.
/// </summary>
/// <param name="TpInsc">Tipo de inscricao (1=CNPJ p/ ente publico).</param>
/// <param name="NrInsc">Numero de inscricao (CNPJ do ente — base de 8 digitos quando aplicavel).</param>
public sealed record InscricaoEmpregador(int TpInsc, string NrInsc);

/// <summary>
/// Snapshot de desligamento para o S-2299. Origem: <see cref="Events.ServidorDesligado"/>.
/// // TODO(validar-oficial: dominio de mtvDeslig (Tabela 19) e grupo verbasResc).
/// </summary>
/// <param name="CpfTrab">CPF do trabalhador.</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="DataDesligamento">Data do desligamento.</param>
/// <param name="MtvDeslig">Motivo do desligamento (Tabela 19). // TODO(validar-oficial).</param>
public sealed record InsumoS2299(
    string CpfTrab,
    string Matricula,
    DateOnly DataDesligamento,
    string MtvDeslig);

/// <summary>Um item de verba (rubrica) do demonstrativo de remuneracao (<c>itensRemun</c> do S-1200/S-1202).</summary>
/// <param name="CodRubr">Codigo da rubrica (S-1010).</param>
/// <param name="IdeTabRubr">Identificador da tabela de rubricas. // TODO(validar-oficial).</param>
/// <param name="QtdRubr">Quantidade de referencia (ex.: 1).</param>
/// <param name="VrRubr">Valor da rubrica.</param>
/// <param name="IndApurIr">Indicativo de apuracao do IR. // TODO(validar-oficial: dominio).</param>
public sealed record ItemVerba(string CodRubr, string IdeTabRubr, decimal QtdRubr, decimal VrRubr, int IndApurIr);

/// <summary>
/// Snapshot de remuneracao de UM servidor numa competencia para o S-1200/S-1202. Origem:
/// <see cref="ResultadoCalculoServidor"/> + lancamentos (<see cref="Folha.EventoFolha"/>) da folha
/// fechada. // TODO(validar-oficial: grupos dmDev/infoPerApur/remunPerApur/itensRemun e base RPPS do XSD).
/// </summary>
/// <param name="CpfTrab">CPF do trabalhador.</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="CodCateg">Categoria do trabalhador.</param>
/// <param name="PerApur">Periodo de apuracao (<c>AAAA-MM</c>).</param>
/// <param name="Verbas">Itens de verba (por rubrica/incidencia).</param>
/// <param name="Empregador">Inscricao do empregador (<c>ideEmpregador</c>) — obrigatoria no S-1.3.</param>
/// <param name="EstabLotacao">Estabelecimento + lotacao do demonstrativo (<c>ideEstabLot</c>).</param>
public sealed record InsumoS1200(
    string CpfTrab,
    string Matricula,
    string CodCateg,
    string PerApur,
    IReadOnlyList<ItemVerba> Verbas,
    InscricaoEmpregador Empregador,
    EstabelecimentoLotacao EstabLotacao);

/// <summary>
/// Estabelecimento + codigo de lotacao tributaria do demonstrativo de remuneracao (<c>ideEstabLot</c> do
/// S-1200/S-1202). Para ente publico sem tabela de lotacoes propria, o <paramref name="CodLotacao"/> usa o
/// padrao acordado com o tenant. // TODO(validar-oficial): tabela de lotacoes S-1020 do ente.
/// </summary>
/// <param name="TpInsc">Tipo de inscricao do estabelecimento (1=CNPJ).</param>
/// <param name="NrInsc">Inscricao do estabelecimento (CNPJ do ente).</param>
/// <param name="CodLotacao">Codigo da lotacao tributaria (<c>codLotacao</c>).</param>
public sealed record EstabelecimentoLotacao(int TpInsc, string NrInsc, string CodLotacao);

/// <summary>
/// Snapshot de pagamento de UM servidor para o S-1210. Origem: <see cref="Events.PagamentoEfetuado"/> +
/// liquido por servidor. // TODO(validar-oficial: grupos infoPgto/detPgtoFl do XSD S-1210).
/// </summary>
/// <param name="CpfBenef">CPF do beneficiario.</param>
/// <param name="PerRef">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="DataPagamento">Data do pagamento.</param>
/// <param name="VrLiquido">Valor liquido pago.</param>
public sealed record InsumoS1210(
    string CpfBenef,
    string PerRef,
    DateOnly DataPagamento,
    decimal VrLiquido);

/// <summary>
/// Snapshot para o S-1299 (fechamento da competencia). Origem: orquestracao do gerador.
/// // TODO(validar-oficial: grupos ideRespInf/infoFech do XSD S-1299).
/// </summary>
/// <param name="PerApur">Periodo de apuracao (<c>AAAA-MM</c>).</param>
/// <param name="HouveRemun">Houve evento de remuneracao na competencia (<c>evtRemun</c>).</param>
/// <param name="HouvePgto">Houve evento de pagamento na competencia (<c>evtPgtos</c>).</param>
public sealed record InsumoS1299(string PerApur, bool HouveRemun, bool HouvePgto);

/// <summary>
/// Snapshot de UMA rubrica para o S-1010. Origem: agregado <see cref="RubricaFolha"/> (enriquecido).
/// Mantem fidelidade ao leiaute com os codigos de incidencia (Tabelas 03/20/21/23). // TODO(validar-oficial:
/// natRubr/codInc* exatos das tabelas do MOS — carregados, nunca hardcoded).
/// </summary>
/// <param name="CodRubr">Codigo da rubrica.</param>
/// <param name="IdeTabRubr">Identificador da tabela de rubricas.</param>
/// <param name="DscRubr">Descricao da rubrica.</param>
/// <param name="NatRubr">Natureza da rubrica (Tabela 03). // TODO(validar-oficial).</param>
/// <param name="TpRubr">Tipo (1=provento, 2=desconto, 3/4=informativa).</param>
/// <param name="CodIncCp">Incidencia previdenciaria (Tabela 20).</param>
/// <param name="CodIncIrrf">Incidencia IRRF (Tabela 21).</param>
/// <param name="CodIncFgts">Incidencia FGTS (Tabela 23).</param>
/// <param name="InicioValidade">Inicio de validade (<c>AAAA-MM</c>).</param>
public sealed record InsumoS1010(
    string CodRubr,
    string IdeTabRubr,
    string DscRubr,
    string NatRubr,
    int TpRubr,
    string CodIncCp,
    string CodIncIrrf,
    string CodIncFgts,
    string InicioValidade);
