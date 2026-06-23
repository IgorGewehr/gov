using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using Tensorroot.Gov.SharedKernel;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>Inclui um paciente na fila de espera (sem vaga). Exige profissional OU especialidade (CBO).</summary>
/// <param name="PacienteId">Paciente.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="ProfissionalId">Profissional desejado (opcional).</param>
/// <param name="Especialidade">Especialidade/CBO desejada (opcional).</param>
/// <param name="Tipo">Natureza (consulta/exame).</param>
/// <param name="Prioridade">Prioridade.</param>
public sealed record EntrarNaFilaDeEsperaCommand(
    Guid PacienteId,
    Guid EstabelecimentoId,
    Guid? ProfissionalId,
    string? Especialidade,
    TipoAtendimentoAgenda Tipo,
    PrioridadeAgendamento Prioridade) : ICommand<Guid>;

/// <summary>Convoca explicitamente uma entrada da fila (Aguardando → Convocado).</summary>
/// <param name="FilaEsperaId">Identificador da entrada.</param>
public sealed record ConvocarDaFilaDeEsperaCommand(Guid FilaEsperaId) : ICommand;

/// <summary>Remove uma entrada da fila de espera (desistencia/obsoleto).</summary>
/// <param name="FilaEsperaId">Identificador da entrada.</param>
/// <param name="Motivo">Motivo da remocao.</param>
public sealed record RemoverDaFilaDeEsperaCommand(Guid FilaEsperaId, string Motivo) : ICommand;

/// <summary>Regras de validacao da entrada na fila de espera.</summary>
public sealed class EntrarNaFilaDeEsperaValidator : AbstractValidator<EntrarNaFilaDeEsperaCommand>
{
    /// <summary>Define as regras.</summary>
    public EntrarNaFilaDeEsperaValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.EstabelecimentoId).NotEmpty().WithMessage("Estabelecimento e obrigatorio.");
        RuleFor(comando => comando.Tipo).IsInEnum().WithMessage("Tipo de atendimento invalido.");
        RuleFor(comando => comando.Prioridade).IsInEnum().WithMessage("Prioridade invalida.");
        RuleFor(comando => comando)
            .Must(comando => (comando.ProfissionalId is { } p && p != Guid.Empty) || !string.IsNullOrWhiteSpace(comando.Especialidade))
            .WithMessage("Informe profissional ou especialidade.");
    }
}

/// <summary>Handler da entrada na fila de espera.</summary>
public sealed class EntrarNaFilaDeEsperaHandler(
    IFilaEsperaRepository filas,
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EntrarNaFilaDeEsperaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EntrarNaFilaDeEsperaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pacienteId = new PacienteId(request.PacienteId);
        var paciente = await pacientes.ObterPorIdAsync(new Domain.Pacientes.PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");
        if (paciente.Situacao is not Domain.Pacientes.SituacaoPaciente.Ativo)
        {
            throw new InvalidOperationException("Paciente inativo: nao pode entrar na fila.");
        }

        var profissionalId = request.ProfissionalId is { } p && p != Guid.Empty ? new ProfissionalId(p) : (ProfissionalId?)null;

        var entrada = FilaEspera.Entrar(
            tenant.TenantId,
            pacienteId,
            new EstabelecimentoId(request.EstabelecimentoId),
            profissionalId,
            request.Especialidade,
            request.Tipo,
            request.Prioridade,
            timeProvider.GetUtcNow());

        filas.Adicionar(entrada);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entrada.Id.Value;
    }
}

/// <summary>Handler da convocacao explicita da fila de espera.</summary>
public sealed class ConvocarDaFilaDeEsperaHandler(
    IFilaEsperaRepository filas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ConvocarDaFilaDeEsperaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConvocarDaFilaDeEsperaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entrada = await filas.ObterPorIdAsync(new FilaEsperaId(request.FilaEsperaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Entrada de fila nao encontrada.");

        entrada.Convocar(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da remocao da fila de espera.</summary>
public sealed class RemoverDaFilaDeEsperaHandler(IFilaEsperaRepository filas, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoverDaFilaDeEsperaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RemoverDaFilaDeEsperaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entrada = await filas.ObterPorIdAsync(new FilaEsperaId(request.FilaEsperaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Entrada de fila nao encontrada.");

        entrada.Remover(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Lista paginada da fila de espera por estabelecimento/situacao (ordem de convocacao). Dado de saude
/// (LGPD art. 11): implementa <see cref="ISensivelLgpd"/> e gera trilha de acesso. Tenant-scoped; read-only.
/// </summary>
/// <param name="EstabelecimentoId">Filtro opcional por estabelecimento.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarFilaDeEsperaQuery(
    Guid? EstabelecimentoId,
    SituacaoFilaEspera? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<FilaEsperaItem>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(FilaEspera);

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da busca paginada da fila de espera.</summary>
public sealed class BuscarFilaDeEsperaHandler(IFilaEsperaRepository filas)
    : IQueryHandler<BuscarFilaDeEsperaQuery, ResultadoPaginado<FilaEsperaItem>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<FilaEsperaItem>> Handle(
        BuscarFilaDeEsperaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var estabelecimentoId = request.EstabelecimentoId is { } e && e != Guid.Empty ? new EstabelecimentoId(e) : (EstabelecimentoId?)null;

        var (itens, total) = await filas
            .BuscarAsync(estabelecimentoId, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(entrada => new FilaEsperaItem(
                entrada.Id.Value,
                entrada.PacienteId.Value,
                entrada.EstabelecimentoId.Value,
                entrada.ProfissionalId?.Value,
                entrada.Especialidade,
                entrada.Tipo.ToString(),
                entrada.Prioridade.ToString(),
                entrada.DataEntrada,
                entrada.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<FilaEsperaItem>(projetados, total, pagina, tamanho);
    }
}
