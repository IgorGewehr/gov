using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;

namespace Tensorroot.Gov.Modules.Tributos.Application.Sim;

/// <summary>Produto inspecionado a habilitar no requerimento do S.I.M. (entrada).</summary>
/// <param name="Denominacao">Denominação de venda do produto.</param>
/// <param name="Classificacao">Classificação sanitária do produto.</param>
/// <param name="NumeroRotulo">Número do rótulo aprovado.</param>
public sealed record ProdutoSimInput(string Denominacao, string Classificacao, string NumeroRotulo);

/// <summary>
/// Protocola o requerimento de registro de um estabelecimento no Serviço de Inspeção Municipal (S.I.M.):
/// abre o título em análise e habilita os produtos inspecionados informados (com rótulo). O número do
/// S.I.M. só é atribuído na concessão. Paridade com o incumbente SAPI.
/// </summary>
/// <param name="ResponsavelId">Contribuinte responsável legal pelo estabelecimento.</param>
/// <param name="RazaoSocialEstabelecimento">Razão social/nome do estabelecimento.</param>
/// <param name="Natureza">Natureza do produto (origem animal/vegetal).</param>
/// <param name="EnderecoEstabelecimento">Endereço do estabelecimento.</param>
/// <param name="DataRequerimento">Data do requerimento.</param>
/// <param name="Produtos">Produtos inspecionados a habilitar.</param>
public sealed record RequererTituloSimCommand(
    Guid ResponsavelId,
    string RazaoSocialEstabelecimento,
    NaturezaProdutoSim Natureza,
    string EnderecoEstabelecimento,
    DateOnly DataRequerimento,
    IReadOnlyList<ProdutoSimInput> Produtos) : ICommand<Guid>;

/// <summary>Regras de validação do requerimento de título do S.I.M.</summary>
public sealed class RequererTituloSimValidator : AbstractValidator<RequererTituloSimCommand>
{
    /// <summary>Define as regras.</summary>
    public RequererTituloSimValidator()
    {
        RuleFor(c => c.ResponsavelId).NotEmpty();
        RuleFor(c => c.RazaoSocialEstabelecimento).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Natureza).IsInEnum();
        RuleFor(c => c.EnderecoEstabelecimento).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Produtos).NotEmpty();
        RuleForEach(c => c.Produtos).ChildRules(produto =>
        {
            produto.RuleFor(p => p.Denominacao).NotEmpty().MaximumLength(200);
            produto.RuleFor(p => p.Classificacao).NotEmpty().MaximumLength(120);
            produto.RuleFor(p => p.NumeroRotulo).NotEmpty().MaximumLength(40);
        });
    }
}

/// <summary>Handler do requerimento de título do S.I.M.</summary>
public sealed class RequererTituloSimHandler(
    IContribuinteRepository contribuintes,
    ITituloRegistroSimRepository titulos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RequererTituloSimCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RequererTituloSimCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var responsavelId = new ContribuinteId(request.ResponsavelId);
        _ = await contribuintes.ObterPorIdAsync(responsavelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte responsável não encontrado.");

        var titulo = TituloRegistroSim.Requerer(
            tenant.TenantId,
            responsavelId,
            request.RazaoSocialEstabelecimento,
            request.Natureza,
            request.EnderecoEstabelecimento,
            request.DataRequerimento);

        foreach (var produto in request.Produtos)
        {
            titulo.HabilitarProduto(produto.Denominacao, produto.Classificacao, produto.NumeroRotulo);
        }

        titulos.Adicionar(titulo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return titulo.Id.Value;
    }
}

/// <summary>Resultado da concessão de um registro no S.I.M.</summary>
/// <param name="TituloId">Título concedido.</param>
/// <param name="NumeroSim">Número do S.I.M. atribuído.</param>
public sealed record ResultadoConcessaoSim(Guid TituloId, string NumeroSim);

/// <summary>
/// Concede o registro de um título em análise no S.I.M.: atribui o NÚMERO DO S.I.M. (sequencial do
/// tenant, formatado por exercício) e fixa a vigência. // TODO(validar-oficial): máscara oficial do
/// número do S.I.M. conforme regulamento municipal de Maximiliano de Almeida.
/// </summary>
/// <param name="TituloId">Título a conceder.</param>
/// <param name="DataRegistro">Data da concessão.</param>
/// <param name="FimVigencia">Fim da vigência do título.</param>
public sealed record ConcederTituloSimCommand(
    Guid TituloId,
    DateOnly DataRegistro,
    DateOnly FimVigencia) : ICommand<ResultadoConcessaoSim>;

/// <summary>Regras de validação da concessão de título do S.I.M.</summary>
public sealed class ConcederTituloSimValidator : AbstractValidator<ConcederTituloSimCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcederTituloSimValidator()
    {
        RuleFor(c => c.TituloId).NotEmpty();
        RuleFor(c => c.FimVigencia).GreaterThan(c => c.DataRegistro);
    }
}

