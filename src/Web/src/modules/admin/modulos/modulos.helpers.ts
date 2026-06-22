// Helpers de apresentação da tela de Configuração de Módulos do tenant.
// O backend identifica cada módulo por uma string canônica (ex.: "tributos").
// Aqui mapeamos para um rótulo legível em PT-BR e um ícone gov.br, com fallback
// seguro (capitaliza o id) quando o backend introduzir um módulo ainda não
// catalogado no frontend — assim a tela nunca quebra ao crescer o catálogo.
import type { TagVariant } from '../../../components/ui';
import type { TenantModulo } from './modulos.api';

interface ModuloMeta {
  label: string;
  icon: string;
}

/** Catálogo de rótulos amigáveis por id canônico de módulo (CLAUDE.md §4). */
const MODULO_META: Record<string, ModuloMeta> = {
  administracao: { label: 'Administração (Compras e Licitações)', icon: 'fas fa-file-signature' },
  financas: { label: 'Finanças (Orçamento e Contabilidade)', icon: 'fas fa-coins' },
  tributos: { label: 'Tributos (IPTU, ISS, Dívida Ativa)', icon: 'fas fa-receipt' },
  recursoshumanos: { label: 'Recursos Humanos (Folha e eSocial)', icon: 'fas fa-users' },
  patrimonio: { label: 'Patrimônio (Bens e Frota)', icon: 'fas fa-boxes-stacked' },
  protocolo: { label: 'Protocolo (Processo Eletrônico)', icon: 'fas fa-folder-tree' },
  saude: { label: 'Saúde (UBS, PEP, Farmácia)', icon: 'fas fa-heart-pulse' },
  educacao: { label: 'Educação (Matrículas e Diário)', icon: 'fas fa-graduation-cap' },
  assistenciasocial: { label: 'Assistência Social (SUAS, CadÚnico)', icon: 'fas fa-hands-holding-child' },
  legislativo: { label: 'Legislativo (Sessões e Proposições)', icon: 'fas fa-landmark' },
  transparencia: { label: 'Transparência (LAI e remessas TCE)', icon: 'fas fa-chart-line' },
};

/** Rótulo legível do módulo; capitaliza o id quando fora do catálogo conhecido. */
export function moduloLabel(modulo: string): string {
  return MODULO_META[modulo]?.label ?? capitalizar(modulo);
}

/** Ícone gov.br (Font Awesome) do módulo; engrenagem genérica como fallback. */
export function moduloIcone(modulo: string): string {
  return MODULO_META[modulo]?.icon ?? 'fas fa-cube';
}

/** Variante da Tag de status conforme o módulo esteja ativo ou inativo. */
export function statusTagVariant(ativo: boolean): TagVariant {
  return ativo ? 'success' : 'default';
}

/** Rótulo de status em PT-BR. */
export function statusLabel(ativo: boolean): string {
  return ativo ? 'Ativo' : 'Inativo';
}

/** Ordena os módulos por rótulo amigável (estável) para uma listagem previsível. */
export function ordenarPorRotulo(modulos: TenantModulo[]): TenantModulo[] {
  return [...modulos].sort((a, b) => moduloLabel(a.modulo).localeCompare(moduloLabel(b.modulo), 'pt-BR'));
}

function capitalizar(texto: string): string {
  if (texto.length === 0) return texto;
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}
