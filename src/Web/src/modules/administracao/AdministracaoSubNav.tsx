// Sub-navegação interna do módulo Administracao (Compras e Licitações). Usa o
// componente compartilhado SubNav (abas com flex-wrap — sempre visíveis) e mantém o
// módulo com UMA entrada na Sidebar. Espelha o padrão de RhSubNav.
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/administracao/licitacoes', label: 'Licitações' },
  { to: '/administracao/contratos', label: 'Contratos' },
  { to: '/administracao/fornecedores', label: 'Fornecedores' },
];

export function AdministracaoSubNav() {
  return <SubNav ariaLabel="Seções de Compras e Licitações" itens={ABAS} />;
}
