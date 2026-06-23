using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>
/// Registra um afastamento TIPADO de um servidor em atividade plena: cria o agregado
/// <see cref="Afastamento"/> com o efeito na folha derivado da <see cref="RegraAfastamento"/> vigente do
/// tipo (parametrizada por tenant) e espelha <c>Servidor.Situacao=Afastado</c> na mesma transacao.
/// Dispara o evento eSocial do tipo (S-2230 e congeneres).
/// </summary>
/// <param name="ServidorId">Servidor a afastar.</param>
/// <param name="Tipo">Tipo legal do afastamento (define o efeito na folha — o usuario NAO digita o efeito).</param>
/// <param name="Inicio">Inicio do afastamento.</param>
/// <param name="FimPrevisto">Fim previsto (nulo quando indeterminado, ex.: auxilio-doenca).</param>
/// <param name="Documento">Referencia do documento (atestado/portaria/laudo).</param>
public sealed record RegistrarAfastamentoCommand(
    Guid ServidorId,
    TipoAfastamento Tipo,
    DateOnly Inicio,
    DateOnly? FimPrevisto,
    string? Documento) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de afastamento.</summary>
public sealed class RegistrarAfastamentoValidator : AbstractValidator<RegistrarAfastamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAfastamentoValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.Tipo).IsInEnum().WithMessage("Tipo de afastamento invalido.");
        RuleFor(c => c.FimPrevisto)
            .GreaterThanOrEqualTo(c => c.Inicio)
            .When(c => c.FimPrevisto.HasValue)
            .WithMessage("Fim previsto nao pode ser anterior ao inicio.");
    }
}

/// <summary>Handler do registro de afastamento tipado.</summary>
public sealed class RegistrarAfastamentoHandler(
    IServidorRepository servidores,
    IAfastamentoRepository afastamentos,
    IRegraAfastamentoProvider regras,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAfastamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarAfastamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = new ServidorId(request.ServidorId);
        var servidor = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        // Um afastamento vigente por servidor: bloqueia abertura concorrente (encerre o anterior antes).
        if (await afastamentos.ObterVigenteDoServidorAsync(servidorId, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new InvalidOperationException("Servidor ja possui afastamento vigente; encerre-o antes de abrir outro.");
        }

        // I-7: espelha a situacao no Servidor (valida atividade plena: EmExercicio/Estavel).
        servidor.MarcarAfastado();

        // A regra vigente do tipo (cadastro do tenant ou default legal parametrizado) fornece o EFEITO:
        // o usuario escolhe o TIPO; o percentual/suspensao/contagem-de-tempo vem da regra (CLAUDE.md S7).
        var competencia = Competencia.De(request.Inicio.Year, request.Inicio.Month);
        var regra = await regras.ObterVigenteAsync(request.Tipo, competencia, cancellationToken).ConfigureAwait(false);

        var afastamento = Afastamento.Abrir(
            servidor.TenantId,
            servidorId,
            request.Tipo,
            request.Inicio,
            request.FimPrevisto,
            request.Documento,
            regra);
        afastamentos.Adicionar(afastamento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return afastamento.Id.Value;
    }
}
