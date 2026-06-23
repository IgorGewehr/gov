// Navegacao interna (secoes) do modulo Legislativo. Usa o componente compartilhado
// SubNav (abas com flex-wrap — sempre visiveis, sem scroll horizontal escondido). A
// Sidebar expoe um unico item por modulo; este sub-nav alcanca as demais telas.
import { SubNav, type SubNavItem } from '../../components/ui';

const SECOES: ReadonlyArray<SubNavItem> = [
  { to: '/legislativo', label: 'Proposições', icon: 'fas fa-folder-open', end: true },
  { to: '/legislativo/sessoes', label: 'Sessões', icon: 'fas fa-calendar-day' },
  { to: '/legislativo/votacoes', label: 'Votações', icon: 'fas fa-square-poll-vertical' },
  { to: '/legislativo/painel', label: 'Painel ao vivo', icon: 'fas fa-tower-broadcast' },
  { to: '/legislativo/vereadores', label: 'Vereadores', icon: 'fas fa-users' },
  { to: '/legislativo/comissoes', label: 'Comissões', icon: 'fas fa-people-group' },
  { to: '/legislativo/normas', label: 'Normas', icon: 'fas fa-scale-balanced' },
  { to: '/legislativo/diario', label: 'Diário Oficial', icon: 'fas fa-newspaper' },
  { to: '/legislativo/tribuna', label: 'Tribuna', icon: 'fas fa-microphone' },
  { to: '/legislativo/ata', label: 'Ata', icon: 'fas fa-file-lines' },
];

export function LegislativoSecoesNav() {
  return <SubNav ariaLabel="Seções do Legislativo" itens={SECOES} />;
}
