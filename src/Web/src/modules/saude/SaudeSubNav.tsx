// Sub-navegação interna do módulo Saúde. Usa o componente compartilhado SubNav
// (abas com flex-wrap — sempre visíveis, sem scroll horizontal escondido) e mantém
// o módulo com UMA entrada na Sidebar.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/saude', label: 'Pacientes', end: true },
  { to: '/saude/estabelecimentos', label: 'Estabelecimentos' },
  { to: '/saude/profissionais', label: 'Profissionais' },
  { to: '/saude/agenda', label: 'Agenda' },
  { to: '/saude/fila-espera', label: 'Fila de espera' },
  { to: '/saude/regulacao', label: 'Regulação' },
  { to: '/saude/fiscal', label: 'Painel Fiscal' },
];

export function SaudeSubNav() {
  return <SubNav ariaLabel="Seções de Saúde" itens={ABAS} />;
}
