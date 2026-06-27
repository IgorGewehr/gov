using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

/// <summary>Identificador forte do agregado <see cref="EnquadramentoServidor"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EnquadramentoServidorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EnquadramentoServidorId"/>.</returns>
    public static EnquadramentoServidorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Enquadramento de um servidor num plano de carreira (PCCS): mantem a POSICAO VIGENTE do servidor na
/// matriz (classe + referencia) e o LIVRO-RAZAO de movimentacoes (enquadramento inicial + progressoes
/// horizontais + promocoes verticais), cada uma com a data de efeito, o criterio e o vencimento
/// resultante. A posicao deriva o vencimento via <see cref="PlanoCarreira.VencimentoDa"/>; o efeito no
/// vencimento do cargo e' aplicado pelo caso de uso (cross-aggregate por Id), reusando
/// <c>Cargo.AlterarVencimento</c>. Unico por (TenantId, ServidorId). Raiz de agregado, nasce valida via
/// <see cref="Enquadrar"/>.
/// </summary>
public sealed class EnquadramentoServidor : AggregateRoot<EnquadramentoServidorId>, IMustHaveTenant
{
    private readonly List<MovimentacaoCarreira> _movimentacoes = [];

    private EnquadramentoServidor()
    {
    }

    private EnquadramentoServidor(
        EnquadramentoServidorId id,
        Guid tenantId,
        ServidorId servidorId,
        PlanoCarreiraId planoCarreiraId,
        PosicaoCarreira posicao)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        PlanoCarreiraId = planoCarreiraId;
        ClasseAtual = posicao.Classe;
        ReferenciaAtual = posicao.Referencia;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor enquadrado.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Plano de carreira a que o servidor esta vinculado.</summary>
    public PlanoCarreiraId PlanoCarreiraId { get; private set; }

    /// <summary>Classe (faixa vertical) vigente — persistida para indexar/consultar.</summary>
    public int ClasseAtual { get; private set; }

    /// <summary>Referencia (step horizontal) vigente — persistida para indexar/consultar.</summary>
    public int ReferenciaAtual { get; private set; }

    /// <summary>Posicao vigente (VO de dominio) reconstruida do par persistido (sem coluna propria).</summary>
    public PosicaoCarreira PosicaoAtual => PosicaoCarreira.De(ClasseAtual, ReferenciaAtual);

    /// <summary>Historico de movimentacoes (enquadramento + progressoes/promocoes), exposto somente pela raiz.</summary>
    public IReadOnlyCollection<MovimentacaoCarreira> Movimentacoes => _movimentacoes;

    /// <summary>
    /// Enquadra inicialmente o servidor numa posicao da matriz do plano. Registra a movimentacao de
    /// <see cref="TipoMovimentacaoCarreira.Enquadramento"/> com o vencimento da celula e emite
    /// <see cref="ServidorEnquadrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor a enquadrar.</param>
    /// <param name="plano">Plano de carreira (ativo) destino.</param>
    /// <param name="posicao">Posicao de ingresso (classe + referencia) na grade do plano.</param>
    /// <param name="dataEfeito">Data de efeito do enquadramento.</param>
    /// <param name="fundamento">Fundamento (lei/processo) do enquadramento.</param>
    /// <returns>Novo <see cref="EnquadramentoServidor"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o plano, a posicao ou o fundamento forem nulos.</exception>
    /// <exception cref="ArgumentException">Se o fundamento for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o plano estiver revogado ou a posicao estiver fora da grade.</exception>
    public static EnquadramentoServidor Enquadrar(
        Guid tenantId,
        ServidorId servidorId,
        PlanoCarreira plano,
        PosicaoCarreira posicao,
        DateOnly dataEfeito,
        string fundamento)
    {
        ArgumentNullException.ThrowIfNull(plano);
        ArgumentNullException.ThrowIfNull(posicao);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamento);
        plano.GarantirAtivo();
        if (!plano.ContemPosicao(posicao))
        {
            throw new InvalidOperationException("Posicao de enquadramento fora da grade do plano de carreira.");
        }

        var enquadramento = new EnquadramentoServidor(
            EnquadramentoServidorId.New(),
            tenantId,
            servidorId,
            plano.Id,
            posicao);

        var vencimento = plano.VencimentoDa(posicao);
        enquadramento._movimentacoes.Add(new MovimentacaoCarreira(
            MovimentacaoCarreiraId.New(),
            TipoMovimentacaoCarreira.Enquadramento,
            classeOrigem: null,
            referenciaOrigem: null,
            posicao.Classe,
            posicao.Referencia,
            vencimento.Valor,
            criterio: null,
            dataEfeito,
            fundamento.Trim(),
            portariaId: null));

