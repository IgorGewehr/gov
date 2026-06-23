using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio de estabelecimentos (sempre tenant-scoped via Global Query Filter).</summary>
public sealed class EstabelecimentoCadastroRepository(SaudeDbContext context) : IEstabelecimentoCadastroRepository
{
    /// <inheritdoc />
    public void Adicionar(Estabelecimento estabelecimento)
    {
        ArgumentNullException.ThrowIfNull(estabelecimento);
        context.Estabelecimentos.Add(estabelecimento);
    }

    /// <inheritdoc />
    public Task<Estabelecimento?> ObterPorIdAsync(EstabelecimentoId id, CancellationToken cancellationToken)
        => context.Estabelecimentos.FirstOrDefaultAsync(estabelecimento => estabelecimento.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorCnesAsync(CodigoCnes cnes, CancellationToken cancellationToken)
        => context.Estabelecimentos.AnyAsync(estabelecimento => estabelecimento.Cnes == cnes, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Estabelecimento> Itens, int Total)> BuscarAsync(
        string? termo,
        TipoEstabelecimento? tipo,
        SituacaoEstabelecimento? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Estabelecimentos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var nome = termo.Trim();
            // Cnes e mapeado por value converter (VO -> string), nao fatiavel por LIKE: o CNES casa por
            // igualdade exata quando o termo for um CNES valido completo. O nome casa por trecho.
            var digitos = BuscaTexto.SomenteDigitos(termo);
            var cnesExato = CodigoCnes.EhValido(digitos) ? new CodigoCnes(digitos) : (CodigoCnes?)null;
            consulta = consulta.Where(estabelecimento =>
                EF.Functions.Like(estabelecimento.Nome, "%" + nome + "%")
                || (cnesExato != null && estabelecimento.Cnes == cnesExato.Value));
        }

        if (tipo is { } filtroTipo)
        {
            consulta = consulta.Where(estabelecimento => estabelecimento.Tipo == filtroTipo);
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(estabelecimento => estabelecimento.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(estabelecimento => estabelecimento.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>Implementacao EF Core do repositorio de profissionais (sempre tenant-scoped via Global Query Filter).</summary>
public sealed class ProfissionalCadastroRepository(SaudeDbContext context) : IProfissionalCadastroRepository
{
    /// <inheritdoc />
    public void Adicionar(Profissional profissional)
    {
        ArgumentNullException.ThrowIfNull(profissional);
        context.Profissionais.Add(profissional);
    }

    /// <inheritdoc />
    public Task<Profissional?> ObterPorIdAsync(ProfissionalId id, CancellationToken cancellationToken)
        => context.Profissionais.FirstOrDefaultAsync(profissional => profissional.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        // Cpf e mapeado por value converter (VO -> string): a igualdade do VO inteiro e translatavel.
        return context.Profissionais.AnyAsync(profissional => profissional.Cpf == cpf, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Profissional> Itens, int Total)> BuscarAsync(
        string? termo,
        string? cbo,
        Guid? estabelecimentoId,
        SituacaoProfissional? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.Profissionais.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var nome = termo.Trim();
            // Cpf e mapeado por value converter (VO -> string), nao fatiavel por LIKE: o CPF casa por
            // igualdade exata quando o termo for um CPF valido completo. O nome casa por trecho.
            var digitos = BuscaTexto.SomenteDigitos(termo);
            var cpfExato = Cpf.TryCreate(digitos, out var cpf) ? cpf : null;
            consulta = consulta.Where(profissional =>
                EF.Functions.Like(profissional.Nome, "%" + nome + "%")
                || (cpfExato != null && profissional.Cpf == cpfExato));
        }

        if (!string.IsNullOrWhiteSpace(cbo) && Cbo.EhValido(BuscaTexto.SomenteDigitos(cbo)))
        {
            // Cbo e mapeado por value converter (VO -> string): compara-se o VO inteiro (translatavel).
            var cboFiltro = new Cbo(BuscaTexto.SomenteDigitos(cbo));
            consulta = consulta.Where(profissional =>
                profissional.Vinculos.Any(vinculo => vinculo.Cbo == cboFiltro));
        }

        if (estabelecimentoId is { } estab && estab != Guid.Empty)
        {
            var alvo = new EstabelecimentoId(estab);
            consulta = consulta.Where(profissional =>
                profissional.Vinculos.Any(vinculo => vinculo.EstabelecimentoId == alvo));
        }

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(profissional => profissional.Situacao == filtroSituacao);
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderBy(profissional => profissional.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}

/// <summary>
/// Implementacao REAL da porta de leitura/ACL <see cref="IEstabelecimentoRepository"/> consultando os
/// agregados LOCAIS de Estabelecimento e Profissional (substitui o <c>SimuladoEstabelecimentoRepository</c>
/// que so checava <c>Guid.Empty</c>). Tenant-scoped via Global Query Filter.
/// <para>
/// // TODO(prod: CNES oficial quando houver credencial). A operacao local roda sem credencial; a troca
/// por gateway oficial do CNES e da Onda 4/M10.
/// </para>
/// </summary>
public sealed class EstabelecimentoRepository(SaudeDbContext context) : IEstabelecimentoRepository
{
    /// <inheritdoc />
    public Task<bool> EstabelecimentoAtivoAsync(
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken)
        => context.Estabelecimentos.AnyAsync(
            estabelecimento => estabelecimento.Id == estabelecimentoId
                && estabelecimento.Situacao == SituacaoEstabelecimento.Ativo,
            cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ProfissionalAtivoAsync(
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        var profissional = await context.Profissionais
            .FirstOrDefaultAsync(p => p.Id == profissionalId, cancellationToken)
            .ConfigureAwait(false);
        if (profissional is null || profissional.Situacao != SituacaoProfissional.Ativo)
        {
            return false;
        }

        // Vinculo CBO ativo em QUALQUER dia da competencia (mes de referencia do atendimento).
        var inicioCompetencia = new DateOnly(competencia.Ano, competencia.Mes, 1);
        var fimCompetencia = inicioCompetencia.AddMonths(1).AddDays(-1);
        return profissional.Vinculos.Any(vinculo =>
            vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.DataInicio <= fimCompetencia
            && (vinculo.DataFim is null || vinculo.DataFim.Value >= inicioCompetencia));
    }

    /// <inheritdoc />
    public async Task<bool> ProfissionalComCrmAtivoAsync(ProfissionalId profissionalId, CancellationToken cancellationToken)
    {
        // TemCrmAtivo inspeciona o VO Registro (persistido via value converter): nao translatavel em SQL,
        // entao avalia-se a regra do dominio em memoria sobre o agregado carregado.
        var profissional = await context.Profissionais
            .FirstOrDefaultAsync(p => p.Id == profissionalId, cancellationToken)
            .ConfigureAwait(false);
        return profissional?.TemCrmAtivo ?? false;
    }
}
