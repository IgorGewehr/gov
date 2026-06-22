using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// LG-3: a redacao de PII na trilha de auditoria e DENY-BY-DEFAULT (por convencao/atributo, nao
/// opt-in). CPF/NIS/CNS nunca vao em claro para a trilha; o visualizador mascara o que ainda estiver
/// cru (linhas legadas). Antes, o CPF da folha inteira saia cru de <c>GET /api/admin/auditoria</c>.
/// </summary>
public sealed class Lg3RedacaoAuditoriaPadraoTests : RecursosHumanosTestBase
{
    private const string CpfDigitos = "52998224725"; // 11 digitos, valido.

    [Fact] // Sem opt-in: o CPF do servidor e redigido por CONVENCAO ao gravar a trilha.
    public async Task Cpf_do_servidor_e_redigido_por_padrao_na_trilha()
    {
        await using var contexto = CriarContexto(TenantA);
        var servidor = Servidor.Admitir(
            TenantA,
            Cpf.Create(CpfDigitos),
            Matricula.De("MAT-0001"),
            DadosPessoais.Criar("Maria da Silva", new DateOnly(1990, 3, 20)),
            CargoId.New(),
            RegimePrevidenciario.Rpps,
            new DateOnly(2026, 1, 10));
        contexto.Add(servidor);
        await contexto.SaveChangesAsync();

        var linhas = await contexto.AuditTrail.Where(t => t.EntityName == nameof(Servidor)).ToListAsync();

        linhas.Should().NotBeEmpty();
        // O CPF em claro NUNCA aparece; o marcador de redacao SIM.
        linhas.Select(l => l.NewValues).Should().OnlyContain(v => v == null || !v!.Contains(CpfDigitos));
        linhas.Should().Contain(l => l.NewValues != null && l.NewValues!.Contains("[REDACTED]"));
    }

    [Fact] // Visualizador: mascara CPF cru de uma linha LEGADA (gravada antes de LG-3).
    public void Visualizador_mascara_cpf_cru_de_linha_legada()
    {
        var legado = $"{{\"Matricula\":\"MAT-0001\",\"Cpf\":\"{CpfDigitos}\"}}";

        var mascarado = PoliticaRedacaoAuditoria.MascararJson(legado);

        mascarado.Should().NotBeNull();
        mascarado!.Should().NotContain(CpfDigitos);
        mascarado.Should().Contain("[REDACTED]");
        mascarado.Should().Contain("MAT-0001"); // campo nao-sensivel preservado.
    }

    [Fact] // Visualizador: NIS tambem e mascarado por convencao (CadUnico).
    public void Visualizador_mascara_nis()
    {
        var json = "{\"Nis\":\"12345678919\",\"Territorio\":\"Centro\"}";

        var mascarado = PoliticaRedacaoAuditoria.MascararJson(json);

        mascarado!.Should().NotContain("12345678919");
        mascarado.Should().Contain("Centro");
    }

    [Fact] // Conteudo nao-sensivel passa intacto (sem falso-positivo de mascaramento).
    public void Conteudo_nao_sensivel_passa_intacto()
    {
        var json = "{\"Matricula\":\"MAT-0001\",\"Situacao\":\"Nomeado\"}";

        PoliticaRedacaoAuditoria.MascararJson(json).Should().Be(json);
    }

    [Fact] // LG-3 (residual): CPF aninhado em Value Object OWNED (ex.: Paciente.Identificacao.Cpf.Digitos)
           // tambem e mascarado — a varredura do visualizador e RECURSIVA, nao so no nivel raiz.
    public void Visualizador_mascara_cpf_aninhado_em_value_object_owned()
    {
        var json =
            "{\"Cns\":\"[REDACTED]\",\"Identificacao\":{\"Nome\":\"Joao\",\"Cpf\":{\"Digitos\":\"" +
            CpfDigitos + "\"}},\"Situacao\":1}";

        var mascarado = PoliticaRedacaoAuditoria.MascararJson(json);

        mascarado.Should().NotBeNull();
        mascarado!.Should().NotContain(CpfDigitos); // o CPF aninhado NAO vaza.
        mascarado.Should().Contain("[REDACTED]");
        mascarado.Should().Contain("Joao"); // campo nao-sensivel aninhado preservado.
    }

    [Fact] // PII dentro de itens de ARRAY tambem e mascarada (ex.: lista de membros/dependentes).
    public void Visualizador_mascara_cpf_em_itens_de_array()
    {
        var json = "{\"Membros\":[{\"Cpf\":\"" + CpfDigitos + "\",\"Parentesco\":0}]}";

        var mascarado = PoliticaRedacaoAuditoria.MascararJson(json);

        mascarado!.Should().NotContain(CpfDigitos);
        mascarado.Should().Contain("[REDACTED]");
    }
}
