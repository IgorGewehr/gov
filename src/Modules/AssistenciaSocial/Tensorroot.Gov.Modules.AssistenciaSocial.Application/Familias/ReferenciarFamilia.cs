using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>
/// Referencia uma familia a uma Unidade de Atendimento (CRAS), validando NIS no CadUnico e a
/// cobertura territorial. Publica <see cref="FamiliaReferenciadaIntegrationEvent"/> (Outbox).
/// </summary>
/// <param name="Nis">NIS do responsavel familiar (chave do CadUnico).</param>
/// <param name="CpfResponsavel">CPF do responsavel familiar.</param>
/// <param name="UnidadeAtendimentoId">CRAS de referencia.</param>
/// <param name="Endereco">Endereco com territorio.</param>
/// <param name="Membros">Composicao familiar (rendas + parentescos).</param>
public sealed record ReferenciarFamiliaCommand(
    string Nis,
    string CpfResponsavel,
    Guid UnidadeAtendimentoId,
    EnderecoTerritorializadoDto Endereco,
    IReadOnlyList<MembroFamiliarDto> Membros) : ICommand<Guid>;

/// <summary>Regras de validacao do referenciamento de familia.</summary>
public sealed class ReferenciarFamiliaValidator : AbstractValidator<ReferenciarFamiliaCommand>
{
    /// <summary>Define as regras.</summary>
    public ReferenciarFamiliaValidator()
    {
        RuleFor(comando => comando.Nis)
            .NotEmpty()
            .Must(nis => Nis.IsValid(SomenteDigitos(nis)))
            .WithMessage("NIS e obrigatorio e deve ser valido.");

        RuleFor(comando => comando.UnidadeAtendimentoId)
            .NotEmpty()
            .WithMessage("Unidade de atendimento (CRAS) e obrigatoria.");

        RuleFor(comando => comando.Endereco)
            .NotNull();

        RuleFor(comando => comando.Endereco.Territorio)
            .NotEmpty()
            .WithMessage("Territorio do endereco e obrigatorio.")
            .When(comando => comando.Endereco is not null);

        RuleFor(comando => comando.Membros)
            .NotEmpty()
            .WithMessage("A familia deve ter ao menos um membro.");

        RuleForEach(comando => comando.Membros)
            .Must(membro => Cpf.TryCreate(membro.Cpf, out _))
            .WithMessage("CPF de membro invalido.");
    }

    private static string SomenteDigitos(string? valor)
        => string.IsNullOrEmpty(valor) ? string.Empty : new string([.. valor.Where(char.IsAsciiDigit)]);
}

/// <summary>Handler do referenciamento de familia.</summary>
public sealed class ReferenciarFamiliaHandler(
    IUnidadeAtendimentoRepository unidades,
    IFamiliaRepository familias,
    ICadUnicoGateway cadUnico,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ReferenciarFamiliaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ReferenciarFamiliaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nis = Nis.Create(request.Nis);
        var cpfResponsavel = Cpf.Create(request.CpfResponsavel);

        var unidade = await unidades.ObterPorIdAsync(request.UnidadeAtendimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade de atendimento (CRAS) nao encontrada.");

        // I-1/I-9: o NIS deve ser localizado/consultavel no CadUnico federal (read model autoritativo).
        var resultado = await cadUnico.ConsultarPorNisAsync(nis.Digitos, cancellationToken).ConfigureAwait(false);
        if (!resultado.NisLocalizado)
        {
            throw new InvalidOperationException("NIS invalido ou nao localizado no CadUnico.");
        }

        var endereco = EnderecoTerritorializado.Create(
            request.Endereco.Logradouro,
            request.Endereco.Municipio,
            request.Endereco.Cep,
            request.Endereco.Territorio);

        var membros = request.Membros
            .Select(membro => MembroFamiliar.Registrar(
                Cpf.Create(membro.Cpf),
                (Parentesco)membro.Parentesco,
                membro.DataNascimento,
                ValorMonetario.De(membro.RendaIndividual),
                membro.EhPcd))
            .ToList();

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // I-2: a cobertura territorial do CRAS e validada dentro da factory.
        var familia = Familia.Referenciar(
            tenant.TenantId,
            nis,
            cpfResponsavel,
            endereco,
            unidade.Id,
            unidade.TerritorioCobertura,
            membros,
            hoje);

        familias.Adicionar(familia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new FamiliaReferenciadaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            familia.Id.Value,
            nis.Mascarado,
            familia.UnidadeAtendimentoId,
            familia.Territorio,
            familia.DataReferenciamento);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return familia.Id.Value;
    }
}
