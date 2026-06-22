// Helpers de apresentação da Trilha de Auditoria. Mapeiam o vocabulário do
// AuditSaveChangesInterceptor (EF Core) para rótulos legíveis em PT-BR e variantes
// semânticas de Tag, e formatam os payloads JSON (oldValues/newValues) de forma
// legível para auditores (Tribunal de Contas).
import type { TagVariant } from '../../../components/ui';

/** Rótulo legível em PT-BR para a operação auditada (EF EntityState). */
export function acaoLabel(action: string): string {
  switch (action) {
    case 'Added':
      return 'Criação';
    case 'Modified':
      return 'Alteração';
    case 'Deleted':
      return 'Exclusão';
    default:
      return action;
  }
}

/** Variante semântica da Tag para a operação auditada. */
export function acaoTagVariant(action: string): TagVariant {
  switch (action) {
    case 'Added':
      return 'success';
    case 'Modified':
      return 'warning';
    case 'Deleted':
      return 'danger';
    default:
      return 'info';
  }
}

/**
 * Formata um payload JSON cru (oldValues/newValues) de forma legível (indentado).
 * Aceita string JSON ou nula; devolve null quando não há conteúdo, permitindo à
 * UI exibir um marcador de "vazio" sem renderizar um <pre> em branco.
 */
export function formatarJson(raw: string | null | undefined): string | null {
  const limpo = raw?.trim();
  if (!limpo) return null;
  try {
    return JSON.stringify(JSON.parse(limpo), null, 2);
  } catch {
    // Não é JSON válido (ex.: texto livre) — devolve o conteúdo original.
    return limpo;
  }
}
