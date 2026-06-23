using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core dos estabelecimentos sujeitos a VISA (tenant-scoped via Global Query Filter).</summary>
public sealed class EstabelecimentoFiscalizavelRepository(SaudeDbContext context) : IEstabelecimentoFiscalizavelRepository
{
    /// <inheritdoc />
    public void Adicionar(EstabelecimentoFiscalizavel estabelecimento)
    {
        ArgumentNullException.ThrowIfNull(estabelecimento);
        context.EstabelecimentosFiscalizaveis.Add(estabelecimento);
    }

    /// <inheritdoc />
    public Task<EstabelecimentoFiscalizavel?> ObterPorIdAsync(EstabelecimentoFiscalizavelId id, CancellationToken cancellationToken)
        => context.EstabelecimentosFiscalizaveis.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteAsync(string documentoPersistido, string razaoSocial, CancellationToken cancellationToken)
    {
        // Compara pela forma persistida do documento (o value converter traduz a igualdade do VO para a
        // coluna "DocumentoPersistido"). Reconstroi o VO a partir da forma persistida para a comparacao.
        var documento = DocumentoResponsavel.DePersistencia(documentoPersistido);
        var razao = razaoSocial.Trim();
        return context.EstabelecimentosFiscalizaveis.AnyAsync(
            e => e.Documento == documento && e.RazaoSocial == razao,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EstabelecimentoFiscalizavel> Itens, int Total)> BuscarAsync(
        string? termo,
        RamoVisa? ramo,
        GrauRiscoSanitario? risco,
        SituacaoEstabelecimentoVisa? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.EstabelecimentosFiscalizaveis.AsQueryable();

        if (ramo is { } r)
        {
            consulta = consulta.Where(e => e.Ramo == r);
        }

        if (risco is { } g)
        {
            consulta = consulta.Where(e => e.Risco == g);
        }

        if (situacao is { } s)
        {
            consulta = consulta.Where(e => e.Situacao == s);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            var padrao = "%" + t + "%";
            // Razao social por LIKE; documento por igualdade exata (PJ/PF) quando o termo for so digitos.
            var digitos = new string([.. t.Where(char.IsAsciiDigit)]);
            var documento = digitos.Length is 11 or 14 ? TentarDocumento(t) : null;
            consulta = documento is { } doc
                ? consulta.Where(e => EF.Functions.Like(e.RazaoSocial, padrao) || e.Documento == doc)
                : consulta.Where(e => EF.Functions.Like(e.RazaoSocial, padrao));
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await consulta
            .OrderBy(e => e.RazaoSocial)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    // Converte um termo de busca em documento (CNPJ/CPF) quando valido; null se nao for um documento.
    private static DocumentoResponsavel? TentarDocumento(string termo)
    {
        try
        {
            return DocumentoResponsavel.Criar(termo);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>Implementacao EF Core das inspecoes/vistorias sanitarias (tenant-scoped).</summary>
public sealed class InspecaoRepository(SaudeDbContext context) : IInspecaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Inspecao inspecao)
    {
        ArgumentNullException.ThrowIfNull(inspecao);
        context.Inspecoes.Add(inspecao);
    }

    /// <inheritdoc />
    public Task<Inspecao?> ObterPorIdAsync(InspecaoId id, CancellationToken cancellationToken)
        => context.Inspecoes.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inspecao>> ListarPorEstabelecimentoAsync(
        EstabelecimentoFiscalizavelId estabelecimentoId, CancellationToken cancellationToken)
    {
        var itens = await context.Inspecoes
            .Where(i => i.EstabelecimentoFiscalizavelId == estabelecimentoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. itens.OrderByDescending(i => i.DataInspecao)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inspecao>> ListarAgendaAsync(
        DateOnly de, DateOnly ate, SituacaoInspecao? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.Inspecoes.Where(i => i.DataInspecao >= de && i.DataInspecao <= ate);
        if (situacao is { } s)
        {
            consulta = consulta.Where(i => i.Situacao == s);
        }

        var itens = await consulta.ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. itens.OrderBy(i => i.DataInspecao)];
    }
}

/// <summary>Implementacao EF Core dos autos da VISA (tenant-scoped).</summary>
public sealed class AutoVisaRepository(SaudeDbContext context) : IAutoVisaRepository
{
    /// <inheritdoc />
    public void Adicionar(AutoVisa autoVisa)
    {
        ArgumentNullException.ThrowIfNull(autoVisa);
        context.AutosVisa.Add(autoVisa);
    }

    /// <inheritdoc />
    public Task<AutoVisa?> ObterPorIdAsync(AutoVisaId id, CancellationToken cancellationToken)
        => context.AutosVisa.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteNumeroAsync(string numero, CancellationToken cancellationToken)
    {
        var n = numero.Trim();
        return context.AutosVisa.AnyAsync(a => a.Numero == n, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutoVisa>> ListarPorSituacaoAsync(SituacaoAutoVisa? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.AutosVisa.AsQueryable();
        if (situacao is { } s)
        {
            consulta = consulta.Where(a => a.Situacao == s);
        }

        var itens = await consulta.ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. itens.OrderByDescending(a => a.DataLavratura)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutoVisa>> ListarPrazosVencidosAsync(DateOnly ate, CancellationToken cancellationToken)
    {
        var itens = await context.AutosVisa
            .Where(a => a.Situacao == SituacaoAutoVisa.Lavrado && a.PrazoFinal < ate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. itens.OrderBy(a => a.PrazoFinal)];
    }
}

/// <summary>Implementacao EF Core das licencas/alvaras sanitarios (tenant-scoped).</summary>
public sealed class LicencaSanitariaRepository(SaudeDbContext context) : ILicencaSanitariaRepository
{
    /// <inheritdoc />
    public void Adicionar(LicencaSanitaria licenca)
    {
        ArgumentNullException.ThrowIfNull(licenca);
        context.LicencasSanitarias.Add(licenca);
    }

    /// <inheritdoc />
    public Task<LicencaSanitaria?> ObterPorIdAsync(LicencaSanitariaId id, CancellationToken cancellationToken)
        => context.LicencasSanitarias.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<LicencaSanitaria>> ListarPorEstabelecimentoAsync(
        EstabelecimentoFiscalizavelId estabelecimentoId, CancellationToken cancellationToken)
    {
        var itens = await context.LicencasSanitarias
            .Where(l => l.EstabelecimentoFiscalizavelId == estabelecimentoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. itens.OrderByDescending(l => l.EmitidaEm)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LicencaSanitaria>> ListarAVencerAsync(DateOnly ate, CancellationToken cancellationToken)
    {
        var itens = await context.LicencasSanitarias
            .Where(l => l.Situacao == SituacaoLicenca.Vigente && l.ValidadeAte <= ate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. itens.OrderBy(l => l.ValidadeAte)];
    }
}
