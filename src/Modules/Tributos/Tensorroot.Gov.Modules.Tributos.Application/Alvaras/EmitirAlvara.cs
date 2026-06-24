using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Alvaras;

/// <summary>Resultado da emissão de um alvará com a TLL correspondente.</summary>
/// <param name="AlvaraId">Alvará (ato de polícia) emitido.</param>
/// <param name="LancamentoTllId">Lançamento da Taxa de Licença (TLL) gerado.</param>
/// <param name="DamId">DAM (guia) da TLL gerado.</param>
/// <param name="ValorTll">Valor da TLL apurado (R$).</param>
public sealed record ResultadoEmissaoAlvara(Guid AlvaraId, Guid LancamentoTllId, Guid DamId, decimal ValorTll);

/// <summary>
/// Emite um alvará (ato administrativo de polícia) e lança a Taxa de Licença de Localização/Funcionamento
/// (TLL) correspondente: o ato e o tributo são distintos (o alvará não é o tributo). A TLL é calculada
/// pela <see cref="Domain.Taxas.TabelaTaxa"/> vigente de licença (código + exercício) sobre a
/// quantidade-base (ex.: área/atividade/risco), gera o <see cref="Lancamento"/> <c>TipoTributo.Taxa</c>
/// e a guia (DAM). Nenhum valor é hardcoded. Ver M6-DESIGN §3.3.
/// </summary>
/// <param name="ContribuinteId">Contribuinte titular do estabelecimento.</param>
/// <param name="ImovelId">Imóvel do estabelecimento (opcional).</param>
/// <param name="Especie">Espécie do alvará.</param>
/// <param name="NomeEstabelecimento">Nome/razão social.</param>
/// <param name="AtividadeCnae">Atividade (CNAE).</param>
/// <param name="InicioVigencia">Início da vigência.</param>
/// <param name="FimVigencia">Fim da vigência.</param>
/// <param name="CodigoTaxaTll">Código da taxa de licença (TLL) no CTM.</param>
/// <param name="Exercicio">Exercício fiscal da TLL.</param>
/// <param name="QuantidadeBaseTll">Quantidade-base da TLL (ex.: área/risco); ignorada no modo ValorFixo.</param>
/// <param name="VencimentoTll">Vencimento da guia da TLL.</param>
public sealed record EmitirAlvaraCommand(
    Guid ContribuinteId,
    Guid? ImovelId,
    EspecieAlvara Especie,
    string NomeEstabelecimento,
    string AtividadeCnae,
    DateOnly InicioVigencia,
    DateOnly FimVigencia,
    string CodigoTaxaTll,
    int Exercicio,
    decimal QuantidadeBaseTll,
    DateOnly VencimentoTll) : ICommand<ResultadoEmissaoAlvara>;

/// <summary>Regras de validação da emissão de alvará + TLL.</summary>
public sealed class EmitirAlvaraValidator : AbstractValidator<EmitirAlvaraCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirAlvaraValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Especie).IsInEnum();
        RuleFor(c => c.NomeEstabelecimento).NotEmpty().MaximumLength(200);
        RuleFor(c => c.AtividadeCnae).NotEmpty().MaximumLength(40);
        RuleFor(c => c.FimVigencia).GreaterThanOrEqualTo(c => c.InicioVigencia);
        RuleFor(c => c.CodigoTaxaTll).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.QuantidadeBaseTll).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da emissão de alvará + TLL.</summary>
public sealed class EmitirAlvaraHandler(
    IAlvaraRepository alvaras,
    ITabelaTaxaRepository tabelas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EmitirAlvaraCommand, ResultadoEmissaoAlvara>
{
    /// <inheritdoc />
    public async Task<ResultadoEmissaoAlvara> Handle(EmitirAlvaraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = await tabelas.ObterVigentePorCodigoAsync(request.CodigoTaxaTll, request.Exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há tabela de TLL vigente para o código {request.CodigoTaxaTll} no exercício {request.Exercicio}.");

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var imovelId = request.ImovelId is null ? (ImovelId?)null : new ImovelId(request.ImovelId.Value);

        var alvara = Alvara.Emitir(
            tenant.TenantId,
            contribuinteId,
            imovelId,
            request.Especie,
            request.NomeEstabelecimento,
            request.AtividadeCnae,
            request.InicioVigencia,
            request.FimVigencia);

        var valorTll = tabela.Calcular(request.QuantidadeBaseTll);

        // Fato gerador da TLL no exercício informado; data da constituição = "hoje" administrativo
        // (sem relógio no domínio — CLAUDE.md §16). A decadência (CTN art. 173, I) é aferida no agregado.
        var dataFatoGerador = new DateOnly(request.Exercicio, request.VencimentoTll.Month, 1);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var lancamento = Lancamento.LancarComImovel(
            tenant.TenantId,
            contribuinteId,
            TipoTributo.Taxa,
            Competencia.De(request.Exercicio, request.VencimentoTll.Month),
            valorTll,
            request.VencimentoTll,
            dataFatoGerador,
            hoje,
            imovelId);

        var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, contribuinteId, valorTll, numeroParcelas: 1, request.VencimentoTll);

        alvaras.Adicionar(alvara);
        lancamentos.Adicionar(lancamento);
        dams.Adicionar(dam);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoEmissaoAlvara(alvara.Id.Value, lancamento.Id.Value, dam.Id.Value, valorTll.Valor);
    }
}
