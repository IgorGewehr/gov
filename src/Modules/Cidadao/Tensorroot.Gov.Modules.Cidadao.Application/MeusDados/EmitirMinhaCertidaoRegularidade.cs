using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// MINHA CERTIDAO DE REGULARIDADE (CND/CPEN — CTN arts. 205/206): emite, em AUTOSSERVICO online, a
/// certidao de regularidade fiscal do PROPRIO cidadao autenticado (balcao online de altissimo uso). O
/// documento e SEMPRE resolvido server-side (ancora dado-proprio) — o cliente nada informa. Dado pessoal
/// (LGPD): implementa <see cref="ISensivelLgpd"/> e gera trilha de acesso.
/// </summary>
public sealed record EmitirMinhaCertidaoRegularidadeCommand
    : ICommand<MinhaCertidaoRegularidadeDto?>, ISensivelLgpd
{
    /// <summary>
    /// Fundamento legal padrao da certidao emitida pelo autosservico (CTN arts. 205/206 + CTM). // TODO(validar-oficial):
    /// citar o artigo do Codigo Tributario Municipal de Maximiliano de Almeida/RS.
    /// </summary>
    public const string FundamentoLegalPadrao = "CTN arts. 205 e 206; Codigo Tributario Municipal.";

    /// <inheritdoc />
    public string EntidadeSensivel => "MinhaCertidaoRegularidade";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler de "minha certidao de regularidade" — delega ao Tributos (Contracts) com a pessoa resolvida.</summary>
public sealed class EmitirMinhaCertidaoRegularidadeHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada)
    : ICommandHandler<EmitirMinhaCertidaoRegularidadeCommand, MinhaCertidaoRegularidadeDto?>
{
    /// <inheritdoc />
    public async Task<MinhaCertidaoRegularidadeDto?> Handle(EmitirMinhaCertidaoRegularidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ANCORA: pessoa do PROPRIO cidadao autenticado — nunca um documento/id do cliente.
        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);

        // A emissao no Tributos roda em ESCOPO DEDICADO (guarda H5): o documento JA resolvido e o unico
        // dado que cruza — o isolamento dado-proprio nao e afetado. A persistencia ocorre no escopo do
        // TributosDbContext (SaveChanges la dentro), preservando os Global Query Filters.
        return await consultaDedicada
            .ExecutarAsync<IConsultaTributariaCidadao, MinhaCertidaoRegularidadeDto?>(
                (consulta, ct) => consulta.EmitirMinhaCertidaoRegularidadeAsync(
                    pessoa.Documento,
                    EmitirMinhaCertidaoRegularidadeCommand.FundamentoLegalPadrao,
                    ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
