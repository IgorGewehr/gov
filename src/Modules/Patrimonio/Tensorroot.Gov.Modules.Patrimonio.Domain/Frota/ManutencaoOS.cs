using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte de uma <see cref="ManutencaoOS"/> (ordem de serviço).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ManutencaoOsId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ManutencaoOsId"/>.</returns>
    public static ManutencaoOsId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Ordem de serviço de manutenção de um veículo/equipamento da frota:
/// abertura, custo estimado/realizado e conclusão.
/// </summary>
public sealed class ManutencaoOS : Entity<ManutencaoOsId>
{
    private ManutencaoOS()
    {
    }

    private ManutencaoOS(
        ManutencaoOsId id,
        string descricao,
        ValorMonetario custoEstimado,
        Odometro odometro)
        : base(id)
    {
        Descricao = descricao;
        CustoEstimado = custoEstimado;
        Odometro = odometro;
        Situacao = SituacaoOrdemServico.Aberta;
    }

    /// <summary>Descrição do serviço.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Custo estimado na abertura.</summary>
    public ValorMonetario CustoEstimado { get; private set; } = default!;

    /// <summary>Custo realizado na conclusão, quando concluída.</summary>
    public ValorMonetario? CustoRealizado { get; private set; }

    /// <summary>Leitura do odômetro na abertura.</summary>
    public Odometro Odometro { get; private set; }

    /// <summary>Data de conclusão, quando concluída.</summary>
    public DateOnly? DataConclusao { get; private set; }

    /// <summary>Situação atual da ordem de serviço.</summary>
    public SituacaoOrdemServico Situacao { get; private set; }

    /// <summary>Abre uma nova ordem de serviço de manutenção em situação <see cref="SituacaoOrdemServico.Aberta"/>.</summary>
    /// <param name="descricao">Descrição do serviço.</param>
    /// <param name="custoEstimado">Custo estimado.</param>
    /// <param name="odometro">Leitura do odômetro na abertura.</param>
    /// <returns>Nova <see cref="ManutencaoOS"/>.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    internal static ManutencaoOS Abrir(string descricao, ValorMonetario custoEstimado, Odometro odometro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(custoEstimado);
        return new ManutencaoOS(ManutencaoOsId.New(), descricao, custoEstimado, odometro);
    }

    /// <summary>Conclui a ordem de serviço, registrando o custo realizado (I-7).</summary>
    /// <param name="custoRealizado">Custo realizado.</param>
    /// <param name="dataConclusao">Data de conclusão.</param>
    /// <exception cref="InvalidOperationException">Se a OS não estiver em situação <see cref="SituacaoOrdemServico.Aberta"/>.</exception>
    internal void Concluir(ValorMonetario custoRealizado, DateOnly dataConclusao)
    {
        ArgumentNullException.ThrowIfNull(custoRealizado);
        if (Situacao != SituacaoOrdemServico.Aberta)
        {
            throw new InvalidOperationException(
                $"A conclusão exige ordem de serviço em situação 'Aberta'. Situação atual: {Situacao}.");
        }

        CustoRealizado = custoRealizado;
        DataConclusao = dataConclusao;
        Situacao = SituacaoOrdemServico.Concluida;
    }
}
