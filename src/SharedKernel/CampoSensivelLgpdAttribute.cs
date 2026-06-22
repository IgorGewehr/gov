namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Marca uma propriedade de dominio como dado pessoal SENSIVEL sob LGPD (CPF, NIS, CNS, dado
/// clinico, renda, etc.). LG-3: a redacao na trilha de auditoria e DENY-BY-DEFAULT — o
/// <c>AuditSaveChangesInterceptor</c> NUNCA serializa o valor de uma propriedade assim marcada,
/// substituindo-o por um marcador opaco. Diferente de <see cref="IHasRedactedAuditFields"/> (opt-in
/// para material cifrado do Cofre), este atributo declara a sensibilidade NO PROPRIO CAMPO, por
/// convencao explicita, sem exigir que a entidade implemente interface alguma.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class CampoSensivelLgpdAttribute : Attribute
{
}
