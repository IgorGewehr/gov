using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Lanca um evento (provento/desconto) de um servidor numa folha aberta (I-2/I-3/I-4).</summary>
/// <param name="FolhaDePagamentoId">Folha a receber o evento.</param>
/// <param name="ServidorId">Servidor do lancamento.</param>
/// <param name="Rubrica">Codigo da rubrica (S-1010).</param>
/// <param name="Tipo">Provento ou desconto.</param>
/// <param name="BaseCalculo">Valor de incidencia.</param>
/// <param name="Valor">Valor apurado da verba.</param>
public sealed record AdicionarEventoCommand(
    Guid FolhaDePagamentoId,
    Guid ServidorId,
    string Rubrica,
    TipoEvento Tipo,
    decimal BaseCalculo,
    decimal Valor) : ICommand;

/// <summary>Regras de validacao do lancamento de evento.</summary>
public sealed class AdicionarEventoValidator : AbstractValidator<AdicionarEventoCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarEventoValidator()
    {
        RuleFor(comando => comando.FolhaDePagamentoId)
            .NotEmpty()
            .WithMessage("Folha e obrigatoria.");
        RuleFor(comando => comando.ServidorId)
            .NotEmpty()
            .WithMessage("Servidor e obrigatorio.");
        RuleFor(comando => comando.Rubrica)
            .NotEmpty()
            .MaximumLength(30)
            .WithMessage("Rubrica e obrigatoria (max. 30 caracteres).");
        RuleFor(comando => comando.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de evento invalido (Provento/Desconto).");
        RuleFor(comando => comando.BaseCalculo)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Base de calculo nao pode ser negativa.");
        RuleFor(comando => comando.Valor)
            .GreaterThan(0m)
            .WithMessage("Valor do evento deve ser maior que zero.");
    }
}

/// <summary>Handler do lancamento de evento de folha.</summary>
public sealed class AdicionarEventoHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRegimeConsulta servidores,
    IRubricaS1010Consulta rubricas,
    AjustadorProventoPorAfastamento ajustadorAfastamento,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarEventoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AdicionarEventoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        // I-4: regime do evento coerente com o do servidor.
        var regime = await servidores.ObterRegimeAsync(request.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        // I-3: rubrica deve existir e estar vigente em S-1010 na competencia.
        var rubrica = Rubrica.De(request.Rubrica);
        if (!await rubricas.EstaVigenteAsync(rubrica, folha.Competencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Rubrica inexistente ou nao vigente em S-1010 na competencia.");
        }

        // GANCHO DO AFASTAMENTO (design RH §3.3): o provento-base e ajustado pelo efeito dos afastamentos
        // vigentes do servidor na competencia (suspende/reduz/proporcionaliza) ANTES de lancar a verba —
        // a folha de um servidor afastado deixa de ser calculada como se ativo. So afeta PROVENTOS; o
        // MotorDeCalculoFolha continua puro (recebe a verba ja ajustada).
        var ehProvento = request.Tipo == TipoEvento.Provento;
        var valorAjustado = await ajustadorAfastamento
            .AjustarProventoAsync(request.ServidorId, folha.Competencia, ehProvento, request.Valor, cancellationToken)
            .ConfigureAwait(false);

        // Afastamento que suspende 100% do provento na competencia zera a verba: nao ha o que lancar
        // (o agregado exige valor > 0). Sem lancamento, o provento simplesmente nao entra na folha.
        if (valorAjustado <= 0m)
        {
            if (ehProvento)
            {
                return;
            }

            valorAjustado = request.Valor;
        }

        // I-2: o agregado garante que so aceita eventos quando Aberta.
        folha.AdicionarEvento(request.ServidorId, rubrica, request.Tipo, BaseCalculo.De(request.BaseCalculo), valorAjustado, regime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
