using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte do agregado <see cref="Condutor"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CondutorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CondutorId"/>.</returns>
    public static CondutorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Condutor (motorista) habilitado da frota como cadastro próprio (raiz de agregado): controla a
/// Carteira Nacional de Habilitação — número, categorias habilitadas, e a validade do exame de aptidão
/// física e mental (CTB, Lei 9.503/1997 art. 147; periodicidade da Lei 14.071/2021: 10 anos &lt;50,
/// 5 anos 50–69, 3 anos ≥70, parametrizável por tenant). Habilita o alerta e o bloqueio de viagem com
/// CNH vencida (I): condutor com CNH vencida ou suspenso não pode ser escalado. Dado pessoal sob LGPD.
/// </summary>
public sealed class Condutor : AggregateRoot<CondutorId>, IMustHaveTenant
{
    private Condutor()
    {
    }

    private Condutor(
        CondutorId id,
        Guid tenantId,
        string nome,
        string cpf,
        string numeroCnh,
        string categorias,
        DateOnly validadeCnh,
        Guid? servidorId)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Cpf = cpf;
        NumeroCnh = numeroCnh;
        Categorias = categorias;
        ValidadeCnh = validadeCnh;
        ServidorId = servidorId;
        Situacao = SituacaoCondutor.Ativo;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome do condutor.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>CPF do condutor (dado pessoal — LGPD).</summary>
    public string Cpf { get; private set; } = default!;

    /// <summary>Número da Carteira Nacional de Habilitação (CNH).</summary>
    public string NumeroCnh { get; private set; } = default!;

    /// <summary>
    /// Categorias habilitadas na CNH, normalizadas e ordenadas (ex.: "AB", "D", "AE"). Cada letra é uma
    /// categoria do CTB; a escala de um veículo exige condutor habilitado na categoria exigida.
    /// </summary>
    public string Categorias { get; private set; } = default!;

    /// <summary>Validade do exame de aptidão física e mental (data de vencimento da CNH).</summary>
    public DateOnly ValidadeCnh { get; private set; }

    /// <summary>
    /// Vínculo opcional com o servidor (módulo RH) que é o condutor — referência de identidade
    /// (cross-module por Contracts), sem navegação para o agregado do outro módulo.
    /// </summary>
    public Guid? ServidorId { get; private set; }

    /// <summary>Situação administrativa do condutor (ativo/suspenso/inativo).</summary>
    public SituacaoCondutor Situacao { get; private set; }

    /// <summary>Motivo da suspensão administrativa, quando suspenso.</summary>
    public string? MotivoSuspensao { get; private set; }

    /// <summary>Indica se a CNH está válida (não vencida) na data informada.</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns><c>true</c> se a CNH estiver válida em <paramref name="hoje"/>.</returns>
    public bool CnhValidaEm(DateOnly hoje) => ValidadeCnh >= hoje;

    /// <summary>Dias até o vencimento da CNH a partir da data informada (negativo se vencida).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns>Dias até o vencimento (pode ser negativo).</returns>
    public int DiasParaVencerCnh(DateOnly hoje) => ValidadeCnh.DayNumber - hoje.DayNumber;

    /// <summary>
    /// Indica se o condutor está apto a ser escalado (dirigir) em uma viagem na data informada: precisa
    /// estar ativo (não suspenso/inativo) e com CNH válida (não vencida). É a base do bloqueio de viagem
    /// com CNH vencida.
    /// </summary>
    /// <param name="hoje">Data de referência (dia da viagem).</param>
    /// <returns><c>true</c> se o condutor puder ser escalado.</returns>
    public bool AptoParaConduzirEm(DateOnly hoje) => Situacao == SituacaoCondutor.Ativo && CnhValidaEm(hoje);

