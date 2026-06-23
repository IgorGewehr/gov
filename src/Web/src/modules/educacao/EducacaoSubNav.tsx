// Sub-navegação interna do módulo Educação. Usa o componente compartilhado SubNav
// (abas com flex-wrap — sempre visíveis, sem scroll horizontal escondido) e mantém
// o módulo com UMA entrada na Sidebar.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/educacao', label: 'Escolas', end: true },
  { to: '/educacao/matriculas', label: 'Matrículas' },
  { to: '/educacao/fiscal', label: 'Painel Fiscal' },
];

export function EducacaoSubNav() {
  return <SubNav ariaLabel="Seções de Educação" itens={ABAS} />;
}
