using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

/// <summary>
/// Aluno da rede municipal de ensino: cadastro civil (com nome da mae obrigatorio para o EducaCenso),
/// endereco residencial, responsaveis (LGPD art. 14 — melhor interesse do menor) e o codigo INEP do
/// aluno (ID unico do Censo, preenchido pos-EducaCenso). E a entidade-mestre que destrava a matricula
/// real (picker de aluno em vez de GUID digitado). Raiz de agregado; nasce valida via <see cref="Cadastrar"/>
/// e protege as invariantes I-A1..I-A5.
/// </summary>
public sealed class Aluno : AggregateRoot<AlunoId>, IMustHaveTenant
{
    /// <summary>Comprimento exato do codigo INEP do aluno (ID unico no Censo).</summary>
    public const int ComprimentoCodigoInepAluno = 12;

    private readonly List<Responsavel> _responsaveis = [];

    private Aluno()
    {
    }

    private Aluno(
        AlunoId id,
        Guid tenantId,
        DadosCivis dadosCivis,
        EnderecoAluno endereco,
        Cpf? cpf)
        : base(id)
    {
        TenantId = tenantId;
        DadosCivis = dadosCivis;
        Endereco = endereco;
        Cpf = cpf;
        Situacao = SituacaoAluno.Ativo;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CPF do aluno, quando informado (recem-nascido pode nao ter) — PII redigida na trilha (LGPD).</summary>
    [CampoSensivelLgpd]
    public Cpf? Cpf { get; private set; }

    /// <summary>Codigo INEP unico do aluno no Censo/EducaCenso (12 digitos), quando ja atribuido.</summary>
    public string? CodigoInepAluno { get; private set; }

    /// <summary>Dados civis do aluno (PII de menor — LGPD art. 14).</summary>
    [CampoSensivelLgpd]
    public DadosCivis DadosCivis { get; private set; }

    /// <summary>Endereco residencial do aluno.</summary>
    public EnderecoAluno Endereco { get; private set; }

    /// <summary>Situacao atual do cadastro.</summary>
    public SituacaoAluno Situacao { get; private set; }

    /// <summary>Responsaveis pelo aluno (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<Responsavel> Responsaveis => _responsaveis;

    /// <summary>Indica se o aluno esta ativo (apto a novas matriculas/alteracoes).</summary>
    public bool Ativo => Situacao == SituacaoAluno.Ativo;

    /// <summary>
    /// Cadastra um novo aluno na rede de ensino. Nasce em situacao <see cref="SituacaoAluno.Ativo"/>.
    /// Para o aluno menor de idade na data informada, exige ao menos um responsavel (I-A2 — fail-closed).
    /// Emite <see cref="AlunoCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant (rede de ensino) dono do registro.</param>
    /// <param name="dadosCivis">Dados civis (com nome da mae obrigatorio — I-A1/I-A3).</param>
    /// <param name="endereco">Endereco residencial.</param>
    /// <param name="cpf">CPF do aluno (opcional).</param>
    /// <param name="responsaveis">Responsaveis (obrigatorio &gt;= 1 quando menor de idade — I-A2).</param>
    /// <param name="hoje">Data corrente para o calculo de maioridade.</param>
    /// <returns>Novo <see cref="Aluno"/> em situacao Ativo.</returns>
    /// <exception cref="ArgumentNullException">Se a colecao de responsaveis for nula.</exception>
    /// <exception cref="InvalidOperationException">Se for menor de idade sem responsavel (I-A2).</exception>
    public static Aluno Cadastrar(
        Guid tenantId,
        DadosCivis dadosCivis,
        EnderecoAluno endereco,
        Cpf? cpf,
        IReadOnlyCollection<Responsavel> responsaveis,
        DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(responsaveis);

        // I-A2: menor de idade exige ao menos um responsavel (fail-closed no factory).
        if (dadosCivis.EhMenorEm(hoje) && responsaveis.Count == 0)
        {
            throw new InvalidOperationException(
                "Aluno menor de idade exige ao menos um responsavel (LGPD art. 14).");
        }

        var aluno = new Aluno(AlunoId.New(), tenantId, dadosCivis, endereco, cpf);
        foreach (var responsavel in responsaveis)
        {
            ArgumentNullException.ThrowIfNull(responsavel);
            aluno._responsaveis.Add(responsavel);
        }

        aluno.RaiseDomainEvent(new AlunoCadastrado(aluno.Id));
        return aluno;
    }

    /// <summary>
    /// Adiciona um responsavel ao aluno ativo. Veda vinculo duplicado por (parentesco + CPF) — I-A2.
    /// Emite <see cref="ResponsavelAdicionado"/>.
    /// </summary>
    /// <param name="responsavel">Responsavel a adicionar.</param>
    /// <exception cref="ArgumentNullException">Se o responsavel for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se o aluno estiver inativo/transferido ou houver vinculo duplicado.</exception>
    public void AdicionarResponsavel(Responsavel responsavel)
    {
        ArgumentNullException.ThrowIfNull(responsavel);
        GarantirAtivo();

        var duplicado = _responsaveis.Any(existente =>
            existente.Parentesco == responsavel.Parentesco
            && CpfEquivalente(existente.Cpf, responsavel.Cpf));
        if (duplicado)
        {
            throw new InvalidOperationException("Responsavel ja vinculado (mesmo parentesco e CPF).");
        }

        _responsaveis.Add(responsavel);
        RaiseDomainEvent(new ResponsavelAdicionado(Id, responsavel.Id));
    }

    /// <summary>Atualiza os dados civis e o endereco do aluno ativo. Emite <see cref="DadosAlunoAtualizados"/>.</summary>
    /// <param name="dadosCivis">Novos dados civis.</param>
    /// <param name="endereco">Novo endereco residencial.</param>
    /// <exception cref="InvalidOperationException">Se o aluno estiver inativo/transferido (I-A5).</exception>
    public void AtualizarDados(DadosCivis dadosCivis, EnderecoAluno endereco)
    {
        GarantirAtivo();
        DadosCivis = dadosCivis;
        Endereco = endereco;
        RaiseDomainEvent(new DadosAlunoAtualizados(Id));
    }

    /// <summary>
    /// Vincula o codigo INEP do aluno (ID unico do Censo). Idempotente: so atribui quando ainda vazio;
    /// nunca sobrescreve um codigo ja atribuido (I-A4).
    /// </summary>
    /// <param name="codigoInepAluno">Codigo INEP do aluno (12 digitos).</param>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou tiver comprimento invalido.</exception>
    /// <exception cref="InvalidOperationException">Se o aluno estiver inativo/transferido.</exception>
    public void VincularCodigoInep(string codigoInepAluno)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoInepAluno);
        GarantirAtivo();

