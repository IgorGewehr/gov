// Sub-navegacao interna do modulo Protocolo. Mantem UMA entrada na Sidebar e
// expoe os contextos do Bounded Context: Processos (PAE), Documentos (GED) e a
// gestao arquivistica CONARQ (Plano de Classificacao, Temporalidade, Destinacao).
// Abas de gestao arquivistica sao gated por 'protocolo.gerenciar' (espelha o RBAC).
import { SubNav, type SubNavItem } from '../../components/ui';

const ABAS: ReadonlyArray<SubNavItem> = [
  { to: '/protocolo', label: 'Processos', icon: 'fas fa-folder-open', end: true },
  { to: '/protocolo/documentos', label: 'Documentos', icon: 'fas fa-file-lines' },
  {
    to: '/protocolo/plano-classificacao',
    label: 'Plano de Classificação',
    icon: 'fas fa-sitemap',
    perm: 'protocolo.gerenciar',
  },
  {
    to: '/protocolo/temporalidade',
    label: 'Temporalidade (TTD)',
    icon: 'fas fa-hourglass-half',
    perm: 'protocolo.gerenciar',
  },
  {
    to: '/protocolo/destinacao',
    label: 'Destinação',
    icon: 'fas fa-box-archive',
    perm: 'protocolo.gerenciar',
  },
];

export function ProtocoloSubNav() {
  return <SubNav ariaLabel="Seções do Protocolo" itens={ABAS} />;
}
