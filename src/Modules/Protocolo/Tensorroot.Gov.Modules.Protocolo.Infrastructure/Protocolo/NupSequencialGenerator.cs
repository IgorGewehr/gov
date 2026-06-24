using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo;

/// <summary>
/// Gerador do Numero Unico de Protocolo (NUP) no padrao CONARQ/Decreto 8.539/2015
/// (<c>nnnnnn/aaaa-dd</c>): sequencial anual por tenant + ano + digito verificador (modulo 11).
/// <para>
/// W9.4 (Peca 3): o sequencial e alocado por um CONTADOR ATOMICO <see cref="SequenciaNup"/>
/// (uma linha por <c>(TenantId, Ano)</c>), nao mais por <c>COUNT(*)</c>. Em SqlServer (producao)
/// a linha-contador e lida sob <c>UPDLOCK, HOLDLOCK</c> na conexao/transacao ATUAL do contexto,
/// serializando autuacoes concorrentes do mesmo tenant x ano (mesmo padrao do
/// <c>UltimoSeloReader</c> do hash-chain) — fechando o race que gerava NUP duplicado e fazia o
/// segundo SaveChanges explodir (DbUpdateException). Em SQLite/testes (escrita serializada) usa
/// LINQ; o indice unico <c>(TenantId, Nup)</c> permanece como backstop em qualquer provider.
/// O incremento e persistido no MESMO <c>SaveChanges</c> da autuacao (o lock segura ate o commit).
/// </para>
/// </summary>
public sealed class NupSequencialGenerator(
    ProtocoloDbContext context,
    ITenantContext tenantContext,
    TimeProvider timeProvider) : INupGenerator
{
    private const string ProviderSqlServer = "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>Largura (digitos) do corpo sequencial do NUP.</summary>
    private const int LarguraSequencial = 6;

    /// <inheritdoc />
    public async Task<Nup> GerarAsync(CancellationToken cancellationToken)
    {
        var ano = timeProvider.GetUtcNow().UtcDateTime.Year;
        var sequencial = await AlocarSequencialAsync(ano, cancellationToken).ConfigureAwait(false);

        var corpo = sequencial.ToString($"D{LarguraSequencial}", CultureInfo.InvariantCulture);
        var digito = CalcularDigitoVerificador(corpo, ano);

        var valor = $"{corpo}/{ano.ToString(CultureInfo.InvariantCulture)}-{digito.ToString("D2", CultureInfo.InvariantCulture)}";
        return new Nup(valor);
    }

    /// <summary>
    /// Aloca o proximo sequencial do tenant x ano: localiza (ou cria) a linha-contador, incrementa-a
    /// e a deixa marcada para persistir no SaveChanges da autuacao. Em SqlServer le sob lock.
    /// </summary>
    private async Task<int> AlocarSequencialAsync(int ano, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        // SqlServer (producao): pre-leitura sob UPDLOCK/HOLDLOCK serializa concorrentes do tenant x ano
        // antes do EF materializar a entidade rastreada (mesma corrida resolvida no hash-chain).
        if (EhSqlServer())
        {
            await LerContadorComLockAsync(tenantId, ano, cancellationToken).ConfigureAwait(false);
        }

        var contador = await context.SequenciasNup
            .FirstOrDefaultAsync(linha => linha.TenantId == tenantId && linha.Ano == ano, cancellationToken)
            .ConfigureAwait(false);

        if (contador is null)
        {
            // Primeiro do ano / reset anual: cria a linha ja com o sequencial 1 alocado. O INSERT
            // concorrente perde para a PK composta (TenantId, Ano) e o vencedor relê o contador.
            contador = SequenciaNup.Iniciar(tenantId, ano);
            context.SequenciasNup.Add(contador);
            return SequenciaNup.SequencialInicial;
        }

        return contador.AlocarProximo();
    }

    private bool EhSqlServer()
        => string.Equals(context.Database.ProviderName, ProviderSqlServer, StringComparison.Ordinal);

    /// <summary>
    /// Le a linha-contador sob <c>UPDLOCK, HOLDLOCK</c> (range lock serializable) na conexao/transacao
    /// atual do contexto — espelha <c>UltimoSeloReader.LerComLockAsync</c>. O HOLDLOCK impede que duas
    /// requisicoes concorrentes do mesmo tenant x ano leiam a mesma "cabeca" antes do incremento.
    /// </summary>
    private async Task LerContadorComLockAsync(Guid tenantId, int ano, CancellationToken cancellationToken)
    {
        var entityType = context.Model.FindEntityType(typeof(SequenciaNup))
            ?? throw new InvalidOperationException("Entidade SequenciaNup nao mapeada no modelo.");
        var schema = entityType.GetSchema();
        var tabela = entityType.GetTableName() ?? "SequenciasNup";
        var nomeQualificado = schema is null ? $"[{tabela}]" : $"[{schema}].[{tabela}]";

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var abrira = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            abrira = true;
        }

        try
        {
            await using var comando = connection.CreateCommand();
            comando.Transaction = transaction;
            comando.CommandText =
                $"SELECT [UltimoSequencial] FROM {nomeQualificado} WITH (UPDLOCK, HOLDLOCK) "
                + "WHERE [TenantId] = @tenantId AND [Ano] = @ano";

            var paramTenant = comando.CreateParameter();
            paramTenant.ParameterName = "@tenantId";
            paramTenant.Value = tenantId;
            comando.Parameters.Add(paramTenant);

            var paramAno = comando.CreateParameter();
            paramAno.ParameterName = "@ano";
            paramAno.Value = ano;
            comando.Parameters.Add(paramAno);

            // Resultado descartado: o objetivo e adquirir o lock (UPDLOCK/HOLDLOCK) que serializa
            // os concorrentes; a leitura rastreada/incremento ocorre logo apos, sob o mesmo lock.
            _ = await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // So fecha se ESTE metodo abriu a conexao sem transacao ambiente; com transacao ambiente a
            // conexao pertence a UoW e deve permanecer aberta para o SaveChanges subsequente.
            if (abrira && transaction is null)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Calcula o digito verificador (modulo 11) sobre o sequencial concatenado ao ano,
    /// conforme a Orientacao Tecnica do NUP (Decreto 8.539/2015). Inalterado pela Peca 3.
    /// </summary>
    private static int CalcularDigitoVerificador(string sequencial, int ano)
    {
        var baseCalculo = sequencial + ano.ToString(CultureInfo.InvariantCulture);
        var soma = 0;
        var peso = 2;
        for (var indice = baseCalculo.Length - 1; indice >= 0; indice--)
        {
            soma += (baseCalculo[indice] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        var digito = 11 - resto;
        return digito >= 10 ? 0 : digito;
    }
}
