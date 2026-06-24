// Sub-navegação interna do módulo Tributos. Usa o componente compartilhado SubNav
// (abas com flex-wrap — sempre visíveis, sem scroll horizontal escondido) e mantém
// o módulo com UMA entrada na Sidebar.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/tributos', label: 'Dívida Ativa', end: true },
  { to: '/tributos/imoveis', label: 'Imóveis' },
  { to: '/tributos/iptu/parametros', label: 'Parâmetros do IPTU' },
  { to: '/tributos/iss', label: 'ISS' },
  { to: '/tributos/gia', label: 'GIA ISS' },
  { to: '/tributos/certidoes', label: 'Certidões (CND)' },
  { to: '/tributos/itbi', label: 'ITBI' },
  { to: '/tributos/taxas', label: 'Taxas' },
  { to: '/tributos/cosip', label: 'COSIP' },
  { to: '/tributos/alvaras', label: 'Alvarás' },
  { to: '/tributos/melhoria', label: 'Contribuição de Melhoria' },
];

export function TributosSubNav() {
  return <SubNav ariaLabel="Seções de Tributos" itens={ABAS} />;
}
