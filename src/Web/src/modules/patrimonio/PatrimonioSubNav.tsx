// Sub-navegação interna do módulo Patrimonio. Usa o componente compartilhado SubNav
// (abas com flex-wrap — sempre visíveis) e mantém o módulo com UMA entrada na Sidebar.
// Espelha o padrão de RhSubNav. A aba "Bens" é a rota índice do módulo (end exato).
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/patrimonio', label: 'Bens patrimoniais', end: true },
  { to: '/patrimonio/frota', label: 'Frota' },
  { to: '/patrimonio/estoque', label: 'Almoxarifado' },
];

export function PatrimonioSubNav() {
  return <SubNav ariaLabel="Seções de Patrimônio" itens={ABAS} />;
}
