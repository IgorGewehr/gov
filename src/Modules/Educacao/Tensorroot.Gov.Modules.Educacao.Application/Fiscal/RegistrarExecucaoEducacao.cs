using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>Uma linha de execução fiscal de Educação a projetar no read model (Via A2).</summary>
/// <param name="Tipo">Natureza da linha (receita-base / despesa de Educação).</param>
/// <param name="Funcao">Função de governo (2 dígitos) — obrigatória em despesa, ignorada em receita.</param>
/// <param name="Subfuncao">Subfunção (3 dígitos), opcional — chave da classificação MDE fina.</param>
/// <param name="FonteRecurso">Fonte de recurso (opcional).</param>
/// <param name="Valor">Valor executado (&gt;= 0).</param>
/// <param name="OrigemHash">Hash determinístico da origem (idempotência).</param>
public sealed record LinhaExecucaoEducacaoEntrada(
    TipoLinhaExecucaoEducacao Tipo,
    string? Funcao,
    string? Subfuncao,
    string? FonteRecurso,
    decimal Valor,
    string OrigemHash);

/// <summary>
/// <b>E-1 (Via A2).</b> Projeta linhas de execução fiscal de Educação (decompostas por funcional/subfunção/
/// fonte) no read model, de forma idempotente por <c>OrigemHash</c>. É o ponto de entrada alimentado por um
/// ACL que consome a contabilidade (Finanças.Contracts) e, enquanto esse alimentador automático não existe,
/// também o que permite semear execução sintética. NÃO toma decisão fiscal — só persiste o dado já
/// decomposto; a classificação MDE (LDB arts. 70/71) ocorre na leitura, contra as regras vigentes.
/// Espelha o <c>RegistrarExecucaoSaudeCommand</c> da Saúde.
/// </summary>
/// <param name="Exercicio">Ano de exercício das linhas.</param>
/// <param name="Linhas">Linhas a projetar.</param>
public sealed record RegistrarExecucaoEducacaoCommand(int Exercicio, IReadOnlyList<LinhaExecucaoEducacaoEntrada> Linhas) : ICommand<int>;

/// <summary>Validação da projeção de execução de Educação.</summary>
public sealed class RegistrarExecucaoEducacaoValidator : AbstractValidator<RegistrarExecucaoEducacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarExecucaoEducacaoValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThan(0);
        RuleFor(c => c.Linhas).NotEmpty();
        RuleForEach(c => c.Linhas).ChildRules(linha =>
        {
            linha.RuleFor(l => l.OrigemHash).NotEmpty();
            linha.RuleFor(l => l.Valor).GreaterThanOrEqualTo(0m);
            linha.RuleFor(l => l.Funcao)
                .NotEmpty()
                .When(l => l.Tipo == TipoLinhaExecucaoEducacao.DespesaEducacao)
                .WithMessage("Despesa de Educacao exige a funcao (2 digitos).");
        });
    }
}

/// <summary>Handler que projeta as linhas, ignorando as já projetadas (idempotência por OrigemHash).</summary>
public sealed class RegistrarExecucaoEducacaoHandler(
    ILinhaExecucaoEducacaoRepository linhas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarExecucaoEducacaoCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(RegistrarExecucaoEducacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var registradas = 0;
        foreach (var entrada in request.Linhas)
        {
            if (await linhas.ExisteAsync(entrada.OrigemHash, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (entrada.Tipo == TipoLinhaExecucaoEducacao.ReceitaBaseImpostosTransferencias)
            {
                await linhas.AdicionarReceitaBaseAsync(request.Exercicio, entrada.Valor, entrada.OrigemHash, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await linhas.AdicionarDespesaAsync(
                    request.Exercicio,
                    entrada.Funcao ?? throw new ArgumentException("Despesa exige funcao.", nameof(request)),
                    entrada.Subfuncao,
                    entrada.FonteRecurso,
                    entrada.Valor,
                    entrada.OrigemHash,
                    cancellationToken).ConfigureAwait(false);
            }

            registradas++;
        }

        if (registradas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return registradas;
    }
}
