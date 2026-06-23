using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.Saude.Application.Fiscal;

/// <summary>Uma linha de execução fiscal de Saúde a projetar no read model (Via A2).</summary>
/// <param name="Tipo">Natureza da linha (receita-base / despesa de Saúde).</param>
/// <param name="Funcao">Função de governo (2 dígitos) — obrigatória em despesa, ignorada em receita.</param>
/// <param name="Subfuncao">Subfunção (3 dígitos), opcional — chave da classificação ASPS fina.</param>
/// <param name="FonteRecurso">Fonte de recurso (opcional).</param>
/// <param name="Valor">Valor executado (&gt;= 0).</param>
/// <param name="OrigemHash">Hash determinístico da origem (idempotência).</param>
public sealed record LinhaExecucaoSaudeEntrada(
    TipoLinhaExecucaoSaude Tipo,
    string? Funcao,
    string? Subfuncao,
    string? FonteRecurso,
    decimal Valor,
    string OrigemHash);

/// <summary>
/// <b>S-1 (Via A2).</b> Projeta linhas de execução fiscal de Saúde (decompostas por funcional/subfunção/
/// fonte) no read model, de forma idempotente por <c>OrigemHash</c>. É o ponto de entrada alimentado por um
/// ACL que consome a contabilidade (Finanças.Contracts) e, enquanto esse alimentador automático não existe,
/// também o que permite semear execução sintética. NÃO toma decisão fiscal — só persiste o dado já
/// decomposto; a classificação ASPS (LC 141 arts. 3º/4º) ocorre na leitura, contra as regras vigentes.
/// </summary>
/// <param name="Exercicio">Ano de exercício das linhas.</param>
/// <param name="Linhas">Linhas a projetar.</param>
public sealed record RegistrarExecucaoSaudeCommand(int Exercicio, IReadOnlyList<LinhaExecucaoSaudeEntrada> Linhas) : ICommand<int>;

/// <summary>Validação da projeção de execução de Saúde.</summary>
public sealed class RegistrarExecucaoSaudeValidator : AbstractValidator<RegistrarExecucaoSaudeCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarExecucaoSaudeValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThan(0);
        RuleFor(c => c.Linhas).NotEmpty();
        RuleForEach(c => c.Linhas).ChildRules(linha =>
        {
            linha.RuleFor(l => l.OrigemHash).NotEmpty();
            linha.RuleFor(l => l.Valor).GreaterThanOrEqualTo(0m);
            linha.RuleFor(l => l.Funcao)
                .NotEmpty()
                .When(l => l.Tipo == TipoLinhaExecucaoSaude.DespesaSaude)
                .WithMessage("Despesa de Saude exige a funcao (2 digitos).");
        });
    }
}

/// <summary>Handler que projeta as linhas, ignorando as já projetadas (idempotência por OrigemHash).</summary>
public sealed class RegistrarExecucaoSaudeHandler(
    ILinhaExecucaoSaudeRepository linhas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarExecucaoSaudeCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(RegistrarExecucaoSaudeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var registradas = 0;
        foreach (var entrada in request.Linhas)
        {
            if (await linhas.ExisteAsync(entrada.OrigemHash, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (entrada.Tipo == TipoLinhaExecucaoSaude.ReceitaBaseImpostosTransferencias)
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
