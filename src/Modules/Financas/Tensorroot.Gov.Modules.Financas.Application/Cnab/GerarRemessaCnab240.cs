using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Cnab;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Cnab;

/// <summary>Dados do ente pagador para a remessa (convênio/DVs vêm do acordo bancário do tenant).</summary>
/// <param name="CodigoBanco">Código COMPE do banco.</param>
/// <param name="TipoInscricao">1 = CPF, 2 = CNPJ.</param>
/// <param name="NumeroInscricao">CNPJ/CPF do ente.</param>
/// <param name="Convenio">Código do convênio.</param>
/// <param name="DvAgencia">Dígito da agência.</param>
/// <param name="DvConta">Dígito da conta.</param>
/// <param name="DvAgenciaConta">Dígito agência/conta.</param>
/// <param name="NomeEmpresa">Nome do ente.</param>
public sealed record PagadorPayload(
    string CodigoBanco,
    int TipoInscricao,
    string NumeroInscricao,
    string Convenio,
    string DvAgencia,
    string DvConta,
    string DvAgenciaConta,
    string NomeEmpresa);

/// <summary>
/// Gera a remessa CNAB240 (FEBRABAN) de pagamento a fornecedores/servidores a partir de uma ordem de
/// pagamento. Resolve os favorecidos pelas liquidações da ordem → empenho → credor cadastrado (dados
/// bancários). O valor por favorecido é o líquido (descontadas as retenções). A TRANSMISSÃO ao banco é
/// diferida (M10); aqui produz-se o arquivo de remessa.
/// </summary>
/// <param name="OrdemDePagamentoId">Ordem de pagamento.</param>
/// <param name="FormaLancamento">Forma de lançamento (1=CC, 3=DOC/TED, 45=PIX).</param>
/// <param name="SequencialArquivo">Sequencial de controle do arquivo.</param>
/// <param name="Pagador">Dados do ente pagador.</param>
public sealed record GerarRemessaCnab240Query(
    Guid OrdemDePagamentoId,
    int FormaLancamento,
    int SequencialArquivo,
    PagadorPayload Pagador) : IQuery<RemessaCnab240Dto>;

/// <summary>Resultado da geração da remessa CNAB240.</summary>
/// <param name="NomeArquivo">Nome sugerido do arquivo.</param>
/// <param name="Conteudo">Conteúdo textual (registros de 240 posições).</param>
/// <param name="QuantidadeFavorecidos">Quantidade de favorecidos.</param>
/// <param name="ValorTotal">Valor total da remessa (líquido).</param>
public sealed record RemessaCnab240Dto(
    string NomeArquivo,
    string Conteudo,
    int QuantidadeFavorecidos,
    decimal ValorTotal);

/// <summary>Handler da geração da remessa CNAB240.</summary>
public sealed class GerarRemessaCnab240Handler(
    IOrdemDePagamentoRepository ordens,
    ILiquidacaoRepository liquidacoes,
    IEmpenhoRepository empenhos,
    ICredorRepository credores) : IQueryHandler<GerarRemessaCnab240Query, RemessaCnab240Dto>
{
    /// <inheritdoc />
    public async Task<RemessaCnab240Dto> Handle(GerarRemessaCnab240Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ordem = await ordens.ObterPorIdAsync(new OrdemDePagamentoId(request.OrdemDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ordem de pagamento nao encontrada.");

        // Agrupa o líquido por credor (documento), resolvendo via liquidação → empenho → credor.
        var liquidoPorCredor = new Dictionary<string, (string Nome, TipoPessoa Tipo, decimal Valor)>(StringComparer.Ordinal);
        foreach (var item in ordem.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(item.LiquidacaoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Liquidacao {item.LiquidacaoId} nao encontrada.");

            var empenho = await empenhos.ObterPorIdAsync(liquidacao.EmpenhoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Empenho da liquidacao nao encontrado.");

            // Valor líquido do item = proporção do item já é o pago; descontamos as retenções da liquidação
            // na proporção do item sobre o valor liquidado (no PoC, item costuma quitar a liquidação inteira).
            var liquidoItem = ProporcaoLiquida(liquidacao.Valor, liquidacao.TotalRetido, item.Valor);

            var doc = empenho.Credor.Documento;
            if (liquidoPorCredor.TryGetValue(doc, out var atual))
            {
                liquidoPorCredor[doc] = (atual.Nome, atual.Tipo, atual.Valor + liquidoItem);
            }
            else
            {
                liquidoPorCredor[doc] = (empenho.Credor.Nome, empenho.Credor.Tipo, liquidoItem);
            }
        }

        var favorecidos = new List<FavorecidoCnab>();
        foreach (var (documento, dados) in liquidoPorCredor)
        {
            if (dados.Valor <= 0m)
            {
                continue;
            }

            var credor = await credores.ObterPorDocumentoAsync(documento, cancellationToken).ConfigureAwait(false);
            var banco = credor?.DadosBancarios;
            if (banco is null)
            {
                throw new InvalidOperationException(
                    $"Credor {dados.Nome} (doc {documento}) sem dados bancarios cadastrados; impossivel gerar CNAB240.");
            }

            favorecidos.Add(new FavorecidoCnab(
                CodigoBancoFavorecido: banco.Banco,
                AgenciaFavorecido: banco.Agencia,
                DvAgenciaFavorecido: string.Empty,
                ContaFavorecido: banco.Conta,
                DvContaFavorecido: string.Empty,
                NomeFavorecido: dados.Nome,
                TipoInscricaoFavorecido: dados.Tipo == TipoPessoa.Juridica ? TipoInscricaoCnab.Cnpj : TipoInscricaoCnab.Cpf,
                NumeroInscricaoFavorecido: documento,
                NumeroDocumento: ordem.Numero,
                DataPagamento: ordem.DataPagamento,
                Valor: dados.Valor,
                ChavePix: banco.Pix));
        }

        var pagador = new PagadorCnab(
            request.Pagador.CodigoBanco,
            (TipoInscricaoCnab)request.Pagador.TipoInscricao,
            request.Pagador.NumeroInscricao,
            request.Pagador.Convenio,
            ordem.ContaBancaria.Agencia,
            request.Pagador.DvAgencia,
            ordem.ContaBancaria.Conta,
            request.Pagador.DvConta,
            request.Pagador.DvAgenciaConta,
            request.Pagador.NomeEmpresa);

        var remessa = new RemessaCnab240(
            pagador,
            (FormaLancamentoCnab)request.FormaLancamento,
            ordem.DataPagamento,
            TimeOnly.FromDateTime(DateTime.UtcNow),
            request.SequencialArquivo,
            favorecidos);

        var conteudo = Cnab240Writer.Gerar(remessa);
        var nomeArquivo = $"CNAB240_{ordem.Numero}_{ordem.DataPagamento:yyyyMMdd}.rem";
        return new RemessaCnab240Dto(nomeArquivo, conteudo, favorecidos.Count, favorecidos.Sum(f => f.Valor));
    }

    // Líquido proporcional do item: item × (1 − totalRetido/valorLiquidado). Robusto a valor zero.
    private static decimal ProporcaoLiquida(ValorMonetario valorLiquidacao, ValorMonetario totalRetido, ValorMonetario valorItem)
    {
        if (!valorLiquidacao.EhPositivo() || !totalRetido.EhPositivo())
        {
            return valorItem.Valor;
        }

        var fatorLiquido = 1m - (totalRetido.Valor / valorLiquidacao.Valor);
        return decimal.Round(valorItem.Valor * fatorLiquido, 2, MidpointRounding.AwayFromZero);
    }
}
