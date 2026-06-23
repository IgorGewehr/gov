using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

/// <summary>Identificador forte do agregado <see cref="Inventario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct InventarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="InventarioId"/>.</returns>
    public static InventarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Inventário patrimonial (Lei 4.320 art. 96): levantamento periódico (anual obrigatório, ou por setor)
/// que concilia o saldo físico × contábil dos bens. Designa comissão por portaria, congela um snapshot
/// imutável do acervo, coleta a contagem física item-a-item, apura divergências (falta/sobra/localização/
/// estado/valor) e encerra alimentando as recomendações de movimentação/baixa. É fonte de auditoria do
/// levantamento (não muta o <see cref="BemPatrimonial"/> diretamente — agregados distintos).
/// </summary>
public sealed class Inventario : AggregateRoot<InventarioId>, IMustHaveTenant
{
    /// <summary>Mínimo legal usual de membros de uma comissão de inventário (parametrizável por tenant via factory).</summary>
    public const int MinimoMembrosComissaoPadrao = 3;

    private readonly List<MembroComissao> _comissao = [];
    private readonly List<ItemInventario> _itens = [];
    private readonly List<DivergenciaInventario> _divergencias = [];

    private Inventario()
    {
    }

    private Inventario(
        InventarioId id,
        Guid tenantId,
        int exercicio,
        TipoInventario tipo,
        string? setor,
        string portaria,
        DateOnly dataAbertura)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Tipo = tipo;
        Setor = setor;
        Portaria = portaria;
        DataAbertura = dataAbertura;
        Situacao = SituacaoInventario.EmAbertura;
        RaiseDomainEvent(new InventarioAberto(id, exercicio, tipo));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício (ano-base) do levantamento.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Tipo (anual/por setor/eventual/transferência).</summary>
    public TipoInventario Tipo { get; private set; }

    /// <summary>Setor/UO escopo do levantamento; nulo = geral (todo o acervo).</summary>
    public string? Setor { get; private set; }

    /// <summary>Portaria de designação da comissão.</summary>
    public string Portaria { get; private set; } = default!;

    /// <summary>Situação na máquina de estados do levantamento.</summary>
    public SituacaoInventario Situacao { get; private set; }

    /// <summary>Data de abertura.</summary>
    public DateOnly DataAbertura { get; private set; }

    /// <summary>Data de encerramento; nula enquanto não encerrado.</summary>
    public DateOnly? DataEncerramento { get; private set; }

    /// <summary>Membros da comissão designada por portaria.</summary>
    public IReadOnlyCollection<MembroComissao> Comissao => _comissao;

    /// <summary>Linhas do inventário (snapshot contábil + contagem física).</summary>
    public IReadOnlyCollection<ItemInventario> Itens => _itens;

    /// <summary>Divergências apuradas na conciliação.</summary>
    public IReadOnlyCollection<DivergenciaInventario> Divergencias => _divergencias;

    /// <summary>Indica se a conciliação já foi executada ao menos uma vez (estado &gt;= EmConciliacao).</summary>
    public bool Conciliado => Situacao is SituacaoInventario.EmConciliacao or SituacaoInventario.Encerrado;

