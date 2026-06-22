namespace Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

/// <summary>
/// Natureza administrativa de uma <see cref="UnidadeOrganizacional"/> dentro da arvore do ente.
/// </summary>
/// <remarks>
/// TODO(a confirmar): aderencia ao conceito de Unidade Gestora/Orcamentaria do PCASP/MSC e ao
/// cadastro de UG do TCE-RS (MODELO §2.1 e §11.1). A estrutura real do piloto e definida por lei
/// municipal e e PARAMETRIZAVEL por tenant — nunca hard-coded.
/// </remarks>
public enum TipoUnidade
{
    /// <summary>Secretaria (ex.: Secretaria Municipal de Saude) — tipicamente raiz ou alto nivel.</summary>
    Secretaria = 0,

    /// <summary>Departamento subordinado a uma Secretaria.</summary>
    Departamento = 1,

    /// <summary>Setor subordinado a um Departamento.</summary>
    Setor = 2,

    /// <summary>Gabinete (ex.: Gabinete do Prefeito).</summary>
    Gabinete = 3,

    /// <summary>Fundo (ex.: Fundo Municipal de Saude) — frequentemente vinculado a uma UG contabil.</summary>
    Fundo = 4,
}
