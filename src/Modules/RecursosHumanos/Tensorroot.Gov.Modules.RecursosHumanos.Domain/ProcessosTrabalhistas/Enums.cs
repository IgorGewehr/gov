namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

/// <summary>Situacao (ciclo de vida processual) de um processo trabalhista.</summary>
public enum SituacaoProcessoTrabalhista
{
    /// <summary>Processo ativo/em tramitacao (estado inicial).</summary>
    EmAndamento = 1,

    /// <summary>Acordo homologado entre as partes (encerra com valor de acordo).</summary>
    Acordo = 2,

    /// <summary>Transitado em julgado com condenacao do ente (encerra com valor de condenacao).</summary>
    Condenado = 3,

    /// <summary>Transitado em julgado com improcedencia/extincao sem condenacao do ente.</summary>
    Improcedente = 4,

    /// <summary>Arquivado definitivamente (estado terminal administrativo).</summary>
    Arquivado = 5,
}

/// <summary>
/// Prognostico de perda do processo (probabilidade de saida de recursos), na classificacao do CPC 25
/// (R1)/NBC TG 25: PROVAVEL exige PROVISAO contabil (passivo reconhecido); POSSIVEL e' divulgado como
/// passivo contingente (nota explicativa, sem reconhecimento); REMOTA nao se divulga nem provisiona.
/// </summary>
public enum PrognosticoPerda
{
    /// <summary>Perda provavel — reconhece-se PROVISAO no passivo (NBC TG 25).</summary>
    Provavel = 1,

    /// <summary>Perda possivel — passivo contingente divulgado em nota; sem provisao.</summary>
    Possivel = 2,

    /// <summary>Perda remota — sem provisao e sem divulgacao.</summary>
    Remota = 3,
}
