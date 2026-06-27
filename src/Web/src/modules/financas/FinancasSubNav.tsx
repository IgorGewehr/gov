// Sub-navegação interna do módulo Finanças (ciclo da despesa + contabilidade). Usa o
// componente compartilhado SubNav (abas com flex-wrap — sempre visíveis, sem scroll
// horizontal escondido) e mantém o módulo com UMA entrada na Sidebar.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/financas', label: 'Dotações', end: true },
  { to: '/financas/empenhos', label: 'Empenhos' },
  { to: '/financas/liquidacoes', label: 'Liquidações' },
  { to: '/financas/pagamentos', label: 'Pagamentos' },
  { to: '/financas/retencoes/guias', label: 'Retenções' },
  { to: '/financas/restos-a-pagar', label: 'Restos a Pagar' },
  { to: '/financas/tesouraria/contas', label: 'Tesouraria' },
  { to: '/financas/tesouraria/boletim', label: 'Boletim Caixa/Banco' },
  { to: '/financas/contabilidade/plano-de-contas', label: 'Plano de Contas' },
  { to: '/financas/contabilidade/balancete', label: 'Balancete' },
  { to: '/financas/contabilidade/lancamentos', label: 'Lançamentos' },
  { to: '/financas/contabilidade/diario', label: 'Diário' },
  { to: '/financas/contabilidade/demonstracoes', label: 'Demonstrações' },
  { to: '/financas/contabilidade/msc', label: 'MSC' },
];

export function FinancasSubNav() {
  return <SubNav ariaLabel="Seções de Finanças" itens={ABAS} />;
}
