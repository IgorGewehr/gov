using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Fecha a competencia (so a partir de Calculada — I-7), disparando S-1299/S-1210/totalizadores/DCTFWeb (I-8).</summary>
/// <param name="FolhaDePagamentoId">Folha a fechar.</param>
public sealed record FecharFolhaCommand(Guid FolhaDePagamentoId) : ICommand;

/// <summary>Regras de validacao do fechamento de folha.</summary>
public sealed class FecharFolhaValidator : AbstractValidator<FecharFolhaCommand>
{
    /// <summary>Define as regras.</summary>
    public FecharFolhaValidator()
    {
        RuleFor(comando => comando.FolhaDePagamentoId)
            .NotEmpty()
            .WithMessage("Folha e obrigatoria.");
    }
}

/// <summary>
/// Handler do fechamento de folha. Publica o evento de integracao
/// <see cref="FolhaFechadaIntegrationEvent"/> (Contabilidade/Empenho — despesa de pessoal — I-8) e,
/// para a prestacao de contas ao TCE-RS (Res. 1099/2018), o resumo
/// <see cref="FolhaResumoRemessaTceIntegrationEvent"/> consumido pela Transparencia (ponte RH ->
/// Transparencia, espelhando a MSC publicada por Financas).
/// </summary>
public sealed class FecharFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRepository servidores,
    ICargoRepository cargos,
    IRubricaFolhaRepository rubricas,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<FecharFolhaCommand>
{
    /// <inheritdoc />
    public async Task Handle(FecharFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;

        // I-7: o agregado garante que so fecha a partir de Calculada.
        folha.Fechar(DateOnly.FromDateTime(agoraUtc));

        // Ponte RH -> Transparencia: snapshot da folha para a REMESSA TCE-RS (Res. 1099 / SIAPC Vol. V),
        // enfileirado no Outbox na MESMA transacao do fechamento (consistencia transacional — como a MSC
        // de Financas faz). O OutboxPublisher despacha ao consumidor (Transparencia) ao drenar.
        var resumo = await MontarResumoRemessaTceAsync(folha, agoraUtc, cancellationToken).ConfigureAwait(false);
        integrationEvents.Enfileirar(resumo);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new FolhaFechadaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            folha.TenantId,
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.TotalLiquido.Valor,
            // Roteia o empenho por tipo (despesa de pessoal: 13o/ferias/rescisao tem elemento proprio — design §2.4/§4.4).
            folha.Tipo.ToString());

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        // Ponte RH -> Educacao (M7 E-2): expoe a remuneracao dos profissionais da educacao do exercicio
        // para a afericao do piso de 70% do FUNDEB (EC 108/2020). Publicado por exercicio (ano da
        // competencia). // TODO(validar-oficial): o ROL exato de "profissionais da educacao basica"
        // custeados com FUNDEB (divergencia TCE/CNM) deve filtrar a folha por cargo/lotacao/fonte; ate
        // o classificador do magisterio existir, o municipio pode informar o total como parametro na
        // Educacao (RegistrarRemuneracaoMagisterio) — o evento e a superficie de contrato do cruzamento.
        var remuneracaoMagisterio = new RemuneracaoMagisterioApuradaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            folha.TenantId,
            folha.Competencia.Ano,
            folha.TotalLiquido.Valor);

        await publisher.Publish(remuneracaoMagisterio, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FolhaResumoRemessaTceIntegrationEvent> MontarResumoRemessaTceAsync(
        FolhaDePagamento folha,
        DateTime agoraUtc,
        CancellationToken cancellationToken)
    {
        // Servidores presentes na folha (cadastro TCE_4820), por ServidorId dos eventos.
        var servidorIds = folha.Eventos.Select(evento => evento.ServidorId).Distinct().ToList();
        var servidoresDto = new List<ServidorFolhaTceDto>(servidorIds.Count);
        var codigoRegistroPorServidor = new Dictionary<Guid, string>(servidorIds.Count);

        foreach (var servidorId in servidorIds)
        {
            var servidor = await servidores.ObterPorIdAsync(new ServidorId(servidorId), cancellationToken).ConfigureAwait(false);
            if (servidor is null)
            {
                continue;
            }

            // Codigo de registro do funcionario (FK TCE_4810->TCE_4820): codificacao propria estavel por vinculo.
            // TODO(validar-leiaute-folha-1099): formato/origem oficial do "Codigo de registro do Funcionario" (12).
            var codigoRegistro = servidor.Matricula.Valor;
            codigoRegistroPorServidor[servidorId] = codigoRegistro;

            var cargo = await cargos.ObterPorIdAsync(servidor.CargoId, cancellationToken).ConfigureAwait(false);

            servidoresDto.Add(new ServidorFolhaTceDto(
                codigoRegistro,
                servidor.Cpf.Digitos,
                servidor.DadosPessoais.Nome,
                servidor.Matricula.Valor,
                servidor.DadosPessoais.DataNascimento,
                servidor.DataExercicio ?? servidor.DataPosse ?? servidor.DataNomeacao,
                servidor.DataDesligamento,
                servidor.CargoId.Value.ToString(),
                cargo?.Denominacao ?? string.Empty,
                servidor.Regime.ToString()));
        }

        // Rubricas usadas na folha (TCE_4960), enriquecidas com o catalogo vigente quando existir.
        var rubricasDto = new List<RubricaFolhaTceDto>();
        var rubricasVistas = new HashSet<string>(StringComparer.Ordinal);
        var lancamentos = new List<LancamentoFolhaTceDto>();

        foreach (var evento in folha.Eventos)
        {
            if (!codigoRegistroPorServidor.TryGetValue(evento.ServidorId, out var codigoRegistro))
            {
                continue;
            }

            var operacao = evento.Tipo == TipoEvento.Provento ? "V" : "D";

            lancamentos.Add(new LancamentoFolhaTceDto(
                codigoRegistro,
                evento.Rubrica.Codigo,
                operacao,
                evento.Valor));

            if (!rubricasVistas.Add(evento.Rubrica.Codigo))
            {
                continue;
            }

            var rubricaCatalogo = await rubricas
                .ObterVigentePorCodigoAsync(evento.Rubrica, folha.Competencia, cancellationToken)
                .ConfigureAwait(false);

            rubricasDto.Add(new RubricaFolhaTceDto(
                evento.Rubrica.Codigo,
                rubricaCatalogo?.Descricao ?? evento.Rubrica.Codigo,
                operacao,
                rubricaCatalogo?.IncideIrrf ?? false,
                rubricaCatalogo?.IncideRpps ?? false,
                rubricaCatalogo?.IncideInss ?? false,
                // TODO(validar-leiaute-folha-1099): base legal (150) e conta do Plano de Contas da Folha (cod. TCE, 6).
                string.Empty,
                string.Empty));
        }

        return new FolhaResumoRemessaTceIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            folha.TenantId,
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.Tipo.ToString(),
            folha.DataPagamento,
            servidoresDto,
            rubricasDto,
            lancamentos);
    }
}
