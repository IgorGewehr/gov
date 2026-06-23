using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

/// <summary>
/// Profissional de saude (equipe das unidades) identificado pelo CPF, com registro de conselho de
/// classe (habilita teleconsulta — I-9) e historico de vinculos CNES/CBO por estabelecimento. Master
/// data LOCAL do Bounded Context Saude que materializa o que antes era um GUID solto referenciado
/// pelo <see cref="Domain.Atendimento.Atendimento"/>. Raiz de agregado: nasce valida via
/// <see cref="Cadastrar"/> e protege as invariantes de CPF valido, unicidade por tenant e vinculo
/// unico ativo por (estabelecimento, CBO).
/// <para>
/// O <see cref="ProfissionalId"/> e o MESMO tipo forte ja referenciado pelo atendimento
/// (<c>Domain.Atendimento.ProfissionalId</c>) — referencia cross-aggregate por Id, sem navegacao.
/// </para>
/// </summary>
public sealed class Profissional : AggregateRoot<ProfissionalId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome do profissional.</summary>
    public const int ComprimentoNome = 120;

    /// <summary>Comprimento exato do CNS (quando informado).</summary>
    public const int ComprimentoCns = 15;

    private readonly List<VinculoCnes> _vinculos = [];

    private Profissional()
    {
    }

    private Profissional(
        ProfissionalId id,
        Guid tenantId,
        Cpf cpf,
        string nome,
        string? cns,
        RegistroConselho? registro)
        : base(id)
    {
        TenantId = tenantId;
        Cpf = cpf;
        Nome = nome;
        Cns = cns;
        Registro = registro;
        Situacao = SituacaoProfissional.Ativo;
        RaiseDomainEvent(new ProfissionalCadastrado(id));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CPF do profissional (chave de negocio) — PII redigida na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public Cpf Cpf { get; private set; } = default!;

    /// <summary>Nome do profissional.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>CNS do profissional (opcional).</summary>
    public string? Cns { get; private set; }

    /// <summary>Registro em conselho de classe (opcional; habilita teleconsulta quando CRM — I-9).</summary>
    public RegistroConselho? Registro { get; private set; }

    /// <summary>Situacao cadastral atual.</summary>
    public SituacaoProfissional Situacao { get; private set; }

    /// <summary>Vinculos CNES/CBO (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<VinculoCnes> Vinculos => _vinculos;

    /// <summary>Indica se o profissional possui CRM e esta ativo (habilita teleconsulta — I-9).</summary>
    public bool TemCrmAtivo => Situacao == SituacaoProfissional.Ativo && Registro is { Tipo: TipoConselho.Crm };

    /// <summary>
    /// Cadastra um novo profissional de saude. Nasce <see cref="SituacaoProfissional.Ativo"/>.
    /// Emite <see cref="ProfissionalCadastrado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cpf">CPF do profissional (chave de negocio).</param>
    /// <param name="nome">Nome do profissional (obrigatorio).</param>
    /// <param name="cns">CNS do profissional (opcional, 15 digitos).</param>
    /// <param name="registro">Registro em conselho de classe (opcional).</param>
    /// <returns>Novo <see cref="Profissional"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o CPF for nulo.</exception>
    /// <exception cref="ArgumentException">Se o nome for vazio/exceder o limite ou o CNS for invalido.</exception>
    public static Profissional Cadastrar(
        Guid tenantId,
        Cpf cpf,
        string nome,
        string? cns,
        RegistroConselho? registro)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        var normalizado = NormalizarNome(nome);
        var cnsNormalizado = NormalizarCns(cns);
        return new Profissional(ProfissionalId.New(), tenantId, cpf, normalizado, cnsNormalizado, registro);
    }

    /// <summary>Atualiza os dados cadastrais do profissional ativo (CPF e imutavel).</summary>
    /// <param name="nome">Novo nome.</param>
    /// <param name="cns">Novo CNS (opcional).</param>
    /// <param name="registro">Novo registro de conselho (opcional).</param>
    /// <exception cref="ArgumentException">Se o nome for vazio/exceder o limite ou o CNS for invalido.</exception>
    /// <exception cref="InvalidOperationException">Se o profissional estiver inativo.</exception>
    public void AtualizarDados(string nome, string? cns, RegistroConselho? registro)
    {
        GarantirAtivo();
        Nome = NormalizarNome(nome);
        Cns = NormalizarCns(cns);
        Registro = registro;
    }

    /// <summary>
    /// Abre um vinculo CNES/CBO no estabelecimento a partir de <paramref name="inicio"/>. Bloqueia mais
    /// de um vinculo ATIVO simultaneo para o mesmo (estabelecimento, CBO). Emite <see cref="VinculoProfissionalAberto"/>.
    /// </summary>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="cbo">Ocupacao (CBO).</param>
    /// <param name="inicio">Data de inicio do vinculo.</param>
    /// <exception cref="InvalidOperationException">Se o profissional estiver inativo ou ja houver vinculo ativo identico.</exception>
    public void Vincular(EstabelecimentoId estabelecimentoId, Cbo cbo, DateOnly inicio)
    {
        GarantirAtivo();

        var jaTemAtivo = _vinculos.Any(vinculo =>
            !vinculo.Encerrado
            && vinculo.EstabelecimentoId == estabelecimentoId
            && vinculo.Cbo == cbo);
        if (jaTemAtivo)
        {
            throw new InvalidOperationException(
                "Ja existe vinculo ativo para este profissional, estabelecimento e CBO.");
        }

        _vinculos.Add(VinculoCnes.Abrir(estabelecimentoId, cbo, inicio));
        RaiseDomainEvent(new VinculoProfissionalAberto(Id, estabelecimentoId, cbo.Valor));
    }

    /// <summary>Encerra o vinculo ativo do profissional no estabelecimento em <paramref name="fim"/>.</summary>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="fim">Data de encerramento.</param>
    /// <exception cref="InvalidOperationException">Se nao houver vinculo ativo no estabelecimento.</exception>
    public void EncerrarVinculo(EstabelecimentoId estabelecimentoId, DateOnly fim)
    {
        var vinculo = _vinculos.FirstOrDefault(v => !v.Encerrado && v.EstabelecimentoId == estabelecimentoId)
            ?? throw new InvalidOperationException("Nao ha vinculo ativo neste estabelecimento.");

        vinculo.Encerrar(fim);
        RaiseDomainEvent(new VinculoProfissionalEncerrado(Id, estabelecimentoId));
    }

    /// <summary>Indica se o profissional tem vinculo CNES/CBO ativo no estabelecimento na data informada.</summary>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="data">Data de referencia.</param>
    /// <returns><c>true</c> se houver vinculo vigente em <paramref name="data"/>.</returns>
    public bool TemVinculoAtivo(EstabelecimentoId estabelecimentoId, DateOnly data)
        => Situacao == SituacaoProfissional.Ativo
            && _vinculos.Any(vinculo => vinculo.EstabelecimentoId == estabelecimentoId && vinculo.VigenteEm(data));

    /// <summary>Inativa o profissional (desligamento). Estado terminal. Emite <see cref="ProfissionalInativado"/>.</summary>
    /// <exception cref="InvalidOperationException">Se o profissional ja estiver inativo.</exception>
    public void Inativar()
    {
        GarantirAtivo();
        Situacao = SituacaoProfissional.Inativo;
        RaiseDomainEvent(new ProfissionalInativado(Id));
    }

    private static string NormalizarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var normalizado = nome.Trim();
        if (normalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome do profissional excede {ComprimentoNome} caracteres.", nameof(nome));
        }

        return normalizado;
    }

    private static string? NormalizarCns(string? cns)
    {
        if (string.IsNullOrWhiteSpace(cns))
        {
            return null;
        }

        var digitos = new string(cns.Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length != ComprimentoCns)
        {
            throw new ArgumentException($"CNS do profissional deve ter {ComprimentoCns} digitos.", nameof(cns));
        }

        return digitos;
    }

    private void GarantirAtivo()
    {
        if (Situacao != SituacaoProfissional.Ativo)
        {
            throw new InvalidOperationException(
                $"Profissional inativo nao admite alteracoes. Situacao atual: {Situacao}.");
        }
    }
}
