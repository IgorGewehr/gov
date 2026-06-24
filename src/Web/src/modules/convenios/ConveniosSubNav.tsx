// Sub-navegacao interna do modulo Convenios. Mantem UMA entrada na Sidebar e expoe
// os DOIS fluxos do Bounded Context: Convenios federais RECEBIDOS (fluxo A) e
// Parcerias OSC / MROSC (fluxo B). Visibilidade gated por 'convenios.ver'.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  {
    to: '/convenios',
    label: 'Convênios recebidos',
    icon: 'fas fa-hand-holding-dollar',
    perm: 'convenios.ver',
    end: true,
  },
  {
    to: '/convenios/parcerias',
    label: 'Parcerias OSC (MROSC)',
    icon: 'fas fa-people-group',
    perm: 'convenios.ver',
  },
];

export function ConveniosSubNav() {
  return <SubNav ariaLabel="Seções de Convênios e Parcerias" itens={ABAS} />;
}