    /// <summary>Indica se o condutor está habilitado na categoria informada (qualquer letra das categorias).</summary>
    /// <param name="categoria">Categoria exigida (ex.: "D"). Caso-insensível.</param>
    /// <returns><c>true</c> se a categoria constar das categorias habilitadas.</returns>
    public bool HabilitadoNaCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return false;
        }

        return categoria.Trim().ToUpperInvariant().All(letra => Categorias.Contains(letra, StringComparison.Ordinal));
    }

    /// <summary>
    /// Cadastra um novo condutor habilitado, ativo, com a CNH informada. A validade não pode ser passada
    /// na data de referência (não se cadastra condutor já com CNH vencida).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome do condutor.</param>
    /// <param name="cpf">CPF do condutor.</param>
    /// <param name="numeroCnh">Número da CNH.</param>
    /// <param name="categorias">Categorias habilitadas (ex.: "AB").</param>
    /// <param name="validadeCnh">Validade do exame de aptidão (CNH).</param>
    /// <param name="hoje">Data de referência para a checagem de validade.</param>
    /// <param name="servidorId">Vínculo opcional com o servidor (RH).</param>
    /// <returns>Novo <see cref="Condutor"/> ativo.</returns>
    /// <exception cref="ArgumentException">Se nome, CPF, CNH ou categorias forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se a CNH já estiver vencida na data de referência.</exception>
    public static Condutor Cadastrar(
        Guid tenantId,
        string nome,
        string cpf,
        string numeroCnh,
        string categorias,
        DateOnly validadeCnh,
        DateOnly hoje,
        Guid? servidorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(cpf);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroCnh);
        var categoriasNormalizadas = NormalizarCategorias(categorias);
        if (validadeCnh < hoje)
        {
            throw new InvalidOperationException(
                $"Condutor com CNH vencida não pode ser cadastrado. Validade: {validadeCnh:yyyy-MM-dd}.");
        }

        return new Condutor(
            CondutorId.New(),
            tenantId,
            nome,
            cpf,
            numeroCnh,
            categoriasNormalizadas,
            validadeCnh,
            servidorId);
    }

    /// <summary>
    /// Renova a CNH do condutor, estendendo a validade para uma nova data e, opcionalmente, atualizando as
    /// categorias habilitadas. A nova validade deve ser posterior à atual (renovação não retroage). Se o
    /// condutor estava suspenso por CNH vencida, a renovação por si só não o reativa — use <see cref="Reativar"/>.
    /// </summary>
    /// <param name="novaValidade">Nova validade da CNH (posterior à atual).</param>
    /// <param name="novasCategorias">Categorias atualizadas (nulo mantém as atuais).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a nova validade não for posterior à atual.</exception>
    /// <exception cref="InvalidOperationException">Se o condutor estiver inativo.</exception>
    public void RenovarCnh(DateOnly novaValidade, string? novasCategorias)
    {
        if (Situacao == SituacaoCondutor.Inativo)
        {
            throw new InvalidOperationException("Condutor inativo não pode ter a CNH renovada; reative-o antes.");
        }

        if (novaValidade <= ValidadeCnh)
        {
            throw new ArgumentOutOfRangeException(
                nameof(novaValidade),
                $"Nova validade ({novaValidade:yyyy-MM-dd}) deve ser posterior à atual ({ValidadeCnh:yyyy-MM-dd}).");
        }

        ValidadeCnh = novaValidade;
        if (!string.IsNullOrWhiteSpace(novasCategorias))
        {
            Categorias = NormalizarCategorias(novasCategorias);
        }

        RaiseDomainEvent(new CnhRenovada(Id, novaValidade));
    }

    /// <summary>
    /// Suspende administrativamente o condutor (medida interna), impedindo a escala. Não é terminal:
    /// pode ser reativado.
    /// </summary>
    /// <param name="motivo">Motivo da suspensão.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o condutor estiver inativo.</exception>
    public void Suspender(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoCondutor.Inativo)
        {
            throw new InvalidOperationException("Condutor inativo não pode ser suspenso.");
        }

        Situacao = SituacaoCondutor.Suspenso;
        MotivoSuspensao = motivo;
    }

    /// <summary>Reativa um condutor suspenso, devolvendo-o à situação ativa.</summary>
    /// <exception cref="InvalidOperationException">Se o condutor não estiver suspenso.</exception>
    public void Reativar()
    {
        if (Situacao != SituacaoCondutor.Suspenso)
        {
            throw new InvalidOperationException(
                $"Só um condutor suspenso pode ser reativado. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoCondutor.Ativo;
        MotivoSuspensao = null;
    }

    /// <summary>Inativa o condutor (desligamento/afastamento), estado terminal para a escala de frota.</summary>
    /// <exception cref="InvalidOperationException">Se o condutor já estiver inativo.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoCondutor.Inativo)
        {
            throw new InvalidOperationException("Condutor já inativo.");
        }

        Situacao = SituacaoCondutor.Inativo;
    }

    // Normaliza as categorias: maiúsculas, sem espaços/separadores e sem letras repetidas, ordenadas.
    private static string NormalizarCategorias(string categorias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categorias);
        var letras = categorias
            .ToUpperInvariant()
            .Where(char.IsLetter)
            .Distinct()
            .OrderBy(letra => letra)
            .ToArray();

        if (letras.Length == 0)
        {
            throw new ArgumentException("Categorias da CNH inválidas.", nameof(categorias));
        }

        return new string(letras);
    }
}
