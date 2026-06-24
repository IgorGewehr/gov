using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Application.Internal;

/// <summary>
/// Guarda de autorizacao dos comandos ADMINISTRATIVOS sobre a CONTA de um usuario alvo (reset de
/// senha, edicao de e-mail de login, ativacao/desativacao). Fecha os achados W10.6 ID-1/ID-2:
/// estes handlers nao recebiam a verificacao de ESCOPO/I4 que <see cref="AutorizacaoDeConcessao"/>
/// ja impoe a atribuicao/revogacao de papeis — abrindo escalonamento de privilegio (um admin de
/// sub-UO resetava a senha do admin-raiz e tomava a conta).
/// </summary>
/// <remarks>
/// A regra reusa o kernel I4 ("nao age sobre o que nao tem" — MODELO §3.1, CLAUDE.md I4) e tem DUAS
/// componentes, ambas avaliadas sobre o ESCOPO EFETIVO (permissao -> UOs) do administrador atual e do
/// alvo, resolvido a cada requisicao a partir das atribuicoes vigentes:
/// <list type="number">
/// <item><b>Contencao de escopo (ID-2)</b>: o administrador deve cobrir, com
/// <c>identidade.usuarios.gerenciar</c>, TODAS as UOs em que o alvo esta ancorado (a subarvore
/// administrada pelo admin contem a do alvo). Um admin escopado a uma sub-UO nao alcanca usuario
/// fora da sua subarvore.</item>
/// <item><b>Anti-escalacao (ID-1)</b>: para CADA permissao que o alvo detem, o administrador deve
/// possui-la cobrindo as MESMAS UOs (superconjunto via <see cref="EscopoEfetivo.CobreEscopo"/>). Assim
/// um admin NAO atua sobre usuario que detem papel/permissao que ele proprio nao detem — incluindo o
/// admin-raiz (que detem o conjunto global) e qualquer usuario mais poderoso.</item>
/// </list>
/// Deny-by-default e fail-closed: qualquer falha de resolucao (admin nao identificado/inexistente,
/// arvore ambigua) NEGA. Toda tentativa negada e AUDITADA (quem, alvo, IP, motivo) antes de lancar
/// <see cref="ConcessaoNaoAutorizadaException"/>, que a borda HTTP traduz em 403.
/// </remarks>
public sealed class AutorizacaoAdminUsuario(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<AutorizacaoAdminUsuario> logger)
{
    /// <summary>
    /// Verifica que o administrador da requisicao pode AGIR sobre a conta do usuario alvo (contencao
    /// de escopo + anti-escalacao I4). Nao faz I/O de escrita; nao altera estado. Lanca
    /// <see cref="ConcessaoNaoAutorizadaException"/> (auditando a tentativa) quando nega.
    /// </summary>
    /// <param name="alvo">Usuario alvo do comando administrativo (ja carregado, no tenant atual).</param>
    /// <param name="acao">Rotulo da acao para a trilha de auditoria (ex.: "AlterarSenha").</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <exception cref="ConcessaoNaoAutorizadaException">Se o admin nao cobre o escopo do alvo ou nao o domina.</exception>
    public async Task GarantirPodeAgirSobreAsync(Usuario alvo, string acao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alvo);

        var agora = clock.GetUtcNow();

        // Fail-closed: o administrador precisa estar identificado e existir no tenant.
        if (!Guid.TryParse(currentUser.UserId, out var adminGuid))
        {
            NegarEAuditar(alvo, acao, MotivoConcessaoNegada.SemPoderAdministrativo, permissao: null);
        }

        var admin = await usuarios.ObterPorIdAsync(new UsuarioId(adminGuid), cancellationToken).ConfigureAwait(false);
        if (admin is null)
        {
            NegarEAuditar(alvo, acao, MotivoConcessaoNegada.SemPoderAdministrativo, permissao: null);
        }

        var escopoAdmin = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(admin, papeis, unidades, agora, cancellationToken)
            .ConfigureAwait(false);

