using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>
/// Cadastra um estabelecimento sujeito a VISA (restaurante, farmacia, clinica, salao). Garante a unicidade
/// por <c>(TenantId, Documento, RazaoSocial)</c>. O documento aceita CNPJ (PJ) ou CPF (PF) — inferido pelo
/// comprimento e validado pelos VOs do SharedKernel.
/// </summary>
/// <param name="Documento">CNPJ ou CPF do responsavel (com ou sem mascara).</param>
/// <param name="RazaoSocial">Razao social/nome do estabelecimento.</param>
/// <param name="Ramo">Ramo de atividade.</param>
/// <param name="Risco">Grau de risco sanitario.</param>
/// <param name="Endereco">Endereco do estabelecimento.</param>
public sealed record CadastrarEstabelecimentoFiscalizavelCommand(
    string Documento,
    string RazaoSocial,
    RamoVisa Ramo,
    GrauRiscoSanitario Risco,
    EnderecoVisaDto Endereco) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de estabelecimento fiscalizavel.</summary>
public sealed class CadastrarEstabelecimentoFiscalizavelValidator : AbstractValidator<CadastrarEstabelecimentoFiscalizavelCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarEstabelecimentoFiscalizavelValidator()
    {
        RuleFor(c => c.Documento).NotEmpty();
        RuleFor(c => c.RazaoSocial)
            .NotEmpty()
            .MaximumLength(EstabelecimentoFiscalizavel.ComprimentoNome);
        RuleFor(c => c.Ramo).IsInEnum();
        RuleFor(c => c.Risco).IsInEnum();
        RuleFor(c => c.Endereco).NotNull();
        RuleFor(c => c.Endereco.Uf)
            .Length(EnderecoVisa.ComprimentoUf)
            .When(c => c.Endereco is not null);
    }
}

/// <summary>Handler do cadastro de estabelecimento fiscalizavel.</summary>
public sealed class CadastrarEstabelecimentoFiscalizavelHandler(
    IEstabelecimentoFiscalizavelRepository estabelecimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarEstabelecimentoFiscalizavelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarEstabelecimentoFiscalizavelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Endereco);

        var documento = DocumentoResponsavel.Criar(request.Documento);

        if (await estabelecimentos.ExisteAsync(documento.ParaPersistencia(), request.RazaoSocial.Trim(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Estabelecimento ja cadastrado para este documento/razao social.");
        }

        var endereco = EnderecoVisa.Criar(
            request.Endereco.Logradouro,
            request.Endereco.Bairro,
            request.Endereco.Municipio,
            request.Endereco.Uf,
            request.Endereco.Cep);

        var estabelecimento = EstabelecimentoFiscalizavel.Cadastrar(
            tenant.TenantId, documento, request.RazaoSocial, request.Ramo, request.Risco, endereco);

        estabelecimentos.Adicionar(estabelecimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return estabelecimento.Id.Value;
    }
}
