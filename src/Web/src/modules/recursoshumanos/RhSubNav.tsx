// Sub-navegação interna do módulo RecursosHumanos. Usa o componente compartilhado
// SubNav (abas com flex-wrap — sempre visíveis, sem scroll horizontal escondido) e
// mantém o módulo com UMA entrada na Sidebar. A aba "Minha Folha" só aparece para
// quem tem a permissão de autosserviço.
import { SubNav, type SubNavItem } from '../../components/ui';
import { PERM_RH_AUTOSSERVICO } from './recursosHumanos.helpers';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/recursoshumanos', label: 'Servidores', end: true },
  { to: '/recursoshumanos/cargos', label: 'Cargos' },
  { to: '/recursoshumanos/planos-carreira', label: 'Planos de Carreira' },
  { to: '/recursoshumanos/portarias', label: 'Portarias' },
  { to: '/recursoshumanos/processos-trabalhistas', label: 'Processos Trab.' },
  { to: '/recursoshumanos/sicap-pessoal', label: 'SICAP-AP' },
  { to: '/recursoshumanos/rubricas', label: 'Rubricas' },
  { to: '/recursoshumanos/tabelas-legais', label: 'Tabelas Legais' },
  { to: '/recursoshumanos/folhas', label: 'Folha de Pagamento' },
  { to: '/recursoshumanos/ciclo-anual', label: 'Ciclo Anual' },
  { to: '/recursoshumanos/pasep', label: 'PASEP' },
  { to: '/recursoshumanos/ponto', label: 'Ponto' },
  { to: '/recursoshumanos/esocial', label: 'eSocial' },
  { to: '/recursoshumanos/relatorios', label: 'Relatórios' },
  { to: '/recursoshumanos/minha-folha', label: 'Minha Folha', perm: PERM_RH_AUTOSSERVICO },
];

export function RhSubNav() {
  return <SubNav ariaLabel="Seções de Recursos Humanos" itens={ABAS} />;
}
