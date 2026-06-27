using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

/// <summary>Identificador forte da entidade <see cref="AtoAdmissaoSicap"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AtoAdmissaoSicapId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AtoAdmissaoSicapId"/>.</returns>
    public static AtoAdmissaoSicapId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Ato de admissao de pessoal de uma remessa SICAP-AP/SIAPES (linha do "corpo" do arquivo de
/// importacao — leiaute estadual 57 posicoes do TCE-RS). Carrega o identificador unico do ato no
/// sistema (IDENTIFICADOR_ATO), o tipo/titulo (CD_TIPO_ATO), o regime juridico, os dados da pessoa
/// (CPF/nome/nascimento), a descricao do cargo e carga horaria, as datas (ato/historica/termino) e, se
/// extinto, o motivo. Entidade filha — criada apenas pela raiz <see cref="RemessaSicapPessoal"/>.
/// </summary>
public sealed class AtoAdmissaoSicap : Entity<AtoAdmissaoSicapId>
{
    /// <summary>Comprimento maximo do identificador unico do ato (IDENTIFICADOR_ATO — 50 posicoes).</summary>
    public const int ComprimentoMaximoIdentificador = 50;

    /// <summary>Comprimento maximo da descricao do cargo (DS_CARGO — 70 posicoes).</summary>
    public const int ComprimentoMaximoCargo = 70;

    private AtoAdmissaoSicap()
    {
    }

    internal AtoAdmissaoSicap(
        AtoAdmissaoSicapId id,
        ServidorId? servidorId,
        string identificadorAto,
        TipoAtoAdmissao tipoAto,
        RegimeJuridicoSiapes regime,
        string cpf,
        string nome,
        DateOnly dataNascimento,
        string descricaoCargo,
        int cargaHorariaSemanal,
        int? classificacaoConcurso,
        DateOnly dataAto,
        DateOnly? dataHistorica,
        DateOnly? dataTermino,
        MotivoExtincaoVinculo? motivoExtincao)
        : base(id)
    {
        ServidorId = servidorId;
        IdentificadorAto = identificadorAto;
        TipoAto = tipoAto;
        Regime = regime;
        Cpf = cpf;
        Nome = nome;
        DataNascimento = dataNascimento;
        DescricaoCargo = descricaoCargo;
        CargaHorariaSemanal = cargaHorariaSemanal;
        ClassificacaoConcurso = classificacaoConcurso;
        DataAto = dataAto;
        DataHistorica = dataHistorica;
        DataTermino = dataTermino;
        MotivoExtincao = motivoExtincao;
    }

    /// <summary>Servidor de origem do ato no sistema (opcional — vinculo de rastreio interno).</summary>
    public ServidorId? ServidorId { get; private set; }

    /// <summary>Identificador unico do ato no sistema do usuario (IDENTIFICADOR_ATO).</summary>
    public string IdentificadorAto { get; private set; } = default!;

    /// <summary>Tipo/titulo da admissao (CD_TIPO_ATO — Tabela 4).</summary>
    public TipoAtoAdmissao TipoAto { get; private set; }

    /// <summary>Regime juridico de trabalho (CD_REGIME_JURIDICO — Tabela 5).</summary>
    public RegimeJuridicoSiapes Regime { get; private set; }

    /// <summary>CPF do servidor (dado sensivel — mascarado em logs/projecoes — LGPD).</summary>
    [CampoSensivelLgpd]
    public string Cpf { get; private set; } = default!;

    /// <summary>Nome do servidor (NOME — em maiusculas no arquivo).</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Data de nascimento (DATA_NASCIMENTO).</summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>Descricao do cargo atual (DS_CARGO).</summary>
    public string DescricaoCargo { get; private set; } = default!;

    /// <summary>Carga horaria semanal (CARGA_HORARIA).</summary>
    public int CargaHorariaSemanal { get; private set; }

    /// <summary>Classificacao no concurso (CLASSIFICACAO — titulo 01); nula nos demais.</summary>
    public int? ClassificacaoConcurso { get; private set; }

    /// <summary>Data do ato (DATA_ATO — admissao/exercicio).</summary>
    public DateOnly DataAto { get; private set; }

    /// <summary>Data historica (DATA_HISTORICA — nomeacao/inicio de prazo etc.); opcional/variavel por titulo.</summary>
    public DateOnly? DataHistorica { get; private set; }

    /// <summary>Data de termino (DATA_TERMINO — extincao de vinculo); nula se vinculo vigente.</summary>
    public DateOnly? DataTermino { get; private set; }

    /// <summary>Motivo da extincao (CD_EXTINCAO — Tabela 6); nao nulo quando ha data de termino.</summary>
    public MotivoExtincaoVinculo? MotivoExtincao { get; private set; }
}