/// <summary>Handler da concessão de título do S.I.M.</summary>
public sealed class ConcederTituloSimHandler(
    ITituloRegistroSimRepository titulos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConcederTituloSimCommand, ResultadoConcessaoSim>
{
    /// <inheritdoc />
    public async Task<ResultadoConcessaoSim> Handle(ConcederTituloSimCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var titulo = await titulos.ObterPorIdAsync(new TituloRegistroSimId(request.TituloId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Título do S.I.M. não encontrado.");

        var sequencial = await titulos.ObterProximoSequencialAsync(request.DataRegistro.Year, cancellationToken).ConfigureAwait(false);
        var numeroSim = $"SIM-{request.DataRegistro.Year:0000}-{sequencial:000000}";

        titulo.Conceder(numeroSim, request.DataRegistro, request.FimVigencia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ResultadoConcessaoSim(titulo.Id.Value, numeroSim);
    }
}

/// <summary>Habilita um novo produto inspecionado num título do S.I.M. já existente.</summary>
/// <param name="TituloId">Título.</param>
/// <param name="Produto">Produto a habilitar.</param>
public sealed record HabilitarProdutoSimCommand(Guid TituloId, ProdutoSimInput Produto) : ICommand;

/// <summary>Regras de validação da habilitação de produto no S.I.M.</summary>
public sealed class HabilitarProdutoSimValidator : AbstractValidator<HabilitarProdutoSimCommand>
{
    /// <summary>Define as regras.</summary>
    public HabilitarProdutoSimValidator()
    {
        RuleFor(c => c.TituloId).NotEmpty();
        RuleFor(c => c.Produto).NotNull();
        RuleFor(c => c.Produto.Denominacao).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Produto.Classificacao).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Produto.NumeroRotulo).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da habilitação de produto no S.I.M.</summary>
public sealed class HabilitarProdutoSimHandler(
    ITituloRegistroSimRepository titulos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<HabilitarProdutoSimCommand>
{
    /// <inheritdoc />
    public async Task Handle(HabilitarProdutoSimCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var titulo = await titulos.ObterPorIdAsync(new TituloRegistroSimId(request.TituloId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Título do S.I.M. não encontrado.");

        titulo.HabilitarProduto(request.Produto.Denominacao, request.Produto.Classificacao, request.Produto.NumeroRotulo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Ação de mudança de situação de um título do S.I.M.</summary>
public enum AcaoTituloSim
{
    /// <summary>Suspende o título (irregularidade sanitária).</summary>
    Suspender = 1,

    /// <summary>Reativa um título suspenso.</summary>
    Reativar = 2,

    /// <summary>Cassa/cancela o título.</summary>
    Cassar = 3,
}

/// <summary>Altera a situação de um título do S.I.M. (suspender/reativar/cassar).</summary>
/// <param name="TituloId">Título.</param>
/// <param name="Acao">Ação a aplicar.</param>
/// <param name="Motivo">Motivo (obrigatório para suspender/cassar).</param>
public sealed record AlterarSituacaoTituloSimCommand(Guid TituloId, AcaoTituloSim Acao, string? Motivo) : ICommand;

/// <summary>Regras de validação da alteração de situação do título do S.I.M.</summary>
public sealed class AlterarSituacaoTituloSimValidator : AbstractValidator<AlterarSituacaoTituloSimCommand>
{
    /// <summary>Define as regras.</summary>
    public AlterarSituacaoTituloSimValidator()
    {
        RuleFor(c => c.TituloId).NotEmpty();
        RuleFor(c => c.Acao).IsInEnum();
        RuleFor(c => c.Motivo)
            .NotEmpty()
            .MaximumLength(300)
            .When(c => c.Acao is AcaoTituloSim.Suspender or AcaoTituloSim.Cassar);
    }
}

/// <summary>Handler da alteração de situação do título do S.I.M.</summary>
public sealed class AlterarSituacaoTituloSimHandler(
    ITituloRegistroSimRepository titulos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarSituacaoTituloSimCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarSituacaoTituloSimCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var titulo = await titulos.ObterPorIdAsync(new TituloRegistroSimId(request.TituloId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Título do S.I.M. não encontrado.");

        switch (request.Acao)
        {
            case AcaoTituloSim.Suspender:
                titulo.Suspender(request.Motivo!);
                break;
            case AcaoTituloSim.Reativar:
                titulo.Reativar();
                break;
            case AcaoTituloSim.Cassar:
                titulo.Cassar(request.Motivo!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Acao, "Ação de título do S.I.M. inválida.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
