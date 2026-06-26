using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Dados de lotacao/estabelecimento de exercicio do cargo (entrada).</summary>
/// <param name="InscricaoEstabelecimento">Inscricao do estabelecimento (S-1005).</param>
/// <param name="DenominacaoUnidade">Denominacao da unidade de exercicio.</param>
/// <param name="CodigoLotacaoTributaria">Codigo da lotacao tributaria (S-1020), opcional.</param>
public sealed record LotacaoDto(string InscricaoEstabelecimento, string DenominacaoUnidade, string? CodigoLotacaoTributaria);

/// <summary>Cria um cargo publico na estrutura de pessoal.</summary>
/// <param name="Denominacao">Denominacao legal do cargo.</param>
/// <param name="Tipo">Tipo (efetivo/comissionado/temporario).</param>
/// <param name="Vencimento">Remuneracao-base do cargo.</param>
/// <param name="Lotacao">Lotacao/estabelecimento de exercicio.</param>
/// <param name="QuantidadeVagas">Vagas autorizadas em lei.</param>
/// <param name="LeiCriacao">Lei de criacao do cargo.</param>
/// <param name="PlanoDeCargosId">Plano de cargos a que pertence (opcional).</param>
public sealed record CriarCargoCommand(
    string Denominacao,
    TipoCargo Tipo,
    decimal Vencimento,
    LotacaoDto Lotacao,
    int QuantidadeVagas,
    string LeiCriacao,
    Guid? PlanoDeCargosId) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de cargo.</summary>
public sealed class CriarCargoValidator : AbstractValidator<CriarCargoCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarCargoValidator()
    {
        RuleFor(comando => comando.Denominacao)
            .NotEmpty()
            .MaximumLength(120)
            .WithMessage("Denominacao do cargo e obrigatoria (max. 120 caracteres).");

        RuleFor(comando => comando.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de cargo invalido.");

        RuleFor(comando => comando.Vencimento)
            .GreaterThan(0)
            .WithMessage("Vencimento deve ser maior que zero.");

        RuleFor(comando => comando.Lotacao)
            .NotNull()
            .WithMessage("Lotacao e obrigatoria.");

        RuleFor(comando => comando.QuantidadeVagas)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantidade de vagas deve ser ao menos 1.");

        RuleFor(comando => comando.LeiCriacao)
            .NotEmpty()
            .MaximumLength(80)
            .WithMessage("Lei de criacao e obrigatoria.");
    }
}

/// <summary>Handler da criacao de cargo.</summary>
public sealed class CriarCargoHandler(
    ICargoRepository cargos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IPoliticaPrevidenciariaProvider politicaPrevidenciaria)
    : ICommandHandler<CriarCargoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarCargoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Lotacao);

        var lotacao = Lotacao.Criar(
            request.Lotacao.InscricaoEstabelecimento,
            request.Lotacao.DenominacaoUnidade,
            request.Lotacao.CodigoLotacaoTributaria);

        // Regime previdenciario do cargo = politica do ente (tem RPPS proprio?), nunca derivacao fixa.
        var politica = await politicaPrevidenciaria.ObterAsync(cancellationToken).ConfigureAwait(false);

        var cargo = Cargo.Criar(
            tenant.TenantId,
            request.Denominacao,
            request.Tipo,
            Vencimento.De(request.Vencimento),
            lotacao,
            request.QuantidadeVagas,
            request.LeiCriacao,
            politica,
            request.PlanoDeCargosId is { } plano ? new PlanoDeCargosId(plano) : null);

        cargos.Adicionar(cargo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return cargo.Id.Value;
    }
}
