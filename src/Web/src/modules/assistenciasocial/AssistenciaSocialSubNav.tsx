// Sub-navegacao interna do modulo AssistenciaSocial. Usa o componente compartilhado
// SubNav (abas com flex-wrap — sempre visiveis, sem scroll horizontal escondido) e
// mantem o modulo com UMA entrada na Sidebar.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/assistenciasocial', label: 'Famílias / CadÚnico', end: true },
  { to: '/assistenciasocial/beneficios', label: 'Benefícios' },
  { to: '/assistenciasocial/beneficios/concessoes', label: 'Concessões por competência' },
  { to: '/assistenciasocial/prontuarios', label: 'Prontuário SUAS' },
  { to: '/assistenciasocial/pbf/condicionalidades', label: 'PBF — Condicionalidades' },
  { to: '/assistenciasocial/censo/unidades', label: 'Censo — Unidades' },
  { to: '/assistenciasocial/censo', label: 'Censo — Consolidação', end: true },
  { to: '/assistenciasocial/igd', label: 'IGD (estimativa)' },
];

export function AssistenciaSocialSubNav() {
  return <SubNav ariaLabel="Seções de Assistência Social" itens={ABAS} />;
}
