// Sub-navegação interna do módulo Transparência (uma entrada na Sidebar; a navegação
// entre as seções do Bounded Context acontece aqui). Reusa o componente-base <SubNav>
// recém-criado. Onda 0 (navegabilidade): expõe explicitamente as seções consultáveis —
// Remessas TCE-RS, Declarações fiscais (SICONFI) e o Painel de Mínimos Constitucionais.
import { SubNav } from '../../components/ui';
import type { SubNavItem } from '../../components/ui';

const ITENS: ReadonlyArray<SubNavItem> = [
  { to: '/transparencia', label: 'Remessas TCE-RS', icon: 'fas fa-paper-plane', end: true, perm: 'transparencia.ver' },
  { to: '/transparencia/declaracoes-fiscais', label: 'Declarações fiscais', icon: 'fas fa-file-invoice', perm: 'transparencia.ver' },
  { to: '/transparencia/minimos', label: 'Mínimos constitucionais', icon: 'fas fa-scale-balanced', perm: 'transparencia.ver' },
];

/** Abas de navegação entre as seções consultáveis do módulo Transparência. */
export function TransparenciaSubNav() {
  return <SubNav ariaLabel="Seções de Transparência" itens={ITENS} />;
}