        if (!string.IsNullOrWhiteSpace(CodigoInepAluno))
        {
            return; // Idempotente: ja vinculado.
        }

        var normalizado = codigoInepAluno.Trim();
        if (normalizado.Length != ComprimentoCodigoInepAluno)
        {
            throw new ArgumentException(
                $"Codigo INEP do aluno deve ter {ComprimentoCodigoInepAluno} digitos.", nameof(codigoInepAluno));
        }

        CodigoInepAluno = normalizado;
    }

    /// <summary>Inativa o cadastro do aluno (obito, evasao definitiva, duplicidade) — estado terminal (I-A5).</summary>
    /// <param name="motivo">Motivo da inativacao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o aluno ja estiver inativo/transferido.</exception>
    public void Inativar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirAtivo();
        Situacao = SituacaoAluno.Inativo;
        RaiseDomainEvent(new AlunoInativado(Id, motivo));
    }

    private void GarantirAtivo()
    {
        if (Situacao != SituacaoAluno.Ativo)
        {
            throw new InvalidOperationException(
                $"Aluno em situacao terminal nao admite alteracoes. Situacao atual: {Situacao}.");
        }
    }

    private static bool CpfEquivalente(Cpf? a, Cpf? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        return a is not null && b is not null && a.Digitos == b.Digitos;
    }
}
