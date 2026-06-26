using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>
/// Divulga o EDITAL da licitacao no Portal Nacional de Contratacoes Publicas (divulgacao obrigatoria do
/// edital — art. 54/174). L4: NAO recebe mais o numero de controle de fora — a transmissao e feita pela
/// ACL (<see cref="IPncpGateway"/>), que executa o pre-cadastro orgao/unidade/compra e devolve o
/// NUMERO DE CONTROLE PNCP DA COMPRA, gravado no agregado. (Antes o handler so persistia um numero
/// informado, sem transmitir — edital nunca chegava ao PNCP.)
/// </summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="CnpjOrgao">CNPJ do orgao/entidade comprador (pre-cadastro PNCP).</param>
/// <param name="CodigoUnidade">Codigo da unidade administrativa compradora.</param>
/// <param name="AnoCompra">Ano da contratacao.</param>
/// <param name="NumeroCompra">Numero da contratacao no sistema de origem (ex.: "0007/2026").</param>
/// <param name="ModalidadeId">Codigo da modalidade de contratacao (tabela de dominio PNCP).</param>
/// <param name="ModoDisputaId">Codigo do modo de disputa (tabela de dominio PNCP).</param>
/// <param name="AmparoLegalCodigo">Codigo do amparo legal (tabela de dominio PNCP).</param>
public sealed record PublicarEditalNoPncpCommand(
    Guid LicitacaoId,
    string CnpjOrgao,
    string CodigoUnidade,
    int AnoCompra,
    string NumeroCompra,
    int ModalidadeId,
    int ModoDisputaId,
    string AmparoLegalCodigo) : ICommand;

/// <summary>Regras de validacao da publicacao do edital no PNCP (campos do pre-cadastro da compra).</summary>
public sealed class PublicarEditalNoPncpValidator : AbstractValidator<PublicarEditalNoPncpCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarEditalNoPncpValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.CnpjOrgao).NotEmpty().MaximumLength(14);
        RuleFor(comando => comando.CodigoUnidade).NotEmpty().MaximumLength(20);
        RuleFor(comando => comando.NumeroCompra).NotEmpty().MaximumLength(50);
        RuleFor(comando => comando.AnoCompra).GreaterThan(0).WithMessage("anoCompra e obrigatorio.");
        RuleFor(comando => comando.ModalidadeId).GreaterThan(0).WithMessage("modalidadeId (tabela de dominio PNCP) e obrigatorio.");
        RuleFor(comando => comando.AmparoLegalCodigo).NotEmpty().WithMessage("amparoLegal (tabela de dominio PNCP) e obrigatorio.");
    }
}

/// <summary>
/// Handler da divulgacao do edital no PNCP: transmite via <see cref="IPncpGateway"/> (idempotente + Polly)
/// e grava o numero de controle PNCP da compra no agregado.
/// <para>
/// FRONTEIRA M9/M10: no M9 a <see cref="IPncpGateway"/> e a impl. SIMULADA. // TODO(M10): transmissao real
/// (JWT/credenciais no Key Vault; validacao em treina.pncp.gov.br).
/// </para>
/// </summary>
public sealed class PublicarEditalNoPncpHandler(
    ILicitacaoRepository licitacoes,
    IPncpGateway pncpGateway,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarEditalNoPncpCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarEditalNoPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        // Idempotencia: edital ja divulgado => no-op (o gateway tambem e idempotente, mas evitamos transmissao).
        if (!string.IsNullOrWhiteSpace(licitacao.NumeroEditalPncp))
        {
            return;
        }

        var requisicao = new PublicacaoEditalPncpRequest(
            LicitacaoId: licitacao.Id.Value,
            CnpjOrgao: request.CnpjOrgao,
            CodigoUnidade: request.CodigoUnidade,
            AnoCompra: request.AnoCompra,
            NumeroCompra: request.NumeroCompra,
            ModalidadeId: request.ModalidadeId,
            ModoDisputaId: request.ModoDisputaId,
            AmparoLegalCodigo: request.AmparoLegalCodigo,
            ObjetoCompra: licitacao.Objeto,
            ValorTotalEstimado: licitacao.ValorEstimado.Valor,
            ChaveIdempotencia: $"edital:{licitacao.Id.Value:N}");

        var resultado = await pncpGateway.PublicarEditalAsync(requisicao, cancellationToken).ConfigureAwait(false);
        if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.NumeroControlePncpCompra))
        {
            throw new InvalidOperationException(
                $"Falha ao divulgar edital no PNCP ({resultado.CodigoErro}): {resultado.MensagemErro}");
        }

        licitacao.PublicarEditalPncp(resultado.NumeroControlePncpCompra);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