        enquadramento.RaiseDomainEvent(new ServidorEnquadrado(enquadramento.Id, servidorId, plano.Id, posicao.Classe, posicao.Referencia, vencimento.Valor));
        return enquadramento;
    }

    /// <summary>
    /// Aplica uma PROGRESSAO HORIZONTAL: avanca uma referencia na mesma classe, exigindo que o plano
    /// permita (ha referencia seguinte). Registra a movimentacao e devolve o novo vencimento (para o
    /// caso de uso aplicar ao cargo). Emite <see cref="ProgressaoConcedida"/>.
    /// </summary>
    /// <param name="plano">Plano de carreira do servidor (mesmo do enquadramento).</param>
    /// <param name="criterio">Criterio que fundamenta a progressao (tempo/avaliacao/ambos).</param>
    /// <param name="dataEfeito">Data de efeito da progressao.</param>
    /// <param name="fundamento">Fundamento (processo/justificativa) da progressao.</param>
    /// <param name="portariaId">Portaria que formalizou a progressao (opcional).</param>
    /// <returns>Novo <see cref="Vencimento"/> da posicao resultante.</returns>
    /// <exception cref="ArgumentNullException">Se o plano ou o fundamento forem nulos.</exception>
    /// <exception cref="ArgumentException">Se o fundamento for vazio ou o plano nao corresponder ao enquadramento.</exception>
    /// <exception cref="InvalidOperationException">Se o plano estiver revogado ou nao houver referencia seguinte.</exception>
    public Vencimento ConcederProgressao(
        PlanoCarreira plano,
        CriterioProgressao criterio,
        DateOnly dataEfeito,
        string fundamento,
        Guid? portariaId = null)
    {
        ArgumentNullException.ThrowIfNull(plano);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamento);
        GarantirPlanoCorrespondente(plano);
        plano.GarantirAtivo();

        var atual = PosicaoAtual;
        if (!plano.PermiteProgressao(atual))
        {
            throw new InvalidOperationException("Servidor ja esta na ultima referencia da classe; progressao horizontal indisponivel (cabe promocao).");
        }

        var destino = atual.ProximaReferencia();
        var vencimento = plano.VencimentoDa(destino);
        Registrar(TipoMovimentacaoCarreira.ProgressaoHorizontal, atual, destino, vencimento.Valor, criterio, dataEfeito, fundamento.Trim(), portariaId);
        RaiseDomainEvent(new ProgressaoConcedida(Id, ServidorId, destino.Classe, destino.Referencia, vencimento.Valor));
        return vencimento;
    }

    /// <summary>
    /// Aplica uma PROMOCAO VERTICAL: avanca uma classe (voltando a referencia inicial), exigindo que o
    /// plano permita (ha classe seguinte). Registra a movimentacao e devolve o novo vencimento. Emite
    /// <see cref="PromocaoConcedida"/>.
    /// </summary>
    /// <param name="plano">Plano de carreira do servidor (mesmo do enquadramento).</param>
    /// <param name="dataEfeito">Data de efeito da promocao.</param>
    /// <param name="fundamento">Fundamento (lei/titulacao/processo) da promocao.</param>
    /// <param name="portariaId">Portaria que formalizou a promocao (opcional).</param>
    /// <returns>Novo <see cref="Vencimento"/> da posicao resultante.</returns>
    /// <exception cref="ArgumentNullException">Se o plano ou o fundamento forem nulos.</exception>
    /// <exception cref="ArgumentException">Se o fundamento for vazio ou o plano nao corresponder ao enquadramento.</exception>
    /// <exception cref="InvalidOperationException">Se o plano estiver revogado ou nao houver classe seguinte.</exception>
    public Vencimento ConcederPromocao(
        PlanoCarreira plano,
        DateOnly dataEfeito,
        string fundamento,
        Guid? portariaId = null)
    {
        ArgumentNullException.ThrowIfNull(plano);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamento);
        GarantirPlanoCorrespondente(plano);
        plano.GarantirAtivo();

        var atual = PosicaoAtual;
        if (!plano.PermitePromocao(atual))
        {
            throw new InvalidOperationException("Servidor ja esta na ultima classe da carreira; promocao vertical indisponivel.");
        }

        var destino = atual.ProximaClasse();
        var vencimento = plano.VencimentoDa(destino);
        Registrar(TipoMovimentacaoCarreira.PromocaoVertical, atual, destino, vencimento.Valor, criterio: null, dataEfeito, fundamento.Trim(), portariaId);
        RaiseDomainEvent(new PromocaoConcedida(Id, ServidorId, destino.Classe, destino.Referencia, vencimento.Valor));
        return vencimento;
    }

    private void Registrar(
        TipoMovimentacaoCarreira tipo,
        PosicaoCarreira origem,
        PosicaoCarreira destino,
        decimal vencimentoResultante,
        CriterioProgressao? criterio,
        DateOnly dataEfeito,
        string fundamento,
        Guid? portariaId)
    {
        _movimentacoes.Add(new MovimentacaoCarreira(
            MovimentacaoCarreiraId.New(),
            tipo,
            origem.Classe,
            origem.Referencia,
            destino.Classe,
            destino.Referencia,
            vencimentoResultante,
            criterio,
            dataEfeito,
            fundamento,
            portariaId));

        ClasseAtual = destino.Classe;
        ReferenciaAtual = destino.Referencia;
    }

    private void GarantirPlanoCorrespondente(PlanoCarreira plano)
    {
        if (plano.Id != PlanoCarreiraId)
        {
            throw new ArgumentException("Plano informado nao corresponde ao plano de enquadramento do servidor.", nameof(plano));
        }
    }
}
