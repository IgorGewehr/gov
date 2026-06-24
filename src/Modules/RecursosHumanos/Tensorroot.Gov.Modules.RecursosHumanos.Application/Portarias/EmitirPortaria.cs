using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Portarias;

/// <summary>
/// Emite uma portaria / ato de pessoal. A numeracao SEQUENCIAL do exercicio e' apurada pelo handler
/// (proximo sequencial do exercicio no tenant) — o cliente NAO informa o numero. Vinculo opcional ao
/// servidor (atos individuais) com validacao de existencia.
/// </summary>
/// <param name="Tipo">Natureza do ato (nomeacao/exoneracao/designacao/concessao/outro).</param>
/// <param name="Ementa">Ementa/resumo do ato.</param>
/// <param name="Texto">Texto integral do ato.</param>
/// <param name="ServidorId">Servidor vinculado (opcional).</param>
/// <param name="DataAto">Data do ato; quando nula, usa o "hoje" do tenant.</param>
public sealed record EmitirPortariaCommand(
    TipoPortaria Tipo,
    string Ementa,
    string Texto,
    Guid? ServidorId,
    DateOnly? DataAto) : ICommand<Guid>;

/// <summary>Regras de validacao da emissao de portaria.</summary>
public sealed class EmitirPortariaValidator : AbstractValidator<EmitirPortariaCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirPortariaValidator()
    {
        RuleFor(comando => comando.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de portaria invalido.");

        RuleFor(comando => comando.Ementa)
            .NotEmpty()
            .MaximumLength(Portaria.ComprimentoMaximoEmenta)
            .WithMessage($"Ementa e obrigatoria (max. {Portaria.ComprimentoMaximoEmenta} caracteres).");

        RuleFor(comando => comando.Texto)
            .NotEmpty()
            .MaximumLength(Portaria.ComprimentoMaximoTexto)
            .WithMessage($"Texto da portaria e obrigatorio (max. {Portaria.ComprimentoMaximoTexto} caracteres).");
    }
}

/// <summary>Handler da emissao de portaria.</summary>
public sealed class EmitirPortariaHandler(
    IPortariaRepository portarias,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EmitirPortariaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmitirPortariaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dataAto = request.DataAto ?? dataHoje.Hoje();

        ServidorId? servidorId = null;
        if (request.ServidorId is { } sid)
        {
            var servidor = await servidores.ObterPorIdAsync(new ServidorId(sid), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Servidor vinculado a portaria nao encontrado.");
            servidorId = servidor.Id;
        }

        // Numeracao sequencial por exercicio/tenant: o exercicio e' o ano da DATA DO ATO (civil do tenant).
        var exercicio = dataAto.Year;
        var sequencial = await portarias.ProximoSequencialAsync(exercicio, cancellationToken).ConfigureAwait(false);
        var numero = NumeroPortaria.De(exercicio, sequencial);

        var portaria = Portaria.Emitir(
            tenant.TenantId,
            numero,
            request.Tipo,
            dataAto,
            request.Ementa,
            request.Texto,
            servidorId);

        portarias.Adicionar(portaria);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return portaria.Id.Value;
    }
}
