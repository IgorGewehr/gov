using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>
/// Registra um ASO (Atestado de Saude Ocupacional) de um servidor — base do S-2220 e do PCMSO. O servidor
/// deve existir e estar ativo (nao desligado) para registrar exame ocupacional.
/// </summary>
/// <param name="ServidorId">Servidor examinado.</param>
/// <param name="Tipo">Tipo do exame ocupacional.</param>
/// <param name="DataExame">Data de realizacao.</param>
/// <param name="Resultado">Resultado/aptidao (apto/inapto).</param>
/// <param name="MedicoNome">Nome do medico responsavel.</param>
/// <param name="MedicoNrCrm">Numero do CRM.</param>
/// <param name="MedicoUfCrm">UF do CRM.</param>
/// <param name="DataProximoExame">Data prevista do proximo exame (opcional).</param>
/// <param name="Observacao">Observacao/restricoes (opcional).</param>
/// <param name="ExamesComplementares">Codigos de exames complementares (Tabela 27; opcional).</param>
public sealed record RegistrarExameOcupacionalCommand(
    Guid ServidorId,
    TipoExameOcupacional Tipo,
    DateOnly DataExame,
    ResultadoAso Resultado,
    string MedicoNome,
    string MedicoNrCrm,
    string MedicoUfCrm,
    DateOnly? DataProximoExame = null,
    string? Observacao = null,
    IReadOnlyList<string>? ExamesComplementares = null) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de ASO.</summary>
public sealed class RegistrarExameOcupacionalValidator : AbstractValidator<RegistrarExameOcupacionalCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarExameOcupacionalValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.Tipo).IsInEnum().WithMessage("Tipo de exame invalido.");
        RuleFor(c => c.Resultado).IsInEnum().WithMessage("Resultado do ASO invalido.");
        RuleFor(c => c.DataExame).NotEmpty().WithMessage("Data do exame e obrigatoria.");
        RuleFor(c => c.MedicoNome).NotEmpty().WithMessage("Nome do medico e obrigatorio.");
        RuleFor(c => c.MedicoNrCrm).NotEmpty().WithMessage("CRM do medico e obrigatorio.");
        RuleFor(c => c.MedicoUfCrm).NotEmpty().Length(2).WithMessage("UF do CRM deve ter 2 letras.");
    }
}

/// <summary>Handler do registro de ASO.</summary>
public sealed class RegistrarExameOcupacionalHandler(
    IServidorRepository servidores,
    IExameOcupacionalRepository exames,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RegistrarExameOcupacionalCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarExameOcupacionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        if (servidor.Situacao == SituacaoServidor.Desligado)
        {
            throw new InvalidOperationException("Servidor desligado nao admite registro de exame ocupacional (salvo demissional pela rescisao).");
        }

        var medico = MedicoResponsavel.Criar(request.MedicoNome, request.MedicoNrCrm, request.MedicoUfCrm);
        var exame = ExameOcupacional.Registrar(
            tenant.TenantId,
            servidor.Id,
            request.Tipo,
            request.DataExame,
            request.Resultado,
            medico,
            request.DataProximoExame,
            request.Observacao);

        if (request.ExamesComplementares is { } complementares)
        {
            foreach (var codigo in complementares)
            {
                exame.AdicionarExameComplementar(codigo);
            }
        }

        exames.Adicionar(exame);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return exame.Id.Value;
    }
}
