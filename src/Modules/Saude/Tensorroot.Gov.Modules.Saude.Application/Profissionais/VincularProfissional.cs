using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>
/// Abre um vinculo CNES/CBO do profissional num estabelecimento. Valida que o estabelecimento existe
/// e esta ativo no acervo local (coerencia profissional-unidade).
/// </summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="Cbo">Ocupacao (CBO, 6 digitos).</param>
/// <param name="DataInicio">Data de inicio do vinculo.</param>
public sealed record VincularProfissionalCommand(
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    string Cbo,
    DateOnly DataInicio) : ICommand<Guid>;

/// <summary>Regras de validacao do vinculo de profissional.</summary>
public sealed class VincularProfissionalValidator : AbstractValidator<VincularProfissionalCommand>
{
    /// <summary>Define as regras.</summary>
    public VincularProfissionalValidator()
    {
        RuleFor(comando => comando.ProfissionalId).NotEmpty();
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty().WithMessage("Estabelecimento (CNES) e obrigatorio.");
        RuleFor(comando => comando.Cbo).Must(Cbo.EhValido).WithMessage("CBO e obrigatorio e deve ser valido (6 digitos).");
        RuleFor(comando => comando.DataInicio).NotEmpty().WithMessage("Data de inicio do vinculo e obrigatoria.");
    }
}

/// <summary>Handler do vinculo de profissional.</summary>
public sealed class VincularProfissionalHandler(
    IProfissionalCadastroRepository profissionais,
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<VincularProfissionalCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(VincularProfissionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profissional = await profissionais
            .ObterPorIdAsync(new ProfissionalId(request.ProfissionalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");

        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoId);

        // Coerencia: o estabelecimento do vinculo deve existir e estar ativo no acervo local.
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(estabelecimentoId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento (CNES) nao encontrado.");
        if (!estabelecimento.EstaAtivo)
        {
            throw new InvalidOperationException("Estabelecimento (CNES) inativo nao admite novos vinculos.");
        }

        profissional.Vincular(estabelecimentoId, new Cbo(request.Cbo), request.DataInicio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return profissional.Id.Value;
    }
}

/// <summary>Encerra o vinculo ativo do profissional num estabelecimento.</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do vinculo.</param>
/// <param name="DataFim">Data de encerramento.</param>
public sealed record EncerrarVinculoProfissionalCommand(
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    DateOnly DataFim) : ICommand;

/// <summary>Regras de validacao do encerramento de vinculo.</summary>
public sealed class EncerrarVinculoProfissionalValidator : AbstractValidator<EncerrarVinculoProfissionalCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarVinculoProfissionalValidator()
    {
        RuleFor(comando => comando.ProfissionalId).NotEmpty();
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty();
        RuleFor(comando => comando.DataFim).NotEmpty();
    }
}

/// <summary>Handler do encerramento de vinculo.</summary>
public sealed class EncerrarVinculoProfissionalHandler(
    IProfissionalCadastroRepository profissionais,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarVinculoProfissionalCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarVinculoProfissionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profissional = await profissionais
            .ObterPorIdAsync(new ProfissionalId(request.ProfissionalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");

        profissional.EncerrarVinculo(new EstabelecimentoId(request.EstabelecimentoId), request.DataFim);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
