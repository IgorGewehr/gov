using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Seed;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;

/// <summary>
/// Semeia o plano de contas PCASP e os roteiros contábeis do tenant (idempotente). Executado na
/// ativação do módulo. [validar-plano-oficial]: o catálogo é o mínimo do PCASP Federação.
/// </summary>
public sealed record SemearPlanoDeContasCommand : ICommand<int>;

/// <summary>Handler do seed do plano de contas e roteiros.</summary>
public sealed class SemearPlanoDeContasHandler(
    IContaContabilRepository contas,
    IEventoContabilRepository eventos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<SemearPlanoDeContasCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(SemearPlanoDeContasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var criadas = 0;
        var porCodigo = new Dictionary<string, ContaContabil>(StringComparer.Ordinal);

        foreach (var def in PlanoDeContasCatalogo.Contas())
        {
            if (await contas.ExisteCodigoAsync(def.Codigo, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var codigo = CodigoContabil.De(def.Codigo);
            var paiCodigo = codigo.CodigoPai();
            ContaContabilId? paiId = paiCodigo is not null && porCodigo.TryGetValue(paiCodigo, out var pai)
                ? pai.Id
                : null;

            var conta = ContaContabil.Criar(
                tenant.TenantId,
                codigo,
                def.Titulo,
                def.Funcao,
                def.Funcionamento,
                def.Tipo,
                paiId,
                def.Indicador,
                def.Encerramento,
                def.NaturezaSaldoOverride);

            contas.Adicionar(conta);
            porCodigo[def.Codigo] = conta;
            criadas++;
        }

        foreach (var def in RoteirosCatalogo.Roteiros())
        {
            if (await eventos.ExisteParaFatoAsync(def.Fato, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var evento = Domain.Contabilidade.EventosContabeis.EventoContabil.Criar(
                tenant.TenantId,
                def.Fato,
                def.Codigo,
                def.Nome,
                exercicioVigenciaInicio: 0,
                exercicioVigenciaFim: null,
                def.Linhas);

            eventos.Adicionar(evento);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return criadas;
    }
}
