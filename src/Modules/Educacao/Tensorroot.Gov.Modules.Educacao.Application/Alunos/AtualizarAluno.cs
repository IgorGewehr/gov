using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Atualiza os dados civis e o endereco de um aluno ativo.</summary>
/// <param name="AlunoId">Identificador do aluno.</param>
/// <param name="DadosCivis">Novos dados civis.</param>
/// <param name="Endereco">Novo endereco residencial.</param>
public sealed record AtualizarAlunoCommand(
    Guid AlunoId,
    DadosCivisPayload DadosCivis,
    EnderecoAlunoPayload Endereco) : ICommand;

/// <summary>Regras de validacao da atualizacao de aluno.</summary>
public sealed class AtualizarAlunoValidator : AbstractValidator<AtualizarAlunoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarAlunoValidator()
    {
        RuleFor(comando => comando.AlunoId).NotEmpty();
        RuleFor(comando => comando.DadosCivis).NotNull();
        RuleFor(comando => comando.DadosCivis.Nome).NotEmpty().MaximumLength(DadosCivis.ComprimentoNome);
        RuleFor(comando => comando.DadosCivis.NomeMae).NotEmpty().MaximumLength(DadosCivis.ComprimentoNome);
        RuleFor(comando => comando.Endereco).NotNull();
        RuleFor(comando => comando.Endereco.Logradouro).NotEmpty();
        RuleFor(comando => comando.Endereco.Uf).NotEmpty().Length(EnderecoAluno.ComprimentoUf);
    }
}

/// <summary>Handler da atualizacao de aluno.</summary>
public sealed class AtualizarAlunoHandler(
    IAlunoRepository alunos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AtualizarAlunoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aluno = await alunos.ObterPorIdAsync(new AlunoId(request.AlunoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dadosCivis = AlunoMapeamento.CriarDadosCivis(request.DadosCivis, hoje);
        var endereco = AlunoMapeamento.CriarEndereco(request.Endereco);

        aluno.AtualizarDados(dadosCivis, endereco);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