    /// <summary>
    /// Abre um inventário (situação inicial <see cref="SituacaoInventario.EmAbertura"/>) com a comissão
    /// designada. O mínimo de membros é parametrizável por tenant (default <see cref="MinimoMembrosComissaoPadrao"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício (ano-base).</param>
    /// <param name="tipo">Tipo do inventário.</param>
    /// <param name="setor">Setor/UO escopo (nulo = geral).</param>
    /// <param name="portaria">Portaria de designação da comissão.</param>
    /// <param name="membros">Membros da comissão.</param>
    /// <param name="dataAbertura">Data de abertura.</param>
    /// <param name="minimoMembrosComissao">Mínimo de membros exigido (parametrizável por tenant).</param>
    /// <returns>Novo <see cref="Inventario"/>.</returns>
    /// <exception cref="ArgumentException">Se a portaria for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercício/mínimo de membros for inválido.</exception>
    /// <exception cref="InvalidOperationException">Se a comissão tiver menos que o mínimo de membros.</exception>
    public static Inventario Abrir(
        Guid tenantId,
        int exercicio,
        TipoInventario tipo,
        string? setor,
        string portaria,
        IEnumerable<MembroComissao> membros,
        DateOnly dataAbertura,
        int minimoMembrosComissao = MinimoMembrosComissaoPadrao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portaria);
        ArgumentNullException.ThrowIfNull(membros);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exercicio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimoMembrosComissao);

        var listaMembros = membros.ToList();
        // I-INV1: comissão com pelo menos o mínimo de membros (parametrizável; fail-closed no factory).
        if (listaMembros.Count < minimoMembrosComissao)
        {
            throw new InvalidOperationException(
                $"A comissão de inventário exige ao menos {minimoMembrosComissao} membros. Informados: {listaMembros.Count}.");
        }

        var inventario = new Inventario(InventarioId.New(), tenantId, exercicio, tipo, setor, portaria, dataAbertura);
        inventario._comissao.AddRange(listaMembros);
        return inventario;
    }

    /// <summary>
    /// Congela o snapshot contábil esperado do acervo (filtrado por setor a montante) e avança para
    /// <see cref="SituacaoInventario.EmContagem"/>. O snapshot é imutável dentro do inventário (evita drift).
    /// </summary>
    /// <param name="bens">Bens do acervo a congelar.</param>
    /// <exception cref="InvalidOperationException">Se não estiver em abertura ou o acervo estiver vazio.</exception>
    public void CarregarSnapshotContabil(IEnumerable<SnapshotBem> bens)
    {
        ArgumentNullException.ThrowIfNull(bens);
        if (Situacao != SituacaoInventario.EmAbertura)
        {
            throw new InvalidOperationException(
                $"O snapshot contábil só pode ser carregado em abertura. Situação atual: {Situacao}.");
        }

        var lista = bens.ToList();
        // I-INV2: snapshot só pode ser congelado uma vez (não recarrega após congelado).
        if (_itens.Count > 0)
        {
            throw new InvalidOperationException("O snapshot contábil já foi congelado para este inventário.");
        }

        if (lista.Count == 0)
        {
            throw new InvalidOperationException("O snapshot contábil não pode ser vazio.");
        }

        foreach (var bem in lista)
        {
            _itens.Add(ItemInventario.DoSnapshot(
                bem.BemPatrimonialId,
                bem.NumeroTombamento,
                bem.Descricao,
                bem.LocalizacaoEsperada,
                bem.ValorContabil));
        }

        Situacao = SituacaoInventario.EmContagem;
    }

    /// <summary>Registra a contagem física de um item do snapshot (físico real).</summary>
    /// <param name="bemPatrimonialId">Bem contado (deve constar do snapshot).</param>
    /// <param name="situacaoEncontrada">Situação física apurada.</param>
    /// <param name="localizacaoEncontrada">Localização encontrada (opcional).</param>
    /// <param name="observacao">Observação livre (opcional).</param>
    /// <exception cref="InvalidOperationException">Se não estiver em contagem ou o bem não constar do snapshot.</exception>
    public void RegistrarContagem(
        BemPatrimonialId bemPatrimonialId,
        SituacaoEncontrada situacaoEncontrada,
        string? localizacaoEncontrada,
        string? observacao)
    {
        GarantirEmContagem();
        var item = _itens.FirstOrDefault(i => i.BemPatrimonialId == bemPatrimonialId)
            ?? throw new InvalidOperationException("Item não consta do snapshot contábil deste inventário; use RegistrarBemNaoCadastrado para sobras.");

        item.RegistrarContagem(situacaoEncontrada, localizacaoEncontrada, observacao);
    }

    /// <summary>
    /// Registra um bem físico encontrado sem tombo/registro contábil ("sobra"/achado), gerando de imediato
    /// uma divergência do tipo <see cref="TipoDivergencia.Sobra"/> com recomendação de incorporação.
    /// </summary>
    /// <param name="descricao">Descrição do achado.</param>
    /// <param name="localizacao">Localização onde foi encontrado.</param>
    /// <param name="valorEstimado">Valor estimado do achado.</param>
    /// <exception cref="InvalidOperationException">Se não estiver em contagem.</exception>
    public void RegistrarBemNaoCadastrado(string descricao, string localizacao, decimal valorEstimado)
    {
        GarantirEmContagem();
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(localizacao);
        _ = ValorMonetario.De(valorEstimado);

        _divergencias.Add(DivergenciaInventario.Registrar(
            TipoDivergencia.Sobra,
            null,
            $"Sobra (sem tombo): {descricao} — localização: {localizacao}; valor estimado: {valorEstimado:0.00}.",
            RecomendacaoDivergencia.Incorporacao));
    }

    /// <summary>
    /// Concilia físico × contábil: cruza o snapshot com a contagem e (re)gera as divergências, avançando
    /// para <see cref="SituacaoInventario.EmConciliacao"/>. Idempotente: descarta as divergências de itens
    /// do snapshot e reapura, preservando as sobras (achados sem tombo).
    /// </summary>
    /// <returns>As divergências apuradas (somente leitura).</returns>
    /// <exception cref="InvalidOperationException">Se não estiver em contagem ou já em conciliação.</exception>
    public IReadOnlyCollection<DivergenciaInventario> Conciliar()
    {
        if (Situacao is not (SituacaoInventario.EmContagem or SituacaoInventario.EmConciliacao))
        {
            throw new InvalidOperationException(
                $"A conciliação exige inventário em contagem/conciliação. Situação atual: {Situacao}.");
        }

        // Reapuração idempotente: mantém só as sobras (já registradas na contagem) e recalcula o restante.
        _divergencias.RemoveAll(d => d.Tipo != TipoDivergencia.Sobra);

        foreach (var item in _itens)
        {
            if (!item.Contado || item.SituacaoEncontrada == SituacaoEncontrada.NaoLocalizado)
            {
                // Falta: no snapshot e não contado (ou contado como não localizado).
                _divergencias.Add(DivergenciaInventario.Registrar(
                    TipoDivergencia.Falta,
                    item.BemPatrimonialId,
                    $"Falta: bem {item.NumeroTombamento ?? item.BemPatrimonialId.ToString()} ({item.DescricaoSnapshot}) não localizado.",
                    RecomendacaoDivergencia.Baixa));
                continue;
            }

            if (item.SituacaoEncontrada == SituacaoEncontrada.LocalizadoOutroSetor
                || (!string.IsNullOrWhiteSpace(item.LocalizacaoEncontrada)
                    && !string.IsNullOrWhiteSpace(item.LocalizacaoEsperada)
                    && !string.Equals(item.LocalizacaoEncontrada, item.LocalizacaoEsperada, StringComparison.OrdinalIgnoreCase)))
            {
                _divergencias.Add(DivergenciaInventario.Registrar(
                    TipoDivergencia.DivergenciaLocalizacao,
                    item.BemPatrimonialId,
                    $"Localização divergente: bem {item.NumeroTombamento ?? item.BemPatrimonialId.ToString()} esperado em '{item.LocalizacaoEsperada}', encontrado em '{item.LocalizacaoEncontrada}'.",
                    RecomendacaoDivergencia.Transferencia));
                continue;
            }

            if (item.SituacaoEncontrada == SituacaoEncontrada.Inservivel)
            {
                _divergencias.Add(DivergenciaInventario.Registrar(
                    TipoDivergencia.DivergenciaEstado,
                    item.BemPatrimonialId,
                    $"Estado divergente: bem {item.NumeroTombamento ?? item.BemPatrimonialId.ToString()} ({item.DescricaoSnapshot}) inservível.",
                    RecomendacaoDivergencia.Reavaliacao));
            }
        }

        Situacao = SituacaoInventario.EmConciliacao;
        return _divergencias;
    }

    /// <summary>Encerra o inventário (terminal) — exige conciliação prévia; emite <see cref="InventarioEncerrado"/>.</summary>
    /// <param name="data">Data de encerramento.</param>
    /// <exception cref="InvalidOperationException">Se a conciliação não tiver sido executada.</exception>
    public void Encerrar(DateOnly data)
    {
        // I-INV3: encerrar exige conciliação feita.
        if (Situacao != SituacaoInventario.EmConciliacao)
        {
            throw new InvalidOperationException(
                $"O encerramento exige conciliação prévia (EmConciliacao). Situação atual: {Situacao}.");
        }

        Situacao = SituacaoInventario.Encerrado;
        DataEncerramento = data;
        RaiseDomainEvent(new InventarioEncerrado(Id, Exercicio, _divergencias.Count));
    }

    /// <summary>Cancela o inventário (terminal) sem efeito patrimonial.</summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se já estiver encerrado/cancelado.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoInventario.Encerrado or SituacaoInventario.Cancelado)
        {
            throw new InvalidOperationException($"Inventário terminal não admite cancelamento. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoInventario.Cancelado;
    }

    private void GarantirEmContagem()
    {
        if (Situacao != SituacaoInventario.EmContagem)
        {
            throw new InvalidOperationException(
                $"A operação exige inventário em contagem (snapshot congelado). Situação atual: {Situacao}.");
        }
    }
}
