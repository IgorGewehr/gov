using Tensorroot.Gov.Modules.Convenios.Domain.Comum;

namespace Tensorroot.Gov.Modules.Convenios.Application.Recebidos;

/// <summary>Etapa do plano de trabalho na superficie de aplicacao (fluxo A).</summary>
/// <param name="Ordem">Ordem da etapa.</param>
/// <param name="Descricao">Descricao/meta.</param>
/// <param name="Valor">Valor previsto.</param>
/// <param name="InicioPrevisto">Inicio previsto.</param>
/// <param name="FimPrevisto">Fim previsto.</param>
public sealed record EtapaPayload(int Ordem, string Descricao, decimal Valor, DateOnly InicioPrevisto, DateOnly FimPrevisto);

/// <summary>Parcela prevista do cronograma na superficie de aplicacao (fluxo A).</summary>
/// <param name="NumeroOrdem">Numero de ordem.</param>
/// <param name="Valor">Valor previsto.</param>
/// <param name="DataPrevista">Data prevista de liberacao.</param>
public sealed record ParcelaPayload(int NumeroOrdem, decimal Valor, DateOnly DataPrevista);

/// <summary>Dados do orgao concedente na superficie de aplicacao (fluxo A).</summary>
/// <param name="Cnpj">CNPJ (com ou sem mascara).</param>
/// <param name="Nome">Nome do orgao.</param>
/// <param name="Esfera">Esfera (Uniao/Estado).</param>
/// <param name="SistemaOrigem">Sistema de origem.</param>
public sealed record ConcedentePayload(string Cnpj, string Nome, EsferaConcedente Esfera, SistemaOrigemConvenio SistemaOrigem);
