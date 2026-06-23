using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>Gera o evento S-1000 (empregador/orgao) a partir da configuracao do tenant. Idempotente.</summary>
public sealed record GerarS1000Command : ICommand<Guid>;

/// <summary>Handler do S-1000.</summary>
public sealed class GerarS1000Handler(IEmpregadorESocialProvider empregador, GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS1000Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS1000Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(p.CnpjEnte))
        {
            throw new InvalidOperationException("CNPJ do ente nao configurado (RecursosHumanos:ESocial:CnpjEnte).");
        }

        var insumo = new InsumoS1000(
            TipoInscricao.Cnpj,
            p.CnpjEnte,
            p.NomeEnte ?? string.Empty,
            p.ClassTrib ?? string.Empty,
            p.CnpjEfr,
            p.InicioValidade ?? string.Empty);

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1000Empregador, p.CnpjEnte);
        var xml = GeradorEventosESocial.GerarS1000(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S1000Empregador, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}

/// <summary>Gera o evento S-1005 (estabelecimento). Idempotente por (tipo, nrInsc do estabelecimento).</summary>
/// <param name="CnpjEstabelecimento">CNPJ do estabelecimento/unidade.</param>
/// <param name="CnaePreponderante">CNAE preponderante.</param>
public sealed record GerarS1005Command(string CnpjEstabelecimento, string CnaePreponderante) : ICommand<Guid>;

/// <summary>Handler do S-1005.</summary>
public sealed class GerarS1005Handler(IEmpregadorESocialProvider empregador, GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS1005Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS1005Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CnpjEstabelecimento);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);

        var insumo = new InsumoS1005(
            TipoInscricao.Cnpj,
            request.CnpjEstabelecimento,
            p.InicioValidade ?? string.Empty,
            request.CnaePreponderante,
            // // TODO(validar-oficial): aliqGilrat aplicavel a orgao publico RPPS; nulo por enquanto.
            AliqGilrat: null);

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1005Estabelecimento, request.CnpjEstabelecimento);
        var xml = GeradorEventosESocial.GerarS1005(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S1005Estabelecimento, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}

/// <summary>
/// Gera o evento S-1010 (rubrica) a partir de uma <see cref="RubricaFolha"/> vigente na competencia.
/// Idempotente por (tipo, codigo da rubrica). // TODO(validar-oficial): natRubr/codInc* das Tabelas
/// 03/20/21/23 — aqui derivados das flags de incidencia ate o XSD/Anexo I serem congelados.
/// </summary>
/// <param name="CodigoRubrica">Codigo da rubrica (S-1010).</param>
/// <param name="Ano">Ano da competencia de referencia (para localizar a vigente).</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record GerarS1010Command(string CodigoRubrica, int Ano, int Mes) : ICommand<Guid>;

/// <summary>Handler do S-1010.</summary>
public sealed class GerarS1010Handler(
    IEmpregadorESocialProvider empregador,
    IRubricaFolhaRepository rubricas,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS1010Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS1010Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var competencia = Competencia.De(request.Ano, request.Mes);
        var rubrica = await rubricas.ObterVigentePorCodigoAsync(Rubrica.De(request.CodigoRubrica), competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rubrica '{request.CodigoRubrica}' nao vigente na competencia {competencia}.");

        var insumo = MapearRubrica(rubrica, p.IdeTabRubricas, p.InicioValidade ?? competencia.ToString());
        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1010Rubrica, rubrica.Codigo.Codigo);
        var xml = GeradorEventosESocial.GerarS1010(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S1010Rubrica, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }

    /// <summary>
    /// ACL RubricaFolha -> InsumoS1010. tpRubr mapeia <see cref="NaturezaRubrica"/>; os codInc* sao
    /// DERIVADOS das flags de incidencia (regra confirmada: informativa => codIncCP=00, codIncIRRF=9).
    /// // TODO(validar-oficial): substituir esta derivacao pelos codigos exatos das Tabelas 03/20/21/23
    /// quando o XSD/Anexo I da versao travada estiver congelado (ESOCIAL-SPEC §1.3).
    /// </summary>
    internal static InsumoS1010 MapearRubrica(RubricaFolha rubrica, string ideTabRubr, string inicioValidade)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        var tpRubr = (int)rubrica.Natureza; // 1=provento, 2=desconto, 3/4=informativa (alinhado ao tpRubr).
        var informativa = rubrica.Natureza is NaturezaRubrica.Informativa or NaturezaRubrica.InformativaDedutora;

        // codIncCP: "00" se informativa (CONFIRMADO) ou sem incidencia; senao "11" (incidencia normal,
        // placeholder estrutural). // TODO(validar-oficial: Tabela 20).
        var codIncCp = informativa || !(rubrica.IncideInss || rubrica.IncideRpps) ? "00" : "11";
        // codIncIRRF: "9" se informativa (CONFIRMADO); senao "11" (tributavel) ou "00" (nao). // TODO(validar-oficial: Tabela 21).
        var codIncIrrf = informativa ? "9" : rubrica.IncideIrrf ? "11" : "00";
        // codIncFGTS: "00" sem FGTS; "11" com (placeholder). // TODO(validar-oficial: Tabela 23).
        var codIncFgts = rubrica.IncideFgts ? "11" : "00";
        // natRubr: pendente da Tabela 03; usa placeholder estrutural. // TODO(validar-oficial: Tabela 03).
        const string natRubr = "0000";

        return new InsumoS1010(
            rubrica.Codigo.Codigo,
            ideTabRubr,
            rubrica.Descricao,
            natRubr,
            tpRubr,
            codIncCp,
            codIncIrrf,
            codIncFgts,
            inicioValidade);
    }
}
