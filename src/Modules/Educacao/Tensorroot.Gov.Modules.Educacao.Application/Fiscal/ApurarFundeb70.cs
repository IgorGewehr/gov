using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>Resultado da apuração do piso de 70% do FUNDEB (remuneração dos profissionais da educação).</summary>
/// <param name="Exercicio">Ano de exercício apurado.</param>
/// <param name="ReceitaFundeb">Receita FUNDEB do exercício (cota-parte + complementações recebidas).</param>
/// <param name="RemuneracaoProfissionais">Remuneração paga aos profissionais da educação (folha — RH/Contracts).</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PisoMinimo">Piso mínimo vigente (default legal 70%).</param>
/// <param name="Atingido">Se o piso de 70% foi atingido.</param>
public sealed record ApuracaoFundeb70Resultado(
    int Exercicio,
    decimal ReceitaFundeb,
    decimal RemuneracaoProfissionais,
    decimal PercentualAplicado,
    decimal PisoMinimo,
    bool Atingido);

/// <summary>
/// <b>E-2.</b> Apura o piso de <b>70% do FUNDEB</b> na remuneração dos profissionais da educação básica
/// (EC 108/2020; Lei 14.113/2020 art. 26) de um exercício, tenant-scoped. A receita FUNDEB vem da
/// <see cref="DistribuicaoFundeb"/> (E-3, cota-parte + complementações); a remuneração paga vem da folha
/// do <b>RH via Contracts</b> ou, até o cruzamento, do total informado pelo município
/// (<see cref="IRemuneracaoMagisterioReadModel"/>). Read-only e reprodutível (sem relógio).
/// </summary>
/// <param name="Exercicio">Ano de exercício a apurar.</param>
public sealed record ApurarFundeb70Query(int Exercicio) : IQuery<ApuracaoFundeb70Resultado>;

/// <summary>
/// Handler da apuração do piso de 70% do FUNDEB: obtém a receita FUNDEB do exercício (da distribuição
/// E-3), a remuneração paga aos profissionais (RH/Contracts ou parâmetro) e o piso vigente, delega ao
/// <see cref="IndicadorAplicacaoFundeb"/> e projeta o resultado. Toda a regra fiscal é do domínio.
/// Espelha o <c>ApurarAspsHandler</c> da Saúde, cruzando com a folha do RH (como a MSC/remessa-folha).
/// </summary>
public sealed class ApurarFundeb70Handler(
    IDistribuicaoFundebRepository distribuicoes,
    IRemuneracaoMagisterioReadModel remuneracao,
    IParametroFundebProvider parametros)
    : IQueryHandler<ApurarFundeb70Query, ApuracaoFundeb70Resultado>
{
    /// <inheritdoc />
    public async Task<ApuracaoFundeb70Resultado> Handle(ApurarFundeb70Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var distribuicao = await distribuicoes.ObterPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var receitaFundeb = distribuicao?.ReceitaFundebTotal ?? 0m;

        var remuneracaoProfissionais = await remuneracao
            .ObterRemuneracaoProfissionaisAsync(request.Exercicio, cancellationToken)
            .ConfigureAwait(false);

        var piso = await parametros.ObterPisoRemuneracaoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var indicador = IndicadorAplicacaoFundeb.Apurar(receitaFundeb, remuneracaoProfissionais, piso);

        return new ApuracaoFundeb70Resultado(
            request.Exercicio,
            indicador.ReceitaFundeb,
            indicador.RemuneracaoProfissionais,
            indicador.PercentualAplicado,
            indicador.PisoMinimo,
            indicador.Atingido);
    }
}

/// <summary>
/// <b>E-2 (cruzamento com a folha).</b> Define o total de remuneração dos profissionais da educação básica
/// do exercício (numerador dos 70%). É o ponto de entrada quando o município informa o total como
/// <b>parâmetro</b> — o caminho automático é o consumo do evento de folha do RH (Contracts), que chama a
/// mesma porta. Idempotente por exercício (sobrescreve o valor do exercício).
/// </summary>
/// <param name="Exercicio">Ano de exercício.</param>
/// <param name="RemuneracaoProfissionais">Total pago a profissionais da educação básica (&gt;= 0).</param>
public sealed record RegistrarRemuneracaoMagisterioCommand(int Exercicio, decimal RemuneracaoProfissionais) : ICommand;

/// <summary>Validação do registro da remuneração do magistério.</summary>
public sealed class RegistrarRemuneracaoMagisterioValidator : AbstractValidator<RegistrarRemuneracaoMagisterioCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarRemuneracaoMagisterioValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThan(0);
        RuleFor(c => c.RemuneracaoProfissionais).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler do registro da remuneração do magistério (alimenta o cruzamento do 70%).</summary>
public sealed class RegistrarRemuneracaoMagisterioHandler(
    IRemuneracaoMagisterioReadModel remuneracao,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarRemuneracaoMagisterioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarRemuneracaoMagisterioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await remuneracao
            .DefinirRemuneracaoProfissionaisAsync(request.Exercicio, request.RemuneracaoProfissionais, cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
