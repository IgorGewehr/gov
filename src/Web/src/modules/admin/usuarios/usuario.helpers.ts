// Helpers de apresentação e validação compartilhados pelas telas de Usuários.
import type { TagVariant } from '../../../components/ui';

/** Tamanho mínimo de senha (espelha a política do backend de Identidade). */
export const SENHA_MIN = 8;

/** Variante da Tag para o status ativo/inativo do usuário. */
export function ativoTagVariant(ativo: boolean): TagVariant {
  return ativo ? 'success' : 'default';
}

/** Rótulo legível para o status ativo/inativo. */
export function ativoLabel(ativo: boolean): string {
  return ativo ? 'Ativo' : 'Inativo';
}

/** Validação simples de e-mail (formato), suficiente para feedback de UI. */
export function emailValido(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}
