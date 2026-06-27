using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

/// <summary>
/// Emite uma Certidao de Tempo de Servico/Contribuicao (CTC) de um servidor. O EFETIVO EXERCICIO proprio e'
/// APURADO automaticamente do vinculo (inicio de exercicio -> data-base), abatendo os dias nao-computaveis
/// dos afastamentos com <c>ContaTempo=false</c>. Os periodos AVERBADOS de outros orgaos/regimes sao
/// informados pelo solicitante. A numeracao sequencial do exercicio e' apurada pelo handler (o cliente NAO
/// informa o numero); o codigo de autenticacao e' selado a partir do digest deterministico.
/// </summary>
/// <param name="ServidorId">Servidor a certificar.</param>
/// <param name="Finalidade">Finalidade da certidao (aposentadoria/disponibilidade/...).</param>
/// <param name="OrgaoEmissor">Orgao/setor emissor (ex.: Departamento de RH).</param>
/// <param name="DataBase">
/// Data-base da apuracao do efetivo exercicio (inclusiva). Quando nula, usa a data de desligamento do
/// servidor (se desligado) ou o "hoje" do tenant.
/// </param>
/// <param name="Averbados">Periodos averbados de outros orgaos/regimes (opcional).</param>
/// <param name="IncluirEfetivoExercicio">
/// Se <c>true</c> (padrao), apura e inclui o efetivo exercicio proprio. <c>false</c> emite certidao apenas
/// dos periodos averbados (ex.: re-emissao de tempo importado).
/// </param>
/// <param name="FinalidadeDescrita">Texto descritivo da finalidade (opcional).</param>
/// <param name="Observacao">Observacao geral (opcional).</param>
public sealed record EmitirCertidaoTempoServicoCommand(
    Guid ServidorId,
    FinalidadeCertidao Finalidade,
    string OrgaoEmissor,
    DateOnly? DataBase = null,
    IReadOnlyList<PeriodoAverbadoInput>? Averbados = null,
    bool IncluirEfetivoExercicio = true,
    string? FinalidadeDescrita = null,
    string? Observacao = null) : ICommand<Guid>;

/// <summary>Regras de validacao da emissao de certidao de tempo.</summary>
public sealed class EmitirCertidaoTempoServicoValidator : AbstractValidator<EmitirCertidaoTempoServicoCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirCertidaoTempoServicoValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(comando => comando.Finalidade).IsInEnum().WithMessage("Finalidade da certidao invalida.");
        RuleFor(comando => comando.OrgaoEmissor)
            .NotEmpty()
            .MaximumLength(CertidaoTempoServico.ComprimentoMaximoOrgaoEmissor)
            .WithMessage($"Orgao emissor e obrigatorio (max. {CertidaoTempoServico.ComprimentoMaximoOrgaoEmissor} caracteres).");

        RuleForEach(comando => comando.Averbados).ChildRules(periodo =>
        {
            periodo.RuleFor(p => p.Fim).GreaterThanOrEqualTo(p => p.Inicio).WithMessage("Fim do periodo averbado nao pode ser anterior ao inicio.");
            periodo.RuleFor(p => p.RegimeOrigem).IsInEnum().WithMessage("Regime de origem invalido.");
            periodo.RuleFor(p => p.Origem).NotEmpty().WithMessage("Origem do periodo averbado e obrigatoria.");
            periodo.RuleFor(p => p.Fator).GreaterThanOrEqualTo(PeriodoTempo.FatorComum).WithMessage("Fator de conversao nao pode ser inferior a 1,0.");
            periodo.RuleFor(p => p.DiasNaoComputaveis).GreaterThanOrEqualTo(0).WithMessage("Dias nao-computaveis nao podem ser negativos.");
        });
    }
}

/// <summary>Handler da emissao de certidao de tempo de servico/contribuicao.</summary>
public sealed class EmitirCertidaoTempoServicoHandler(
    IServidorRepository servidores,
    IAfastamentoRepository afastamentos,
    ICertidaoTempoServicoRepository certidoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EmitirCertidaoTempoServicoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmitirCertidaoTempoServicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var periodos = new List<PeriodoTempo>();

        if (request.IncluirEfetivoExercicio)
        {
            periodos.Add(await ApurarEfetivoExercicioAsync(servidor, request.DataBase, cancellationToken).ConfigureAwait(false));
        }

        if (request.Averbados is { } averbados)
        {
            foreach (var item in averbados)
            {
                periodos.Add(PeriodoTempo.Averbado(
                    item.Inicio,
                    item.Fim,
                    item.RegimeOrigem,
                    item.Origem,
                    item.DiasNaoComputaveis,
                    item.Fator,
                    item.Observacao));
            }
        }

        // Numeracao sequencial do exercicio da data de emissao (data civil do tenant).
        var hoje = dataHoje.Hoje();
        var numero = NumeroCertidao.De(hoje.Year, await certidoes.ProximoSequencialAsync(hoje.Year, cancellationToken).ConfigureAwait(false));

        var certidao = CertidaoTempoServico.Emitir(
            tenant.TenantId,
            servidor.Id,
            numero,
            request.Finalidade,
            hoje,
            request.OrgaoEmissor,
            periodos,
            request.FinalidadeDescrita,
            request.Observacao);

        // Sela o codigo de autenticacao (digest deterministico calculado na borda).
        var digest = ProjetarCertidao.CalcularDigestAutenticacao(
            tenant.TenantId, certidao.Id, certidao.ServidorId, numero, certidao.Finalidade, certidao.TotalDias);
        certidao.DefinirAutenticacao(CodigoAutenticacao.De(digest));

        certidoes.Adicionar(certidao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return certidao.Id.Value;
    }

    private async Task<PeriodoTempo> ApurarEfetivoExercicioAsync(
        Servidor servidor,
        DateOnly? dataBaseInformada,
        CancellationToken cancellationToken)
    {
        if (servidor.DataExercicio is not { } inicioExercicio)
        {
            throw new InvalidOperationException("Servidor sem data de inicio de exercicio nao tem efetivo exercicio a certificar.");
        }

        // Data-base: informada > data de desligamento (se desligado) > hoje do tenant.
        var dataBase = dataBaseInformada
            ?? servidor.DataDesligamento
            ?? dataHoje.Hoje();

        // Periodos nao-computaveis = afastamentos NAO-cancelados com ContaTempo=false (licenca sem contagem).
        var todos = await afastamentos.ListarPorServidorAsync(servidor.Id.Value, cancellationToken).ConfigureAwait(false);
        var naoComputaveis = todos
            .Where(afastamento => afastamento.Situacao != SituacaoAfastamento.Cancelado && !afastamento.ContaTempo)
            .Select(afastamento => new IntervaloNaoComputavel(
                afastamento.Inicio,
                afastamento.FimEfetivo ?? afastamento.FimPrevisto ?? dataBase))
            .ToList();

        return ApuradorTempoServidor.ApurarEfetivoExercicio(
            inicioExercicio,
            dataBase,
            naoComputaveis,
            observacao: "Tempo de efetivo exercicio no ente, apurado do vinculo.");
    }
}
