using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ProcessosTrabalhistas;

/// <summary>
/// Cadastra um processo trabalhista em que o ente figura como reclamado. Define o valor provisionado a
/// partir do prognostico (NBC TG 25: provavel provisiona; possivel/remoto nao). Numero unico por tenant.
/// </summary>
/// <param name="NumeroProcesso">Numero do processo (CNJ).</param>
/// <param name="Vara">Vara/orgao julgador.</param>
/// <param name="Reclamante">Nome do reclamante.</param>
/// <param name="ServidorId">Servidor (ex-servidor) vinculado (opcional).</param>
/// <param name="Objeto">Objeto/pedidos.</param>
/// <param name="ValorCausa">Valor da causa.</param>
/// <param name="DataAjuizamento">Data de ajuizamento.</param>
/// <param name="Prognostico">Prognostico de perda inicial.</param>
public sealed record CadastrarProcessoTrabalhistaCommand(
    string NumeroProcesso,
    string Vara,
    string Reclamante,
    Guid? ServidorId,
    string Objeto,
    decimal ValorCausa,
    DateOnly DataAjuizamento,
    PrognosticoPerda Prognostico) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de processo trabalhista.</summary>
public sealed class CadastrarProcessoTrabalhistaValidator : AbstractValidator<CadastrarProcessoTrabalhistaCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarProcessoTrabalhistaValidator()
    {
        RuleFor(comando => comando.NumeroProcesso)
            .NotEmpty()
            .MaximumLength(ProcessoTrabalhista.ComprimentoMaximoNumero);
        RuleFor(comando => comando.Vara).NotEmpty();
        RuleFor(comando => comando.Reclamante).NotEmpty();
        RuleFor(comando => comando.Objeto)
            .NotEmpty()
            .MaximumLength(ProcessoTrabalhista.ComprimentoMaximoObjeto);
        RuleFor(comando => comando.ValorCausa).GreaterThanOrEqualTo(0m);
        RuleFor(comando => comando.Prognostico).IsInEnum();
    }
}

/// <summary>Handler do cadastro de processo trabalhista.</summary>
public sealed class CadastrarProcessoTrabalhistaHandler(
    IProcessoTrabalhistaRepository processos,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarProcessoTrabalhistaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarProcessoTrabalhistaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await processos.ExisteNumeroAsync(request.NumeroProcesso.Trim(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe processo trabalhista com este numero no tenant.");
        }

        ServidorId? servidorId = null;
        if (request.ServidorId is { } sid)
        {
            var servidor = await servidores.ObterPorIdAsync(new ServidorId(sid), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Servidor vinculado ao processo nao encontrado.");
            servidorId = servidor.Id;
        }

        var processo = ProcessoTrabalhista.Cadastrar(
            tenant.TenantId,
            request.NumeroProcesso,
            request.Vara,
            request.Reclamante,
            servidorId,
            request.Objeto,
            request.ValorCausa,
            request.DataAjuizamento,
            request.Prognostico);

        processos.Adicionar(processo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return processo.Id.Value;
    }
}