        // (a) Poder administrativo de usuarios em ALGUMA UO — pre-requisito (o grupo HTTP ja exige a
        // claim, mas reavaliamos no dominio para nao depender so da borda; deny-by-default).
        if (!escopoAdmin.Possui(DomainPermissoes.IdentidadeUsuariosGerenciar))
        {
            NegarEAuditar(alvo, acao, MotivoConcessaoNegada.SemPoderAdministrativo, permissao: null);
        }

        var escopoAlvo = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(alvo, papeis, unidades, agora, cancellationToken)
            .ConfigureAwait(false);

        // (b) CONTENCAO DE ESCOPO (ID-2): o admin precisa administrar todas as UOs onde o alvo esta
        // ancorado. Resolve o conjunto de UOs do alvo (uniao das suas atribuicoes vigentes, ja
        // expandidas) e exige que o poder administrativo do admin seja superconjunto.
        var uosDoAlvo = ResolverUnidadesDoAlvo(escopoAlvo, alvo, agora);
        if (uosDoAlvo.Count > 0
            && !escopoAdmin.CobreEscopo(DomainPermissoes.IdentidadeUsuariosGerenciar, uosDoAlvo))
        {
            NegarEAuditar(alvo, acao, MotivoConcessaoNegada.ForaDoEscopoAdministrativo, permissao: null);
        }

        // (c) ANTI-ESCALACAO (ID-1, I4 "nao age sobre o que nao tem"): para CADA permissao do alvo, o
        // admin deve possui-la cobrindo as mesmas UOs. Barra o admin escopado de tocar um usuario que
        // detenha papel/permissao (ou abrangencia) que ele proprio nao detem — incluindo o admin-raiz.
        foreach (var permissao in escopoAlvo.Permissoes)
        {
            var uosDaPermissao = escopoAlvo.UnidadesDaPermissao(permissao);
            if (!escopoAdmin.CobreEscopo(permissao, uosDaPermissao))
            {
                NegarEAuditar(alvo, acao, MotivoConcessaoNegada.PermissaoNaoPossuida, permissao);
            }
        }
    }

    // Uniao das UOs onde o alvo tem QUALQUER permissao (cobre o caso de papeis que so concedem
    // permissoes fora do verbo de gestao). Se o alvo nao tem nenhuma permissao efetiva (conta recem
    // criada/sem papel), cai no conjunto das UOs cruas das suas atribuicoes vigentes, para que um
    // admin de sub-UO ainda nao alcance uma conta ancorada fora da sua subarvore.
    private static HashSet<UnidadeOrganizacionalId> ResolverUnidadesDoAlvo(
        EscopoEfetivo escopoAlvo,
        Usuario alvo,
        DateTimeOffset agora)
    {
        var uos = new HashSet<UnidadeOrganizacionalId>();
        foreach (var permissao in escopoAlvo.Permissoes)
        {
            uos.UnionWith(escopoAlvo.UnidadesDaPermissao(permissao));
        }

        if (uos.Count == 0)
        {
            foreach (var atribuicao in alvo.Atribuicoes)
            {
                if (atribuicao.VigenteEm(agora))
                {
                    uos.Add(atribuicao.UnidadeId);
                }
            }
        }

        return uos;
    }

    // Audita a tentativa NEGADA (CLAUDE.md §6 — trilha de seguranca) e lanca a excecao de dominio.
    [DoesNotReturn]
    private void NegarEAuditar(Usuario alvo, string acao, MotivoConcessaoNegada motivo, string? permissao)
    {
        logger.LogWarning(
            "Tentativa NEGADA de acao administrativa sobre conta de usuario. Acao={Acao} AdminId={AdminId} "
            + "AlvoId={AlvoId} Ip={Ip} Motivo={Motivo} PermissaoFaltante={Permissao}",
            acao,
            currentUser.UserId,
            alvo.Id,
            currentUser.IpAddress,
            motivo,
            permissao);

        throw new ConcessaoNaoAutorizadaException(ResultadoConcessao.Negar(motivo, permissao));
    }
}
