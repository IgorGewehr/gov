using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Certidoes;

/// <summary>Certidão de regularidade fiscal emitida (read model) — CND/CPEN/Positiva.</summary>
/// <param name="CertidaoId">Identificador da certidão.</param>
/// <param name="Numero">Número da certidão.</param>
/// <param name="Tipo">Tipo apurado ("Negativa", "PositivaComEfeitoNegativa", "Positiva").</param>
/// <param name="AtestaRegularidade">Se a certidão produz efeito de regularidade (CND/CPEN).</param>
/// <param name="Documento">Documento do contribuinte (CPF/CNPJ).</param>
/// <param name="NomeContribuinte">Nome/razão social.</param>
/// <param name="DataEmissao">Data de emissão.</param>
/// <param name="DataValidade">Data-limite de validade.</param>
/// <param name="CodigoAutenticacao">Código de autenticação para conferência pública.</param>
/// <param name="FundamentoLegal">Fundamento legal.</param>
/// <param name="Observacao">Observação (ex.: débitos suspensos na CPEN), se houver.</param>
public sealed record CertidaoRegularidadeDto(
    Guid CertidaoId,
    string Numero,
    string Tipo,
    bool AtestaRegularidade,
    string Documento,
    string NomeContribuinte,
    DateOnly DataEmissao,
    DateOnly DataValidade,
    string CodigoAutenticacao,
    string FundamentoLegal,
    string? Observacao);

/// <summary>
/// Emite a Certidão de regularidade fiscal de um contribuinte (CND/CPEN — CTN arts. 205/206). Apura a
/// situação fiscal na data-base (débitos vencidos em aberto + dívida ativa, separando exigíveis de
/// suspensos) e DECIDE o tipo: Negativa (nenhum débito), Positiva-com-efeito-Negativa (só débitos
/// suspensos) ou Positiva (há débito exigível). A validade e o fundamento são parametrizáveis (lei
/// municipal). A data-base é o "hoje" do tenant (sem relógio no domínio — CLAUDE.md §16).
/// </summary>
/// <param name="ContribuinteId">Contribuinte a certificar.</param>
/// <param name="FundamentoLegal">Fundamento legal (CTN + CTM) — parametrizável.</param>
/// <param name="DiasValidade">Prazo de validade em dias (parametrizável); padrão do domínio se nulo.</param>
public sealed record EmitirCertidaoRegularidadeCommand(
    Guid ContribuinteId,
    string FundamentoLegal,
    int? DiasValidade = null) : ICommand<CertidaoRegularidadeDto>;

/// <summary>Regras de validação da emissão da certidão de regularidade.</summary>
public sealed class EmitirCertidaoRegularidadeValidator : AbstractValidator<EmitirCertidaoRegularidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirCertidaoRegularidadeValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        When(c => c.DiasValidade is not null, () =>
            RuleFor(c => c.DiasValidade!.Value).GreaterThanOrEqualTo(1).LessThanOrEqualTo(180));
    }
}

/// <summary>Handler da emissão da certidão de regularidade fiscal.</summary>
public sealed class EmitirCertidaoRegularidadeHandler(
    IContribuinteRepository contribuintes,
    ISituacaoFiscalConsulta situacaoFiscal,
    ICertidaoRegularidadeFiscalRepository certidoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EmitirCertidaoRegularidadeCommand, CertidaoRegularidadeDto>
{
    /// <inheritdoc />
    public async Task<CertidaoRegularidadeDto> Handle(EmitirCertidaoRegularidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var contribuinte = await contribuintes.ObterPorIdAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte não encontrado.");

        var hoje = dataHoje.Hoje();
        var situacao = await situacaoFiscal.ApurarAsync(contribuinteId, hoje, cancellationToken).ConfigureAwait(false);
        var tipo = CertidaoRegularidadeFiscal.DecidirTipo(
            situacao.LancamentosVencidosEmAberto,
            situacao.DividasAtivasExigiveis,
            situacao.DividasAtivasSuspensas);
        var observacao = situacao.DividasAtivasSuspensas > 0 && tipo == TipoCertidaoRegularidade.PositivaComEfeitoNegativa
            ? $"Existem {situacao.DividasAtivasSuspensas} inscrição(ões) em dívida ativa com exigibilidade suspensa (parcelamento)."
            : null;

        var sequencial = await certidoes.ObterProximoSequencialAsync(hoje.Year, cancellationToken).ConfigureAwait(false);

        var certidao = CertidaoRegularidadeFiscal.Emitir(
            tenant.TenantId,
            contribuinteId,
            contribuinte.Documento,
            contribuinte.Nome,
            contribuinte.InscricaoMunicipal,
            tipo,
            hoje,
            sequencial,
            request.FundamentoLegal,
            request.DiasValidade ?? CertidaoRegularidadeFiscal.DiasValidadePadrao,
            observacao);

        certidoes.Adicionar(certidao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Mapear(certidao);
    }

    private static CertidaoRegularidadeDto Mapear(CertidaoRegularidadeFiscal certidao)
        => new(
            certidao.Id.Value,
            certidao.Numero,
            certidao.Tipo.ToString(),
            certidao.AtestaRegularidade,
            certidao.Documento,
            certidao.NomeContribuinte,
            certidao.DataEmissao,
            certidao.DataValidade,
            certidao.CodigoAutenticacao,
            certidao.FundamentoLegal,
            certidao.Observacao);
}
