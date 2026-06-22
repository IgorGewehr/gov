using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

/// <summary>
/// Parlamentar (vereador) da Camara Municipal: o titular do mandato cujo <see cref="VereadorId"/>
/// ja e referenciado por <c>Presenca</c>, <c>Voto</c> e pela tribuna. Raiz de agregado que da NOME,
/// partido e situacao de mandato ao identificador antes cru, alimentando o painel eletronico, a lista
/// de votos nominais e a ata (CF/88 art. 29; Lei Organica Municipal; Regimento Interno). Reusa o
/// <see cref="VereadorId"/> existente (Sessoes) como identidade — vincula o cadastro sem quebrar o
/// modelo de Sessao/Votacao. Mantem trilha imutavel pela auditoria do interceptor.
/// </summary>
public sealed class Vereador : AggregateRoot<VereadorId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome civil/parlamentar.</summary>
    public const int NomeMaximo = 200;

    /// <summary>Comprimento maximo da sigla partidaria.</summary>
    public const int PartidoMaximo = 30;

    private Vereador()
    {
    }

    private Vereador(
        VereadorId id,
        Guid tenantId,
        string nomeCivil,
        string nomeParlamentar,
        string partido,
        int legislaturaInicio,
        int legislaturaFim,
        CargoMesa cargoMesa)
        : base(id)
    {
        TenantId = tenantId;
        NomeCivil = nomeCivil;
        NomeParlamentar = nomeParlamentar;
        Partido = partido;
        LegislaturaInicio = legislaturaInicio;
        LegislaturaFim = legislaturaFim;
        CargoMesa = cargoMesa;
        Situacao = SituacaoVereador.EmExercicio;
        RaiseDomainEvent(new VereadorCadastrado(id, nomeParlamentar, partido));
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome civil completo (registro).</summary>
    public string NomeCivil { get; private set; } = default!;

    /// <summary>Nome parlamentar (como aparece no painel, na ata e na cedula).</summary>
    public string NomeParlamentar { get; private set; } = default!;

    /// <summary>Sigla do partido pelo qual exerce o mandato.</summary>
    public string Partido { get; private set; } = default!;

    /// <summary>Ano de inicio da legislatura (mandato de 4 anos).</summary>
    public int LegislaturaInicio { get; private set; }

    /// <summary>Ano de fim da legislatura.</summary>
    public int LegislaturaFim { get; private set; }

    /// <summary>Cargo ocupado na Mesa Diretora (ou <see cref="CargoMesa.Nenhum"/>).</summary>
    public CargoMesa CargoMesa { get; private set; }

    /// <summary>Situacao atual do mandato.</summary>
    public SituacaoVereador Situacao { get; private set; }

    /// <summary>Indica se o mandato esta em estado terminal (encerrado/cassado).</summary>
    public bool Terminal => Situacao == SituacaoVereador.Encerrado;

    /// <summary>
    /// Cadastra um novo vereador em <see cref="SituacaoVereador.EmExercicio"/> (V-1, V-2, V-3).
    /// Quando o <paramref name="id"/> nao e informado, gera um novo; quando informado (ex.: seed
    /// ou vinculo a presenca/voto preexistente), reusa-o como identidade.
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="nomeCivil">Nome civil completo (obrigatorio).</param>
    /// <param name="nomeParlamentar">Nome parlamentar (obrigatorio; default = nome civil).</param>
    /// <param name="partido">Sigla partidaria (obrigatoria).</param>
    /// <param name="legislaturaInicio">Ano de inicio da legislatura (plausivel).</param>
    /// <param name="legislaturaFim">Ano de fim (maior que o inicio).</param>
    /// <param name="cargoMesa">Cargo na Mesa Diretora (opcional).</param>
    /// <param name="id">Identidade a reusar (opcional; gera nova se nulo).</param>
    /// <returns>Novo <see cref="Vereador"/> valido em exercicio.</returns>
    /// <exception cref="ArgumentException">Se nome/partido forem vazios ou a legislatura for implausivel.</exception>
    public static Vereador Cadastrar(
        Guid tenantId,
        string nomeCivil,
        string nomeParlamentar,
        string partido,
        int legislaturaInicio,
        int legislaturaFim,
        CargoMesa cargoMesa = CargoMesa.Nenhum,
        VereadorId? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCivil);
        ArgumentException.ThrowIfNullOrWhiteSpace(partido);

        var civil = Normalizar(nomeCivil, NomeMaximo, nameof(nomeCivil));
        var parlamentar = string.IsNullOrWhiteSpace(nomeParlamentar)
            ? civil
            : Normalizar(nomeParlamentar, NomeMaximo, nameof(nomeParlamentar));
        var sigla = Normalizar(partido, PartidoMaximo, nameof(partido)).ToUpperInvariant();

        ValidarLegislatura(legislaturaInicio, legislaturaFim);
        if (!Enum.IsDefined(cargoMesa))
        {
            throw new ArgumentException("Cargo de Mesa invalido.", nameof(cargoMesa));
        }

        return new Vereador(
            id ?? VereadorId.New(),
            tenantId,
            civil,
            parlamentar,
            sigla,
            legislaturaInicio,
            legislaturaFim,
            cargoMesa);
    }

    /// <summary>Atualiza os dados editaveis do cadastro (V-4) — nao altera a identidade nem a legislatura ja iniciada.</summary>
    /// <param name="nomeCivil">Novo nome civil.</param>
    /// <param name="nomeParlamentar">Novo nome parlamentar.</param>
    /// <param name="partido">Nova sigla partidaria (filiacao pode mudar no mandato).</param>
    /// <param name="cargoMesa">Novo cargo na Mesa Diretora.</param>
    /// <exception cref="ArgumentException">Se nome/partido forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se o mandato estiver encerrado (terminal).</exception>
    public void AtualizarCadastro(string nomeCivil, string nomeParlamentar, string partido, CargoMesa cargoMesa)
    {
        GarantirNaoTerminal();
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCivil);
        ArgumentException.ThrowIfNullOrWhiteSpace(partido);
        if (!Enum.IsDefined(cargoMesa))
        {
            throw new ArgumentException("Cargo de Mesa invalido.", nameof(cargoMesa));
        }

        NomeCivil = Normalizar(nomeCivil, NomeMaximo, nameof(nomeCivil));
        NomeParlamentar = string.IsNullOrWhiteSpace(nomeParlamentar)
            ? NomeCivil
            : Normalizar(nomeParlamentar, NomeMaximo, nameof(nomeParlamentar));
        Partido = Normalizar(partido, PartidoMaximo, nameof(partido)).ToUpperInvariant();
        CargoMesa = cargoMesa;
    }

    /// <summary>Altera a situacao do mandato respeitando a maquina de estados (V-5).</summary>
    /// <param name="situacao">Nova situacao.</param>
    /// <exception cref="ArgumentException">Se a situacao for invalida.</exception>
    /// <exception cref="InvalidOperationException">Se o mandato ja estiver encerrado (terminal).</exception>
    public void AlterarSituacao(SituacaoVereador situacao)
    {
        if (!Enum.IsDefined(situacao))
        {
            throw new ArgumentException("Situacao de vereador invalida.", nameof(situacao));
        }

        // V-5: Encerrado e terminal — nao retorna ao exercicio.
        GarantirNaoTerminal();
        Situacao = situacao;
    }

    private void GarantirNaoTerminal()
    {
        if (Terminal)
        {
            throw new InvalidOperationException("Mandato encerrado nao admite alteracao.");
        }
    }

    private static void ValidarLegislatura(int inicio, int fim)
    {
        const int AnoMinimo = 1900;
        const int AnoMaximo = 2100;
        if (inicio is < AnoMinimo or > AnoMaximo)
        {
            throw new ArgumentException("Ano de inicio da legislatura implausivel.", nameof(inicio));
        }

        if (fim <= inicio || fim > AnoMaximo)
        {
            throw new ArgumentException("Ano de fim da legislatura deve ser maior que o inicio.", nameof(fim));
        }
    }

    private static string Normalizar(string valor, int maximo, string parametro)
    {
        var normalizado = valor.Trim();
        if (normalizado.Length > maximo)
        {
            throw new ArgumentException($"{parametro} excede {maximo} caracteres.", parametro);
        }

        return normalizado;
    }
}
