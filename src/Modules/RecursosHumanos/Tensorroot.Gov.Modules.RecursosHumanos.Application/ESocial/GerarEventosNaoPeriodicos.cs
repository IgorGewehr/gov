using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Gera o S-2200 (admissao) a partir do agregado <see cref="Servidor"/>. Idempotente por (tipo,
/// servidorId). // TODO(validar-oficial): roteamento S-2200 x S-2300 (TSVE) por codCateg; o
/// <c>codCateg</c> e parametro porque o <see cref="Servidor"/> ainda nao o carrega (GAP — ESOCIAL-SPEC §1.4).
/// </summary>
/// <param name="ServidorId">Servidor admitido.</param>
/// <param name="CodCateg">Categoria eSocial do trabalhador (Tabela 01). // TODO(validar-oficial).</param>
/// <param name="CodCargo">Codigo do cargo (S-1010/estrutura). // TODO(validar-oficial).</param>
/// <param name="VrSalFx">Valor do salario fixo/vencimento do vinculo.</param>
public sealed record GerarS2200Command(Guid ServidorId, string CodCateg, string CodCargo, decimal VrSalFx) : ICommand<Guid>;

/// <summary>Handler do S-2200.</summary>
public sealed class GerarS2200Handler(
    IEmpregadorESocialProvider empregador,
    IServidorRepository servidores,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS2200Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS2200Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CodCateg);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var insumo = new InsumoS2200(
            servidor.Cpf.ToString(),
            servidor.DadosPessoais.Nome,
            servidor.DadosPessoais.DataNascimento,
            servidor.Matricula.Valor,
            // // TODO(validar-oficial): dtAdm = qual marco (nomeacao/posse/exercicio) para estatutario.
            servidor.DataPosse ?? servidor.DataNomeacao,
            request.CodCateg,
            RoteadorRemuneracao.DerivarTpRegPrev(servidor.Regime),
            request.CodCargo,
            request.VrSalFx);

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S2200Admissao, servidor.Id.Value.ToString());
        var xml = GeradorEventosESocial.GerarS2200(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S2200Admissao, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}

/// <summary>
/// Gera o S-2299 (desligamento) a partir do <see cref="Servidor"/> desligado. Idempotente por (tipo,
/// servidorId). // TODO(validar-oficial): mtvDeslig (Tabela 19) e verbasResc.
/// </summary>
/// <param name="ServidorId">Servidor desligado.</param>
/// <param name="MtvDeslig">Motivo do desligamento (Tabela 19). // TODO(validar-oficial).</param>
public sealed record GerarS2299Command(Guid ServidorId, string MtvDeslig) : ICommand<Guid>;

/// <summary>Handler do S-2299.</summary>
public sealed class GerarS2299Handler(
    IEmpregadorESocialProvider empregador,
    IServidorRepository servidores,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS2299Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS2299Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MtvDeslig);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        if (servidor.DataDesligamento is not { } dataDeslig)
        {
            throw new InvalidOperationException("Servidor nao esta desligado; S-2299 nao aplicavel.");
        }

        var insumo = new InsumoS2299(servidor.Cpf.ToString(), servidor.Matricula.Valor, dataDeslig, request.MtvDeslig);
        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S2299Desligamento, servidor.Id.Value.ToString());
        var xml = GeradorEventosESocial.GerarS2299(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S2299Desligamento, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}
