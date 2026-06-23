using FluentAssertions;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// LG-A3: os verbos <c>*.ver</c> eram grossos demais — a mesma claim liberava a listagem minimizada
/// E o conteudo sigiloso (prontuario com violacao contra menor, historico clinico, NIS/renda do
/// CadUnico). A correcao introduz verbos FINOS de leitura sensivel
/// (<c>saude.prontuario.ler</c>, <c>assistenciasocial.prontuario.ler</c>) SEPARADOS de
/// <c>saude.ver</c>/<c>assistenciasocial.ver</c>: ver o agregado nao expoe o conteudo sensivel sem o
/// verbo fino. Os verbos finos pertencem a <c>Todas</c> (admin de tenant mantem o acesso), mas sao
/// constantes distintas das de visualizacao minimizada.
/// </summary>
public sealed class LgA3VerbosFinosLeituraSensivelTests
{
    [Fact] // Os verbos finos sao constantes distintas e estaveis (valores de claim).
    public void Verbos_finos_de_leitura_sensivel_existem_e_sao_distintos_do_ver()
    {
        DomainPermissoes.SaudeProntuarioLer.Should().Be("saude.prontuario.ler");
        DomainPermissoes.AssistenciaSocialProntuarioLer.Should().Be("assistenciasocial.prontuario.ler");

        DomainPermissoes.SaudeProntuarioLer.Should().NotBe(DomainPermissoes.SaudeVer);
        DomainPermissoes.AssistenciaSocialProntuarioLer.Should().NotBe(DomainPermissoes.AssistenciaSocialVer);
    }

    [Fact] // O verbo de visualizacao minimizada NAO e, por si, o verbo de leitura do conteudo sensivel.
    public void Ver_e_ProntuarioLer_sao_escopos_separados()
    {
        // Granularidade fina: quem so tem "saude.ver" nao tem "saude.prontuario.ler".
        DomainPermissoes.SaudeVer.Should().NotBe(DomainPermissoes.SaudeProntuarioLer);
        DomainPermissoes.AssistenciaSocialVer.Should().NotBe(DomainPermissoes.AssistenciaSocialProntuarioLer);
    }

    [Fact] // Os verbos finos pertencem a "Todas" — o admin de tenant continua com acesso pleno.
    public void Verbos_finos_pertencem_a_Todas()
    {
        DomainPermissoes.Todas.Should().Contain(DomainPermissoes.SaudeProntuarioLer);
        DomainPermissoes.Todas.Should().Contain(DomainPermissoes.AssistenciaSocialProntuarioLer);
    }

    [Fact] // Sao reconhecidos pelo catalogo (deny-by-default: so escopos conhecidos sao persistiveis).
    public void Verbos_finos_sao_conhecidos_pelo_catalogo()
    {
        DomainPermissoes.EhConhecida(DomainPermissoes.SaudeProntuarioLer).Should().BeTrue();
        DomainPermissoes.EhConhecida(DomainPermissoes.AssistenciaSocialProntuarioLer).Should().BeTrue();
    }
}
