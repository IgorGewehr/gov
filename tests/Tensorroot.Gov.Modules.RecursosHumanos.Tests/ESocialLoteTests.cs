using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do <see cref="LoteEventosESocial"/>: limites CONFIRMADOS do MOS v1.15 (50 eventos / 5 MB)
/// e o particionamento de uma fila grande em lotes validos (fecha ao atingir 50 OU ~5 MB).
/// </summary>
public sealed class ESocialLoteTests
{
    private const string Cnpj = "11222333000181";

    private static ItemLoteEvento Item(int i, int bytes = 100)
        => new(EventoESocialId.New(), $"ID{i:D5}", new byte[bytes]);

    [Fact] // Lote valido com poucos eventos.
    public void Montar_lote_valido()
    {
        var itens = Enumerable.Range(1, 3).Select(i => Item(i)).ToList();

        var lote = LoteEventosESocial.Montar(TipoInscricao.Cnpj, Cnpj, AmbienteESocial.ProducaoRestrita, itens);

        lote.Itens.Should().HaveCount(3);
        lote.NrInscEmpregador.Should().Be(Cnpj);
    }

    [Fact] // Montar lote acima de 50 eventos falha (erro 613 conceitual).
    public void Montar_acima_de_50_eventos_lanca()
    {
        var itens = Enumerable.Range(1, 51).Select(i => Item(i)).ToList();

        var acao = () => LoteEventosESocial.Montar(TipoInscricao.Cnpj, Cnpj, AmbienteESocial.ProducaoRestrita, itens);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*50*");
    }

    [Fact] // Montar lote acima de 5 MB falha (erro 612 conceitual).
    public void Montar_acima_de_5mb_lanca()
    {
        var itens = new List<ItemLoteEvento> { Item(1, LoteEventosESocial.MaxBytesPorLote + 1) };

        var acao = () => LoteEventosESocial.Montar(TipoInscricao.Cnpj, Cnpj, AmbienteESocial.ProducaoRestrita, itens);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*612*");
    }

    [Fact] // Particionar 120 eventos -> 3 lotes (50 + 50 + 20).
    public void Particionar_fecha_lote_ao_atingir_50_eventos()
    {
        var fila = Enumerable.Range(1, 120).Select(i => Item(i)).ToList();

        var lotes = LoteEventosESocial.Particionar(TipoInscricao.Cnpj, Cnpj, AmbienteESocial.ProducaoRestrita, fila);

        lotes.Should().HaveCount(3);
        lotes[0].Itens.Should().HaveCount(50);
        lotes[1].Itens.Should().HaveCount(50);
        lotes[2].Itens.Should().HaveCount(20);
        lotes.Should().OnlyContain(l => l.Itens.Count <= LoteEventosESocial.MaxEventosPorLote);
    }

    [Fact] // Particionar fecha lote ao atingir ~5 MB antes de 50 eventos.
    public void Particionar_fecha_lote_ao_atingir_5mb()
    {
        // Cada evento ~2 MB: 3 eventos = 6 MB -> nao cabem juntos; fecha a cada 2.
        var doisMb = 2 * 1024 * 1024;
        var fila = Enumerable.Range(1, 3).Select(i => Item(i, doisMb)).ToList();

        var lotes = LoteEventosESocial.Particionar(TipoInscricao.Cnpj, Cnpj, AmbienteESocial.ProducaoRestrita, fila);

        lotes.Should().HaveCount(2); // (2 MB + 2 MB) | (2 MB)
        lotes.Should().OnlyContain(l => l.TamanhoBytes <= LoteEventosESocial.MaxBytesPorLote);
    }
}
