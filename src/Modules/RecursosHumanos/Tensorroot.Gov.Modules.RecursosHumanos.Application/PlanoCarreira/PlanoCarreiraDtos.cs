using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;

/// <summary>Resumo de um plano de carreira (linha de lista).</summary>
/// <param name="Id">Identificador do plano.</param>
/// <param name="DenominacaoCarreira">Denominacao da carreira.</param>
/// <param name="LeiInstituicao">Lei que instituiu o plano.</param>
/// <param name="VencimentoBase">Vencimento da celula de ingresso.</param>
/// <param name="NumeroClasses">Quantidade de classes.</param>
/// <param name="NumeroReferencias">Quantidade de referencias por classe.</param>
/// <param name="Situacao">Situacao do plano.</param>
public sealed record PlanoCarreiraResumo(
    Guid Id,
    string DenominacaoCarreira,
    string LeiInstituicao,
    decimal VencimentoBase,
    int NumeroClasses,
    int NumeroReferencias,
    string Situacao);

/// <summary>Celula da matriz salarial (posicao + vencimento derivado).</summary>
/// <param name="Classe">Classe (vertical).</param>
/// <param name="Referencia">Referencia (horizontal).</param>
/// <param name="Vencimento">Vencimento derivado da celula.</param>
public sealed record CelulaMatriz(int Classe, int Referencia, decimal Vencimento);

/// <summary>Detalhe de um plano de carreira (parametros + a matriz salarial completa derivada).</summary>
/// <param name="Resumo">Resumo do plano.</param>
/// <param name="PercentualEntreReferencias">Percentual entre referencias.</param>
/// <param name="PercentualEntreClasses">Percentual entre classes.</param>
/// <param name="IntersticioMeses">Interstncio (meses) da progressao.</param>
/// <param name="NotaMinimaProgressao">Nota minima da progressao por merecimento.</param>
/// <param name="Matriz">Matriz salarial (todas as celulas com vencimento derivado).</param>
public sealed record PlanoCarreiraDetalhe(
    PlanoCarreiraResumo Resumo,
    decimal PercentualEntreReferencias,
    decimal PercentualEntreClasses,
    int IntersticioMeses,
    decimal NotaMinimaProgressao,
    IReadOnlyList<CelulaMatriz> Matriz);

/// <summary>Item do historico de movimentacoes funcionais (linha do livro-razao da carreira).</summary>
/// <param name="Tipo">Natureza da movimentacao.</param>
/// <param name="ClasseOrigem">Classe de origem (nula no enquadramento).</param>
/// <param name="ReferenciaOrigem">Referencia de origem (nula no enquadramento).</param>
/// <param name="ClasseDestino">Classe de destino.</param>
/// <param name="ReferenciaDestino">Referencia de destino.</param>
/// <param name="VencimentoResultante">Vencimento resultante.</param>
/// <param name="Criterio">Criterio da progressao (nulo nos demais).</param>
/// <param name="DataEfeito">Data de efeito.</param>
/// <param name="Fundamento">Fundamento da movimentacao.</param>
public sealed record MovimentacaoCarreiraItem(
    string Tipo,
    int? ClasseOrigem,
    int? ReferenciaOrigem,
    int ClasseDestino,
    int ReferenciaDestino,
    decimal VencimentoResultante,
    string? Criterio,
    DateOnly DataEfeito,
    string Fundamento);

/// <summary>Enquadramento vigente de um servidor + historico de movimentacoes.</summary>
/// <param name="EnquadramentoId">Identificador do enquadramento.</param>
/// <param name="ServidorId">Servidor enquadrado.</param>
/// <param name="PlanoCarreiraId">Plano de carreira.</param>
/// <param name="DenominacaoCarreira">Denominacao da carreira.</param>
/// <param name="ClasseAtual">Classe vigente.</param>
/// <param name="ReferenciaAtual">Referencia vigente.</param>
/// <param name="VencimentoAtual">Vencimento da posicao vigente.</param>
/// <param name="PermiteProgressao">Indica se ha referencia seguinte.</param>
/// <param name="PermitePromocao">Indica se ha classe seguinte.</param>
/// <param name="Movimentacoes">Historico (mais recente por ultimo).</param>
public sealed record EnquadramentoDetalhe(
    Guid EnquadramentoId,
    Guid ServidorId,
    Guid PlanoCarreiraId,
    string DenominacaoCarreira,
    int ClasseAtual,
    int ReferenciaAtual,
    decimal VencimentoAtual,
    bool PermiteProgressao,
    bool PermitePromocao,
    IReadOnlyList<MovimentacaoCarreiraItem> Movimentacoes);

/// <summary>Projecoes do dominio do plano de carreira para os DTOs de leitura.</summary>
internal static class ProjetarPlanoCarreira
{
    internal static PlanoCarreiraResumo ParaResumo(Domain.PlanoCarreira.PlanoCarreira plano) => new(
        plano.Id.Value,
        plano.DenominacaoCarreira,
        plano.LeiInstituicao,
        plano.VencimentoBase.Valor,
        plano.NumeroClasses,
        plano.NumeroReferencias,
        plano.Situacao.ToString());

    internal static PlanoCarreiraDetalhe ParaDetalhe(Domain.PlanoCarreira.PlanoCarreira plano)
    {
        var matriz = new List<CelulaMatriz>(plano.NumeroClasses * plano.NumeroReferencias);
        for (var classe = 1; classe <= plano.NumeroClasses; classe++)
        {
            for (var referencia = 1; referencia <= plano.NumeroReferencias; referencia++)
            {
                var vencimento = plano.VencimentoDa(PosicaoCarreira.De(classe, referencia));
                matriz.Add(new CelulaMatriz(classe, referencia, vencimento.Valor));
            }
        }

        return new PlanoCarreiraDetalhe(
            ParaResumo(plano),
            plano.PercentualEntreReferencias,
            plano.PercentualEntreClasses,
            plano.IntersticioMeses,
            plano.NotaMinimaProgressao,
            matriz);
    }

    internal static EnquadramentoDetalhe ParaDetalhe(EnquadramentoServidor enquadramento, Domain.PlanoCarreira.PlanoCarreira plano)
    {
        var posicao = enquadramento.PosicaoAtual;
        var movimentacoes = enquadramento.Movimentacoes
            .OrderBy(m => m.DataEfeito)
            .Select(m => new MovimentacaoCarreiraItem(
                m.Tipo.ToString(),
                m.ClasseOrigem,
                m.ReferenciaOrigem,
                m.ClasseDestino,
                m.ReferenciaDestino,
                m.VencimentoResultante,
                m.Criterio?.ToString(),
                m.DataEfeito,
                m.Fundamento))
            .ToList();

        return new EnquadramentoDetalhe(
            enquadramento.Id.Value,
            enquadramento.ServidorId.Value,
            enquadramento.PlanoCarreiraId.Value,
            plano.DenominacaoCarreira,
            enquadramento.ClasseAtual,
            enquadramento.ReferenciaAtual,
            plano.VencimentoDa(posicao).Valor,
            plano.PermiteProgressao(posicao),
            plano.PermitePromocao(posicao),
            movimentacoes);
    }
}
