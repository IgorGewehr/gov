using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.SicapPessoal;

/// <summary>
/// Abre uma remessa de auditoria de pessoal ao TCE-RS (SICAP-AP / SIAPESweb): o lote de atos de
/// admissao. O sequencial do lote (NRO_MOV) e' apurado por orgao no tenant (o cliente NAO informa).
/// </summary>
/// <param name="CodigoOrgao">Codigo do orgao remetente (CD_ORGAO).</param>
/// <param name="DataGeracaoLote">Data de geracao do lote; quando nula, usa o "hoje" do tenant.</param>
/// <param name="VersaoLeiaute">Versao do leiaute (padrao 57 posicoes).</param>
public sealed record AbrirRemessaSicapCommand(
    int CodigoOrgao,
    DateOnly? DataGeracaoLote,
    int? VersaoLeiaute) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de remessa.</summary>
public sealed class AbrirRemessaSicapValidator : AbstractValidator<AbrirRemessaSicapCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirRemessaSicapValidator()
    {
        RuleFor(comando => comando.CodigoOrgao).GreaterThan(0);
        RuleFor(comando => comando.VersaoLeiaute)
            .GreaterThan(0)
            .When(comando => comando.VersaoLeiaute is not null);
    }
}

/// <summary>Handler da abertura de remessa.</summary>
public sealed class AbrirRemessaSicapHandler(
    IRemessaSicapPessoalRepository remessas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<AbrirRemessaSicapCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirRemessaSicapCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sequencial = await remessas.ProximoSequencialAsync(request.CodigoOrgao, cancellationToken).ConfigureAwait(false);
        var dataGeracao = request.DataGeracaoLote ?? dataHoje.Hoje();

        var remessa = RemessaSicapPessoal.Abrir(
            tenant.TenantId,
            request.CodigoOrgao,
            sequencial,
            dataGeracao,
            request.VersaoLeiaute ?? RemessaSicapPessoal.VersaoLeiautePadrao);

        remessas.Adicionar(remessa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return remessa.Id.Value;
    }
}
