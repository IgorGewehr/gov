using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>Atualiza os dados cadastrais de um estabelecimento ativo (CNES e imutavel).</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
/// <param name="Nome">Novo nome.</param>
/// <param name="Tipo">Novo tipo.</param>
/// <param name="Endereco">Novo endereco.</param>
public sealed record AtualizarEstabelecimentoCommand(
    Guid EstabelecimentoId,
    string Nome,
    TipoEstabelecimento Tipo,
    EnderecoEstabelecimentoDto Endereco) : ICommand;

/// <summary>Regras de validacao da atualizacao de estabelecimento.</summary>
public sealed class AtualizarEstabelecimentoValidator : AbstractValidator<AtualizarEstabelecimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarEstabelecimentoValidator()
    {
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty();
        RuleFor(comando => comando.Nome)
            .NotEmpty()
            .MaximumLength(Estabelecimento.ComprimentoNome);
        RuleFor(comando => comando.Tipo).IsInEnum();
        RuleFor(comando => comando.Endereco).NotNull();
        RuleFor(comando => comando.Endereco.Uf)
            .Length(Endereco.ComprimentoUf)
            .When(comando => comando.Endereco is not null);
    }
}

/// <summary>Handler da atualizacao de estabelecimento.</summary>
public sealed class AtualizarEstabelecimentoHandler(
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AtualizarEstabelecimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Endereco);

        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");

        var endereco = new Endereco(
            request.Endereco.Logradouro,
            request.Endereco.Numero,
            request.Endereco.Bairro,
            request.Endereco.Municipio,
            request.Endereco.Uf,
            request.Endereco.Cep);

        estabelecimento.AtualizarDados(request.Nome, request.Tipo, endereco);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
