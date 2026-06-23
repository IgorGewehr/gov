using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>
/// Cadastra um estabelecimento de saude (CNES) no acervo local. Garante a unicidade por
/// <c>(TenantId, Cnes)</c>.
/// </summary>
/// <param name="Cnes">Codigo CNES (7 digitos).</param>
/// <param name="Nome">Nome do estabelecimento.</param>
/// <param name="Tipo">Tipo do estabelecimento.</param>
/// <param name="Endereco">Endereco do estabelecimento.</param>
public sealed record CadastrarEstabelecimentoCommand(
    string Cnes,
    string Nome,
    TipoEstabelecimento Tipo,
    EnderecoEstabelecimentoDto Endereco) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de estabelecimento.</summary>
public sealed class CadastrarEstabelecimentoValidator : AbstractValidator<CadastrarEstabelecimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarEstabelecimentoValidator()
    {
        RuleFor(comando => comando.Cnes)
            .NotEmpty()
            .Must(CodigoCnes.EhValido)
            .WithMessage("CNES e obrigatorio e deve ser valido (7 digitos).");

        RuleFor(comando => comando.Nome)
            .NotEmpty()
            .MaximumLength(Estabelecimento.ComprimentoNome)
            .WithMessage("Nome do estabelecimento e obrigatorio (max. 200).");

        RuleFor(comando => comando.Tipo).IsInEnum().WithMessage("Tipo de estabelecimento invalido.");

        RuleFor(comando => comando.Endereco).NotNull();
        RuleFor(comando => comando.Endereco.Uf)
            .Length(Endereco.ComprimentoUf)
            .WithMessage("UF deve ter 2 caracteres.")
            .When(comando => comando.Endereco is not null);
    }
}

/// <summary>Handler do cadastro de estabelecimento.</summary>
public sealed class CadastrarEstabelecimentoHandler(
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarEstabelecimentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Endereco);

        var cnes = new CodigoCnes(request.Cnes);

        if (await estabelecimentos.ExistePorCnesAsync(cnes, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Estabelecimento ja cadastrado para este CNES.");
        }

        var endereco = new Endereco(
            request.Endereco.Logradouro,
            request.Endereco.Numero,
            request.Endereco.Bairro,
            request.Endereco.Municipio,
            request.Endereco.Uf,
            request.Endereco.Cep);

        var estabelecimento = Estabelecimento.Cadastrar(tenant.TenantId, cnes, request.Nome, request.Tipo, endereco);

        estabelecimentos.Adicionar(estabelecimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return estabelecimento.Id.Value;
    }
}
