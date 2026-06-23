using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura da maquina de estados do <see cref="EventoESocial"/> (Gerado -> Assinado -> Transmitido ->
/// Processado/Rejeitado), idempotencia das transicoes e rejeicao local (XSD) sem gastar cota.
/// </summary>
public sealed class ESocialEventoESocialTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly byte[] XmlGerado = Encoding.UTF8.GetBytes("<eSocial><evtX Id=\"ID1\"/></eSocial>");
    private static readonly DateTimeOffset Agora = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);

    private static EventoESocial NovoEvento(TipoEventoESocial tipo = TipoEventoESocial.S1000Empregador)
        => EventoESocial.Gerar(
            Tenant,
            tipo,
            ChaveIdempotenciaEvento.Criar(tipo, "11222333000181"),
            "ID1234567890",
            AmbienteESocial.ProducaoRestrita,
            XmlGerado,
            Agora);

    [Fact] // Estado inicial Gerado + hash do XML + evento de dominio.
    public void Gerar_nasce_em_gerado_com_hash_e_evento()
    {
        var evento = NovoEvento();

        evento.Estado.Should().Be(EstadoEventoESocial.Gerado);
        evento.HashXmlGerado.Should().HaveLength(64);
        evento.DomainEvents.OfType<EventoESocialGerado>().Should().ContainSingle();
    }

    [Fact] // Transicao Gerado -> Assinado.
    public void Assinar_transita_para_assinado()
    {
        var evento = NovoEvento();
        var assinado = Encoding.UTF8.GetBytes("<eSocial>assinado</eSocial>");

        evento.RegistrarAssinatura(assinado, "THUMB", Agora);

        evento.Estado.Should().Be(EstadoEventoESocial.Assinado);
        evento.XmlAssinado.Should().Equal(assinado);
        evento.ThumbprintCertificado.Should().Be("THUMB");
        evento.DomainEvents.OfType<EventoESocialAssinado>().Should().ContainSingle();
    }

    [Fact] // Idempotencia: re-assinar e no-op (nao lanca, mantem estado).
    public void Assinar_e_idempotente()
    {
        var evento = NovoEvento();
        var assinado = Encoding.UTF8.GetBytes("<eSocial>assinado</eSocial>");
        evento.RegistrarAssinatura(assinado, "THUMB", Agora);

        var acao = () => evento.RegistrarAssinatura(Encoding.UTF8.GetBytes("<eSocial>outro</eSocial>"), "OUTRO", Agora);

        acao.Should().NotThrow();
        evento.ThumbprintCertificado.Should().Be("THUMB"); // mantem a primeira.
    }

    [Fact] // Fluxo completo: Assinado -> Transmitido -> Processado.
    public void Fluxo_completo_ate_processado()
    {
        var evento = NovoEvento();
        evento.RegistrarAssinatura(Encoding.UTF8.GetBytes("<x/>"), "THUMB", Agora);
        evento.RegistrarTransmissao("PROTO-1", Agora);
        evento.Estado.Should().Be(EstadoEventoESocial.Transmitido);
        evento.ProtocoloLote.Should().Be("PROTO-1");

        evento.RegistrarRecibo("REC-1", Agora);

        evento.Estado.Should().Be(EstadoEventoESocial.Processado);
        evento.NumeroRecibo.Should().Be("REC-1");
        evento.EhTerminalSucesso.Should().BeTrue();
        evento.DomainEvents.OfType<EventoESocialProcessado>().Should().ContainSingle();
    }

    [Fact] // Transmissao exige estar Assinado.
    public void Transmitir_sem_assinar_lanca()
    {
        var evento = NovoEvento();

        var acao = () => evento.RegistrarTransmissao("PROTO", Agora);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // Rejeicao local (XSD) antes de transmitir: nao gasta cota; estado RejeitadoLocal.
    public void Rejeitar_localmente_marca_estado_e_emite_evento()
    {
        var evento = NovoEvento();

        evento.RejeitarLocalmente("XSD", "Elemento obrigatorio ausente.");

        evento.Estado.Should().Be(EstadoEventoESocial.RejeitadoLocal);
        evento.DescricaoErro.Should().Contain("ausente");
        evento.DomainEvents.OfType<EventoESocialRejeitado>().Single().Localizado.Should().BeTrue();
    }

    [Fact] // Rejeicao pelo eSocial: Transmitido -> Rejeitado (re-gerável).
    public void Rejeitar_pelo_esocial_apos_transmitido()
    {
        var evento = NovoEvento();
        evento.RegistrarAssinatura(Encoding.UTF8.GetBytes("<x/>"), "THUMB", Agora);
        evento.RegistrarTransmissao("PROTO", Agora);

        evento.RegistrarRejeicao("301", "Erro de schema.", Agora);

        evento.Estado.Should().Be(EstadoEventoESocial.Rejeitado);
        evento.CodigoErro.Should().Be("301");
        evento.DomainEvents.OfType<EventoESocialRejeitado>().Single().Localizado.Should().BeFalse();
    }

    [Fact] // Recibo so a partir de Transmitido.
    public void Recibo_sem_transmitir_lanca()
    {
        var evento = NovoEvento();

        var acao = () => evento.RegistrarRecibo("REC", Agora);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // Gerar com XML vazio falha (fail-closed).
    public void Gerar_com_xml_vazio_lanca()
    {
        var acao = () => EventoESocial.Gerar(
            Tenant,
            TipoEventoESocial.S1000Empregador,
            ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1000Empregador, "X"),
            "ID1",
            AmbienteESocial.ProducaoRestrita,
            [],
            Agora);

        acao.Should().Throw<ArgumentException>();
    }
}
