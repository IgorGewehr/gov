using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>Resumo (metadado) de um atendimento, para projecao do conteudo sigiloso a usuario autorizado.</summary>
/// <param name="Servico">Servico socioassistencial do atendimento.</param>
/// <param name="DataAtendimento">Data do atendimento.</param>
public sealed record RegistroResumo(string Servico, DateOnly DataAtendimento);

/// <summary>
/// Projecao de detalhe do prontuario — conteudo sigiloso, <b>somente para usuarios autorizados e
/// auditados</b> (I-7). Nunca cruza tenants (I-9).
/// </summary>
/// <param name="Id">Identificador do prontuario.</param>
/// <param name="FamiliaId">Familia acompanhada.</param>
/// <param name="UnidadeAtendimentoId">CRAS/CREAS responsavel.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="Registros">Resumo dos atendimentos (metadados).</param>
/// <param name="PossuiViolacaoCriancaAdolescente">Indica presenca de violacao envolvendo crianca/adolescente (I-10).</param>
public sealed record ProntuarioDetalhe(
    Guid Id,
    Guid FamiliaId,
    Guid UnidadeAtendimentoId,
    string Situacao,
    DateOnly DataAbertura,
    IReadOnlyList<RegistroResumo> Registros,
    bool PossuiViolacaoCriancaAdolescente);

/// <summary>
/// Obtem o conteudo sigiloso do prontuario de uma familia. Leitura sigilosa: exige
/// <see cref="MotivoAcesso"/> e registra a trilha de acesso antes de projetar (I-7).
/// </summary>
/// <param name="FamiliaId">Familia cujo prontuario sera lido.</param>
/// <param name="UsuarioId">Usuario que realiza a leitura.</param>
/// <param name="MotivoAcesso">Justificativa obrigatoria do acesso.</param>
public sealed record ObterProntuarioDaFamiliaQuery(
    Guid FamiliaId,
    Guid UsuarioId,
    string MotivoAcesso) : IQuery<ProntuarioDetalhe>;

/// <summary>Regras de validacao da leitura sigilosa do prontuario da familia.</summary>
public sealed class ObterProntuarioDaFamiliaValidator : AbstractValidator<ObterProntuarioDaFamiliaQuery>
{
    /// <summary>Define as regras.</summary>
    public ObterProntuarioDaFamiliaValidator()
    {
        RuleFor(consulta => consulta.FamiliaId).NotEmpty().WithMessage("Familia e obrigatoria.");
        RuleFor(consulta => consulta.UsuarioId).NotEmpty().WithMessage("Usuario do acesso e obrigatorio.");
        RuleFor(consulta => consulta.MotivoAcesso).NotEmpty().MaximumLength(400).WithMessage("Motivo de acesso ao prontuario e obrigatorio.");
    }
}

/// <summary>
/// Handler da leitura sigilosa do prontuario da familia. Antes de projetar o conteudo, registra
/// o acesso na trilha imutavel (I-7) e persiste essa anotacao; o conteudo nunca cruza tenants (I-9).
/// </summary>
public sealed class ObterProntuarioDaFamiliaHandler(
    IProntuarioSuasRepository prontuarios,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IQueryHandler<ObterProntuarioDaFamiliaQuery, ProntuarioDetalhe>
{
    /// <inheritdoc />
    public async Task<ProntuarioDetalhe> Handle(ObterProntuarioDaFamiliaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prontuario = await prontuarios.ObterPorFamiliaAsync(request.FamiliaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        // I-7: toda leitura de conteudo sigiloso registra a trilha de acesso (append-only) antes de projetar.
        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        prontuario.RegistrarAcesso(request.UsuarioId, request.MotivoAcesso, agoraUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var registros = prontuario.Registros
            .Select(registro => new RegistroResumo(registro.Servico.ToString(), registro.DataAtendimento))
            .ToList();

        return new ProntuarioDetalhe(
            prontuario.Id.Value,
            prontuario.FamiliaId,
            prontuario.UnidadeAtendimentoId,
            prontuario.Situacao.ToString(),
            prontuario.DataAbertura,
            registros,
            prontuario.Violacoes.Any(violacao => violacao.EnvolveCriancaAdolescente));
    }
}
