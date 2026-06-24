using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

/// <summary>
/// Contador atomico do sequencial do NUP por <c>(TenantId, Ano)</c> — a "cabeca" da sequencia anual
/// (Decreto 8.539/2015). Substitui o <c>COUNT(*)</c> que sofria race condition: uma linha-contador por
/// tenant x exercicio, incrementada sob lock (UPDLOCK/HOLDLOCK em SqlServer) no mesmo SaveChanges da
/// autuacao, serializando autuacoes concorrentes do mesmo tenant x ano. Espelha o padrao atomico do
/// hash-chain (<c>UltimoSeloReader</c>). Reset anual: o primeiro do ano cria a linha com
/// <see cref="UltimoSequencial"/> = 1.
/// </summary>
public sealed class SequenciaNup : IMustHaveTenant
{
    /// <summary>Primeiro sequencial do exercicio.</summary>
    public const int SequencialInicial = 1;

    private SequenciaNup()
    {
    }

    private SequenciaNup(Guid tenantId, int ano, int ultimoSequencial)
    {
        TenantId = tenantId;
        Ano = ano;
        UltimoSequencial = ultimoSequencial;
    }

    /// <summary>Tenant (ente publico) dono do contador.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercicio (ano) do contador.</summary>
    public int Ano { get; private set; }

    /// <summary>Ultimo sequencial ja alocado neste tenant x ano.</summary>
    public int UltimoSequencial { get; private set; }

    /// <summary>
    /// Cria a linha-contador do exercicio com o primeiro sequencial alocado (1) — usado quando ainda
    /// nao ha contador para o tenant x ano (reset anual / primeiro do ano).
    /// </summary>
    /// <param name="tenantId">Tenant dono do contador.</param>
    /// <param name="ano">Exercicio.</param>
    /// <returns>Nova <see cref="SequenciaNup"/> com <see cref="UltimoSequencial"/> = 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o ano nao for positivo.</exception>
    public static SequenciaNup Iniciar(Guid tenantId, int ano)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ano);
        return new SequenciaNup(tenantId, ano, SequencialInicial);
    }

    /// <summary>
    /// Aloca e devolve o PROXIMO sequencial do exercicio, incrementando o contador atomicamente
    /// (a serializacao concorrente e garantida pelo lock na leitura da linha — Infrastructure).
    /// </summary>
    /// <returns>O proximo sequencial alocado.</returns>
    public int AlocarProximo()
    {
        UltimoSequencial += 1;
        return UltimoSequencial;
    }
}
