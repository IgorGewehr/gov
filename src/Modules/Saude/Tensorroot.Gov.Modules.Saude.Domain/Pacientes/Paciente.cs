using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>Identificador forte do agregado <see cref="Paciente"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PacienteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PacienteId"/>.</returns>
    public static PacienteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Cidadao usuario do SUS, identificado de forma univoca pelo CNS, com registro clinico
/// longitudinal (condicoes de saude e alergias) tratado como dado pessoal sensivel (LGPD art. 11).
/// Raiz de agregado do PEP/e-SUS APS: nasce valida via <see cref="Cadastrar"/> e protege as
/// invariantes de confirmacao CADSUS, unicidade por tenant e estado terminal.
/// </summary>
public sealed class Paciente : AggregateRoot<PacienteId>, IMustHaveTenant
{
    private readonly List<CondicaoDeSaude> _condicoes = [];
    private readonly List<Alergia> _alergias = [];

    private Paciente()
    {
    }

    private Paciente(
        PacienteId id,
        Guid tenantId,
        Cns cns,
        Identificacao identificacao,
        Endereco endereco)
        : base(id)
    {
        TenantId = tenantId;
        Cns = cns;
        Identificacao = identificacao;
        Endereco = endereco;
        CnsConfirmado = false;
        Situacao = SituacaoPaciente.Ativo;
        RaiseDomainEvent(new PacienteCadastrado(id, cns));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Cartao Nacional de Saude (chave de negocio, imutavel apos o cadastro) — PII redigida na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public Cns Cns { get; private set; }

    /// <summary>Dados civis do paciente.</summary>
    public Identificacao Identificacao { get; private set; }

    /// <summary>Endereco residencial do paciente.</summary>
    public Endereco Endereco { get; private set; }

    /// <summary>Indica se o CNS foi validado/confirmado na base nacional CADSUS.</summary>
    public bool CnsConfirmado { get; private set; }

    /// <summary>Situacao atual do cadastro.</summary>
    public SituacaoPaciente Situacao { get; private set; }

    /// <summary>Condicoes de saude registradas (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<CondicaoDeSaude> Condicoes => _condicoes;

    /// <summary>Alergias registradas (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<Alergia> Alergias => _alergias;

    /// <summary>
    /// Cadastra um novo paciente no PEP a partir de um CNS. Nasce em situacao
    /// <see cref="SituacaoPaciente.Ativo"/> com <see cref="CnsConfirmado"/> falso (a confirmacao
    /// CADSUS e aplicada por <see cref="ConfirmarCadastro"/>). Emite <see cref="PacienteCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cns">Cartao Nacional de Saude (chave de negocio).</param>
    /// <param name="identificacao">Dados civis do paciente.</param>
    /// <param name="endereco">Endereco residencial.</param>
    /// <returns>Novo <see cref="Paciente"/>.</returns>
    public static Paciente Cadastrar(Guid tenantId, Cns cns, Identificacao identificacao, Endereco endereco)
        => new(PacienteId.New(), tenantId, cns, identificacao, endereco);

    /// <summary>
    /// Confirma o cadastro a partir do resultado positivo da validacao CADSUS, transicionando
    /// <see cref="CnsConfirmado"/> de falso para verdadeiro (idempotente; nunca regride). Emite
    /// <see cref="CadastroConfirmadoNoCadsus"/> apenas na primeira confirmacao.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o paciente estiver inativado.</exception>
    public void ConfirmarCadastro()
    {
        GarantirAtivo();
        if (CnsConfirmado)
        {
            return;
        }

        CnsConfirmado = true;
        RaiseDomainEvent(new CadastroConfirmadoNoCadsus(Id, Cns));
    }

    /// <summary>Atualiza os dados cadastrais (identificacao/endereco) do paciente ativo.</summary>
    /// <param name="identificacao">Novos dados civis.</param>
    /// <param name="endereco">Novo endereco residencial.</param>
    /// <exception cref="InvalidOperationException">Se o paciente estiver inativado.</exception>
    public void AtualizarCadastro(Identificacao identificacao, Endereco endereco)
    {
        GarantirAtivo();
        Identificacao = identificacao;
        Endereco = endereco;
        RaiseDomainEvent(new CadastroAtualizado(Id));
    }

    /// <summary>Registra uma condicao de saude (CID-10/CIAP-2) no historico do paciente ativo.</summary>
    /// <param name="codigo">Codigo CID-10/CIAP-2 (obrigatorio).</param>
    /// <param name="descricao">Descricao da condicao.</param>
    /// <param name="dataRegistro">Data do registro (nao futura).</param>
    /// <exception cref="ArgumentException">Se o codigo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o paciente estiver inativado.</exception>
    public void RegistrarCondicaoDeSaude(string codigo, string descricao, DateOnly dataRegistro)
    {
        GarantirAtivo();
        var condicao = CondicaoDeSaude.Registrar(codigo, descricao, dataRegistro, dataRegistro);
        _condicoes.Add(condicao);
        RaiseDomainEvent(new CondicaoDeSaudeRegistrada(Id, condicao.Codigo));
    }

    /// <summary>Registra uma alergia no historico do paciente ativo.</summary>
    /// <param name="substancia">Substancia/agente (obrigatorio).</param>
    /// <param name="gravidade">Gravidade da reacao.</param>
    /// <param name="dataRegistro">Data do registro (nao futura).</param>
    /// <exception cref="ArgumentException">Se a substancia for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o paciente estiver inativado.</exception>
    public void RegistrarAlergia(string substancia, string gravidade, DateOnly dataRegistro)
    {
        GarantirAtivo();
        var alergia = Alergia.Registrar(substancia, gravidade, dataRegistro, dataRegistro);
        _alergias.Add(alergia);
        RaiseDomainEvent(new AlergiaRegistrada(Id, alergia.Substancia));
    }

    /// <summary>
    /// Inativa o cadastro do paciente (obito, transferencia, duplicidade). Estado terminal:
    /// nao admite novas transicoes de negocio. Emite <see cref="PacienteInativado"/>.
    /// </summary>
    /// <param name="motivo">Motivo da inativacao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o paciente ja estiver inativado.</exception>
    public void Inativar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirAtivo();
        Situacao = SituacaoPaciente.Inativo;
        RaiseDomainEvent(new PacienteInativado(Id, motivo));
    }

    private void GarantirAtivo()
    {
        if (Situacao != SituacaoPaciente.Ativo)
        {
            throw new InvalidOperationException(
                $"Paciente inativado nao admite alteracoes. Situacao atual: {Situacao}.");
        }
    }
}
