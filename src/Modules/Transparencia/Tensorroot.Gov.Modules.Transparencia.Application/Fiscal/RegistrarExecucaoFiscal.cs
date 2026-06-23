using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Fiscal;

/// <summary>Tipo de linha de execução a registrar (espelha <see cref="TipoLinhaExecucao"/>).</summary>
public enum TipoLinhaExecucaoEntrada
{
    /// <summary>Receita-base (impostos + transferências constitucionais).</summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa setorial (a classificar por função/fonte).</summary>
    DespesaSetorial = 2,
}

/// <summary>Uma linha de execução fiscal a projetar no read model.</summary>
/// <param name="Tipo">Natureza da linha.</param>
/// <param name="Funcao">Função de governo (2 dígitos) — obrigatória em despesa, ignorada em receita.</param>
/// <param name="FonteRecurso">Fonte de recurso (opcional).</param>
/// <param name="Valor">Valor executado (&gt;= 0).</param>
/// <param name="OrigemHash">Hash determinístico da origem (idempotência).</param>
public sealed record LinhaExecucaoEntrada(
    TipoLinhaExecucaoEntrada Tipo,
    string? Funcao,
    string? FonteRecurso,
    decimal Valor,
    string OrigemHash);

/// <summary>
/// <b>M7.0.0.</b> Projeta linhas de execução fiscal (decompostas por função/fonte) no read model, de
/// forma idempotente por <c>OrigemHash</c>. É o ponto de entrada da Via A2 (alimentado por um ACL que
/// consome a contabilidade) e também o que os testes usam para semear execução sintética. Não toma
/// decisão fiscal — só persiste o dado já decomposto; a classificação setorial ocorre na leitura.
/// </summary>
/// <param name="Exercicio">Ano de exercício das linhas.</param>
/// <param name="Linhas">Linhas a projetar.</param>
public sealed record RegistrarExecucaoFiscalCommand(int Exercicio, IReadOnlyList<LinhaExecucaoEntrada> Linhas) : ICommand<int>;

/// <summary>Handler que projeta as linhas de execução, ignorando as já projetadas (idempotência I-13).</summary>
public sealed class RegistrarExecucaoFiscalHandler(
    ILinhaExecucaoFiscalRepository linhas,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarExecucaoFiscalCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(RegistrarExecucaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // O TenantSaveChangesInterceptor recarimba o TenantId na inserção; aqui ele só nasce coerente.
        var tenantId = tenant.TenantId;
        var registradas = 0;

        foreach (var entrada in request.Linhas)
        {
            if (await linhas.ExisteAsync(entrada.OrigemHash, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var linha = entrada.Tipo == TipoLinhaExecucaoEntrada.ReceitaBaseImpostosTransferencias
                ? LinhaExecucaoFiscal.ReceitaBase(tenantId, request.Exercicio, entrada.Valor, entrada.OrigemHash)
                : LinhaExecucaoFiscal.Despesa(
                    tenantId,
                    request.Exercicio,
                    entrada.Funcao ?? throw new ArgumentException("Despesa exige função.", nameof(request)),
                    entrada.FonteRecurso,
                    entrada.Valor,
                    entrada.OrigemHash);

            await linhas.AdicionarAsync(linha, cancellationToken).ConfigureAwait(false);
            registradas++;
        }

        if (registradas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return registradas;
    }
}
