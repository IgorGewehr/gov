using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Cadastra um novo aluno na rede de ensino, retornando seu identificador.</summary>
/// <param name="DadosCivis">Dados civis (nome da mae obrigatorio).</param>
/// <param name="Endereco">Endereco residencial.</param>
/// <param name="Cpf">CPF do aluno (opcional).</param>
/// <param name="Responsaveis">Responsaveis (obrigatorio &gt;= 1 quando menor de idade — I-A2).</param>
public sealed record CadastrarAlunoCommand(
    DadosCivisPayload DadosCivis,
    EnderecoAlunoPayload Endereco,
    string? Cpf,
    IReadOnlyList<ResponsavelPayload> Responsaveis) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de aluno.</summary>
public sealed class CadastrarAlunoValidator : AbstractValidator<CadastrarAlunoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarAlunoValidator()
    {
        RuleFor(comando => comando.DadosCivis).NotNull();
        RuleFor(comando => comando.DadosCivis.Nome).NotEmpty().MaximumLength(DadosCivis.ComprimentoNome)
            .WithMessage("Nome do aluno obrigatorio.");
        RuleFor(comando => comando.DadosCivis.NomeMae).NotEmpty().MaximumLength(DadosCivis.ComprimentoNome)
            .WithMessage("Nome da mae obrigatorio (EducaCenso).");
        RuleFor(comando => comando.DadosCivis.Sexo).IsInEnum();
        RuleFor(comando => comando.Endereco).NotNull();
        RuleFor(comando => comando.Endereco.Logradouro).NotEmpty();
        RuleFor(comando => comando.Endereco.Uf).NotEmpty().Length(EnderecoAluno.ComprimentoUf);
        RuleForEach(comando => comando.Responsaveis).ChildRules(responsavel =>
        {
            responsavel.RuleFor(item => item.Nome).NotEmpty().WithMessage("Nome do responsavel obrigatorio.");
            responsavel.RuleFor(item => item.Parentesco).IsInEnum();
        });
    }
}

/// <summary>Handler do cadastro de aluno.</summary>
public sealed class CadastrarAlunoHandler(
    IAlunoRepository alunos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<CadastrarAlunoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var cpf = AlunoMapeamento.CriarCpfOuNulo(request.Cpf);

        // I-A4: unicidade por CPF quando informado.
        if (cpf is not null && await alunos.ExisteCpfAsync(cpf.Digitos, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("CPF ja cadastrado para outro aluno.");
        }

        var dadosCivis = AlunoMapeamento.CriarDadosCivis(request.DadosCivis, hoje);
        var endereco = AlunoMapeamento.CriarEndereco(request.Endereco);
        var responsaveis = (request.Responsaveis ?? [])
            .Select(AlunoMapeamento.CriarResponsavel)
            .ToList();

        var aluno = Aluno.Cadastrar(tenant.TenantId, dadosCivis, endereco, cpf, responsaveis, hoje);

        alunos.Adicionar(aluno);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AlunoCadastradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            aluno.Id.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return aluno.Id.Value;
    }
}
