namespace Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

/// <summary>
/// Referencia da pessoa-propria (cidadao) resolvida server-side: o DOCUMENTO civil (CPF/CNPJ em
/// digitos) e a conta de origem. E o UNICO dado passado aos modulos-fonte (Tributos/Protocolo) via
/// Contracts — nunca um id interno de outro modulo (CLAUDE.md §2).
/// </summary>
/// <param name="Documento">CPF/CNPJ (somente digitos) do cidadao autenticado.</param>
/// <param name="Nome">Nome do cidadao (exibicao).</param>
public readonly record struct PessoaCidadaoRef(string Documento, string Nome);

/// <summary>
/// Porta-ANCORA do dado-proprio do Portal do Cidadao (espelho fiel do
/// <c>IResolvedorServidorDoUsuarioAutenticado</c> do Minha Folha): resolve a PESSOA do cidadao
/// ATUALMENTE AUTENTICADO (subject "sub" do JWT, lido do <c>ICurrentUser</c>), NUNCA de um id/CPF
/// informado pelo cliente. E o coracao do isolamento dado-proprio a prova de bala: todo handler de
/// "meus dados" obtem a pessoa por aqui e SO consulta dados dela.
/// </summary>
public interface IResolvedorPessoaDoCidadaoAutenticado
{
    /// <summary>
    /// Resolve a pessoa-propria do cidadao autenticado. LANCA <see cref="CidadaoSemVinculoException"/>
    /// quando nao ha principal autenticado, o <c>sub</c> nao mapeia para uma conta-cidadao no tenant, ou
    /// a conta esta inativa — negar por padrao (sem conta/vinculo = sem dado-proprio), selando a negativa
    /// na trilha LGPD.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A referencia (documento+nome) da pessoa do proprio cidadao autenticado.</returns>
    /// <exception cref="CidadaoSemVinculoException">Quando nao ha pessoa-propria resolvivel.</exception>
    Task<PessoaCidadaoRef> ResolverPessoaAtualAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Erro de DENY-BY-DEFAULT do Portal do Cidadao: o principal autenticado nao possui (ou nao se pode
/// resolver) uma conta-cidadao ativa no tenant atual. Sem ela NAO ha dado-proprio a expor — o acesso e
/// abortado (403) e a tentativa e selada na trilha LGPD pelo resolvedor. A mensagem nao vaza o <c>sub</c>.
/// </summary>
public sealed class CidadaoSemVinculoException : Exception
{
    /// <summary>Cria a excecao com a mensagem padrao (deny-by-default, sem vazar id).</summary>
    public CidadaoSemVinculoException()
        : base("Principal autenticado nao possui conta-cidadao ativa neste tenant; acesso ao portal negado.")
    {
    }
}
