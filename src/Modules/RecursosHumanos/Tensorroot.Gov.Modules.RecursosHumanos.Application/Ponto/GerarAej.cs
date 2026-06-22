using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>Artefato AEJ gerado: nome sugerido, conteudo posicional e a assinatura CAdES destacada (.p7s).</summary>
/// <param name="NomeArquivo">Nome sugerido do arquivo (AEJ).</param>
/// <param name="Conteudo">Bytes do AEJ (ISO-8859-1).</param>
/// <param name="AssinaturaCades">Assinatura CAdES detached (.p7s) sobre o conteudo; vazia se nao assinada.</param>
public sealed record ArtefatoAej(string NomeArquivo, byte[] Conteudo, byte[] AssinaturaCades);

/// <summary>
/// Gera o AEJ (Arquivo Eletronico de Jornada — Anexo VI da Portaria MTP 671/2021) de uma competencia:
/// a partir dos servidores apurados, RE-TRATA as marcacoes (sem alterar o AFD) para compor as linhas
/// diarias da jornada tratada e assina em CAdES destacado. // TODO(validar-oficial): leiaute do Anexo VI.
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia (1 a 12).</param>
/// <param name="Assinar">Quando verdadeiro, anexa a assinatura CAdES detached.</param>
public sealed record GerarAejQuery(int Ano, int Mes, bool Assinar = true) : IQuery<ArtefatoAej>;

/// <summary>Regras de validacao da geracao do AEJ.</summary>
public sealed class GerarAejValidator : AbstractValidator<GerarAejQuery>
{
    /// <summary>Define as regras.</summary>
    public GerarAejValidator()
    {
        RuleFor(q => q.Ano).InclusiveBetween(2000, 2100);
        RuleFor(q => q.Mes).InclusiveBetween(1, 12);
    }
}

/// <summary>Handler da geracao do AEJ.</summary>
public sealed class GerarAejHandler(
    IApuracaoPontoRepository apuracoes,
    IMarcacaoPontoRepository marcacoes,
    IJornadaTrabalhoRepository jornadas,
    IParametrosPontoProvider parametros,
    IServidorPontoConsulta servidores,
    IAssinaturaEmEscopoDedicado assinatura)
    : IQueryHandler<GerarAejQuery, ArtefatoAej>
{
    /// <inheritdoc />
    public async Task<ArtefatoAej> Handle(GerarAejQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(config.CnpjEnte))
        {
            throw new InvalidOperationException("CNPJ do ente nao configurado (RecursosHumanos:Ponto:CnpjEnte).");
        }

        var competencia = Competencia.De(request.Ano, request.Mes);
        var primeiroDia = new DateOnly(request.Ano, request.Mes, 1);
        var apuradas = await apuracoes.ListarPorCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false);

        var linhas = new List<LinhaAej>();
        foreach (var apuracao in apuradas)
        {
            var dados = await servidores.ObterAsync(apuracao.ServidorId, cancellationToken).ConfigureAwait(false);
            if (dados is null)
            {
                continue;
            }

            var jornada = await jornadas.ObterVigenteAsync(apuracao.ServidorId, primeiroDia, cancellationToken).ConfigureAwait(false);
            if (jornada is null)
            {
                continue;
            }

            var doServidor = await marcacoes.ListarPorServidorCompetenciaAsync(apuracao.ServidorId, competencia, cancellationToken).ConfigureAwait(false);
            var instantes = doServidor.Select(m => new InstanteMarcacao(m.DataHora, m.Sentido)).ToList();
            var resultado = TratamentoJornada.Apurar(instantes, jornada);

            foreach (var dia in resultado.Dias)
            {
                linhas.Add(new LinhaAej(
                    dados.Cpf,
                    dia.Dia,
                    dia.MinutosDevidos,
                    dia.MinutosTrabalhados,
                    dia.MinutosExtras,
                    dia.MinutosFalta));
            }
        }

        var ultimoDia = new DateOnly(request.Ano, request.Mes, DateTime.DaysInMonth(request.Ano, request.Mes));
        var cabecalho = new CabecalhoAej(
            Cnpj.Create(config.CnpjEnte),
            config.RazaoSocialEnte ?? string.Empty,
            primeiroDia,
            ultimoDia,
            DateTimeOffset.Now);

        var conteudo = GeradorAej.Gerar(cabecalho, linhas);

        var assinaturaCades = Array.Empty<byte>();
        if (request.Assinar)
        {
            assinaturaCades = await assinatura
                .AssinarCmsAsync(conteudo, new OpcoesAssinaturaCms(DestinoAssinatura.Ponto, Detached: true), cancellationToken)
                .ConfigureAwait(false);
        }

        var nome = $"AEJ_{request.Ano:0000}{request.Mes:00}.txt";
        return new ArtefatoAej(nome, conteudo, assinaturaCades);
    }
}
