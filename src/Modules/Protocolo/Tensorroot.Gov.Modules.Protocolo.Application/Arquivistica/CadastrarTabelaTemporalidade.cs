using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Arquivistica;

/// <summary>Item (regra de temporalidade) de uma TTD a cadastrar.</summary>
/// <param name="CodigoClassificacao">Codigo da classe a que a regra se aplica.</param>
/// <param name="PrazoGuardaCorrenteAnos">Prazo da fase corrente (anos).</param>
/// <param name="PrazoGuardaIntermediariaAnos">Prazo da fase intermediaria (anos).</param>
/// <param name="Destinacao">Destinacao final (eliminacao/guarda permanente).</param>
/// <param name="EventoContagem">Evento base da contagem.</param>
/// <param name="Observacao">Norma-fonte (auditabilidade ao TCE).</param>
public sealed record ItemTabelaTemporalidade(
    string CodigoClassificacao,
    int PrazoGuardaCorrenteAnos,
    int PrazoGuardaIntermediariaAnos,
    Destinacao Destinacao,
    EventoContagem EventoContagem,
    string? Observacao);

/// <summary>
/// Cadastra a Tabela de Temporalidade e Destinacao (TTD) do tenant (Peca 2 / W9.4). I-T6: prazos e
/// destinacao vem SEMPRE daqui (zero hardcode).
/// </summary>
/// <param name="Nome">Nome da TTD.</param>
/// <param name="Regras">Regras de temporalidade.</param>
public sealed record CadastrarTabelaTemporalidadeCommand(
    string Nome,
    IReadOnlyList<ItemTabelaTemporalidade> Regras) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de TTD.</summary>
public sealed class CadastrarTabelaTemporalidadeValidator : AbstractValidator<CadastrarTabelaTemporalidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarTabelaTemporalidadeValidator()
    {
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Regras).NotEmpty();
        RuleForEach(comando => comando.Regras).ChildRules(regra =>
        {
            regra.RuleFor(item => item.CodigoClassificacao).NotEmpty().MaximumLength(ClasseDocumental.ComprimentoMaximoCodigo);
            regra.RuleFor(item => item.PrazoGuardaCorrenteAnos).GreaterThanOrEqualTo(0);
            regra.RuleFor(item => item.PrazoGuardaIntermediariaAnos).GreaterThanOrEqualTo(0);
            regra.RuleFor(item => item.Destinacao).IsInEnum();
            regra.RuleFor(item => item.EventoContagem).IsInEnum();
        });
    }
}

/// <summary>Handler do cadastro de TTD.</summary>
public sealed class CadastrarTabelaTemporalidadeHandler(
    ITabelaTemporalidadeRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarTabelaTemporalidadeCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarTabelaTemporalidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tabela = TabelaTemporalidade.Criar(tenant.TenantId, request.Nome);
        foreach (var item in request.Regras)
        {
            tabela.AdicionarRegra(
                item.CodigoClassificacao,
                item.PrazoGuardaCorrenteAnos,
                item.PrazoGuardaIntermediariaAnos,
                item.Destinacao,
                item.EventoContagem,
                item.Observacao);
        }

        tabelas.Adicionar(tabela);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tabela.Id.Value;
    }
}
