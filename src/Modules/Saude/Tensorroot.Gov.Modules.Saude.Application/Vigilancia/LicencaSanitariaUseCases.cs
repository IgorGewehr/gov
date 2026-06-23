using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>
/// Emite uma licenca/alvara sanitario para um estabelecimento. Regra de negocio que amarra as maquinas de
/// estado: estabelecimentos de risco MEDIO/ALTO exigem inspecao CONCLUIDA e NAO reprovada (aprovada ou com
/// pendencias sanaveis); risco BAIXO dispensa licenciamento previo (Lei 13.874/2019) e pode emitir por mero
/// registro (sem inspecao). O estabelecimento precisa estar fiscalizavel (ativo/nao interditado).
/// </summary>
/// <param name="EstabelecimentoId">Estabelecimento a licenciar.</param>
/// <param name="Numero">Numero do alvara.</param>
/// <param name="ValidadeAte">Data de validade.</param>
/// <param name="InspecaoId">Inspecao fundante (obrigatoria para risco medio/alto).</param>
public sealed record EmitirLicencaCommand(
    Guid EstabelecimentoId,
    string Numero,
    DateOnly ValidadeAte,
    Guid? InspecaoId) : ICommand<Guid>;

/// <summary>Regras de validacao da emissao de licenca.</summary>
public sealed class EmitirLicencaValidator : AbstractValidator<EmitirLicencaCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirLicencaValidator()
    {
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da emissao de licenca.</summary>
public sealed class EmitirLicencaHandler(
    IEstabelecimentoFiscalizavelRepository estabelecimentos,
    IInspecaoRepository inspecoes,
    ILicencaSanitariaRepository licencas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EmitirLicencaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmitirLicencaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        if (!estabelecimento.EstaFiscalizavel())
        {
            throw new InvalidOperationException("Estabelecimento inativo/interditado nao pode ser licenciado.");
        }

        InspecaoId? inspecaoFundante = null;

        // Risco medio/alto exige inspecao concluida e nao reprovada; baixo risco dispensa (Lei 13.874/2019).
        if (!estabelecimento.DispensaLicenciamentoPrevio())
        {
            if (request.InspecaoId is not { } inspId)
            {
                throw new InvalidOperationException("Estabelecimento de risco medio/alto exige inspecao para licenciamento.");
            }

            var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(inspId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Inspecao fundante nao encontrada.");

            if (inspecao.EstabelecimentoFiscalizavelId.Value != request.EstabelecimentoId)
            {
                throw new InvalidOperationException("Inspecao nao pertence ao estabelecimento informado.");
            }

            if (!inspecao.HabilitaLicenca())
            {
                throw new InvalidOperationException("Inspecao reprovada ou nao concluida nao habilita licenca.");
            }

            inspecaoFundante = inspecao.Id;
        }
        else if (request.InspecaoId is { } inspId)
        {
            // Baixo risco pode anexar a inspecao se houver (opcional), apenas validando a pertinencia.
            var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(inspId), cancellationToken).ConfigureAwait(false);
            if (inspecao is not null && inspecao.EstabelecimentoFiscalizavelId.Value == request.EstabelecimentoId)
            {
                inspecaoFundante = inspecao.Id;
            }
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var licenca = LicencaSanitaria.Emitir(
            tenant.TenantId, estabelecimento.Id, request.Numero, hoje, request.ValidadeAte, inspecaoFundante);

        licencas.Adicionar(licenca);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return licenca.Id.Value;
    }
}

/// <summary>
/// Renova uma licenca, gerando uma NOVA licenca vigente a partir da anterior (a antiga segue historica).
/// As mesmas regras de inspecao da emissao se aplicam ao risco do estabelecimento.
/// </summary>
/// <param name="LicencaId">Licenca a renovar.</param>
/// <param name="Numero">Numero da nova licenca.</param>
/// <param name="ValidadeAte">Nova validade.</param>
/// <param name="InspecaoId">Inspecao da renovacao (obrigatoria para risco medio/alto).</param>
public sealed record RenovarLicencaCommand(
    Guid LicencaId,
    string Numero,
    DateOnly ValidadeAte,
    Guid? InspecaoId) : ICommand<Guid>;

/// <summary>Regras de validacao da renovacao.</summary>
public sealed class RenovarLicencaValidator : AbstractValidator<RenovarLicencaCommand>
{
    /// <summary>Define as regras.</summary>
    public RenovarLicencaValidator()
    {
        RuleFor(c => c.LicencaId).NotEmpty();
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da renovacao de licenca.</summary>
public sealed class RenovarLicencaHandler(
    IEstabelecimentoFiscalizavelRepository estabelecimentos,
    IInspecaoRepository inspecoes,
    ILicencaSanitariaRepository licencas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RenovarLicencaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RenovarLicencaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atual = await licencas.ObterPorIdAsync(new LicencaSanitariaId(request.LicencaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licenca nao encontrada.");

        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(atual.EstabelecimentoFiscalizavelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        if (!estabelecimento.EstaFiscalizavel())
        {
            throw new InvalidOperationException("Estabelecimento inativo/interditado nao pode renovar licenca.");
        }

        InspecaoId? inspecaoFundante = null;
        if (!estabelecimento.DispensaLicenciamentoPrevio())
        {
            if (request.InspecaoId is not { } inspId)
            {
                throw new InvalidOperationException("Renovacao de risco medio/alto exige inspecao.");
            }

            var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(inspId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Inspecao fundante nao encontrada.");

            if (inspecao.EstabelecimentoFiscalizavelId != atual.EstabelecimentoFiscalizavelId || !inspecao.HabilitaLicenca())
            {
                throw new InvalidOperationException("Inspecao invalida para a renovacao.");
            }

            inspecaoFundante = inspecao.Id;
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var nova = atual.Renovar(request.Numero, hoje, request.ValidadeAte, inspecaoFundante);

        licencas.Adicionar(nova);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return nova.Id.Value;
    }
}

/// <summary>Cassa/revoga uma licenca (ato da VISA, em regra apos auto de penalidade).</summary>
/// <param name="LicencaId">Licenca a cassar.</param>
/// <param name="Motivo">Motivo da cassacao.</param>
public sealed record CassarLicencaCommand(Guid LicencaId, string Motivo) : ICommand;

/// <summary>Regras de validacao da cassacao.</summary>
public sealed class CassarLicencaValidator : AbstractValidator<CassarLicencaCommand>
{
    /// <summary>Define as regras.</summary>
    public CassarLicencaValidator()
    {
        RuleFor(c => c.LicencaId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler da cassacao de licenca.</summary>
public sealed class CassarLicencaHandler(ILicencaSanitariaRepository licencas, IUnitOfWork unitOfWork)
    : ICommandHandler<CassarLicencaCommand>
{
    /// <inheritdoc />
    public async Task Handle(CassarLicencaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licenca = await licencas.ObterPorIdAsync(new LicencaSanitariaId(request.LicencaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licenca nao encontrada.");

        licenca.Cassar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
