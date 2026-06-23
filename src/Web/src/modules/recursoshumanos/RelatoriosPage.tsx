// Área de RELATÓRIOS gerenciais da folha (Onda 3a — ONDA3-DESIGN §4.1). Reúne, em abas
// internas, os quatro relatórios somente-leitura: folha por secretaria/fonte, evolução
// mensal da despesa, mapa de cargos e demonstrativo TCE. Gated por 'recursoshumanos.ver'
// (Can/useHasPermission) — espelha o RequirePermission do backend.
import { useState } from 'react';
import { Card, EmptyState, PageHeader } from '../../components/ui';
import { useHasPermission } from '../../auth/Can';
import { PERM_RH_VER } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';
import { ABAS_RELATORIO } from './relatorio.helpers';
import type { AbaRelatorio } from './relatorio.helpers';
import { DemonstrativoTceSecao, FolhaPorSecretariaSecao } from './RelatorioFolhaSecoes';
import { EvolucaoDespesaSecao, MapaCargosSecao } from './RelatorioEvolucaoMapa';

export function RelatoriosPage() {
  const [aba, setAba] = useState<AbaRelatorio>('folha-secretaria');
  const podeVer = useHasPermission(PERM_RH_VER);

  if (!podeVer) {
    return (
      <>
        <RhSubNav />
        <PageHeader
          eyebrow="Recursos Humanos"
          title="Relatórios"
          description="Relatórios gerenciais da folha de pagamento."
        />
        <EmptyState
          icon="fas fa-lock"
          title="Acesso restrito"
          description="Você não tem permissão de leitura (recursoshumanos.ver)."
        />
      </>
    );
  }

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Relatórios"
        description="Relatórios gerenciais da folha: por secretaria/fonte, evolução da despesa, mapa de cargos e demonstrativo TCE."
      />

      <nav className="mb-4" aria-label="Seções dos relatórios de RH">
        <ul
          className="d-flex flex-wrap list-style-none p-0 m-0"
          style={{ gap: '0.5rem' }}
          role="tablist"
        >
          {ABAS_RELATORIO.map((a) => (
            <li key={a.id} role="presentation">
              <button
                type="button"
                role="tab"
                aria-selected={aba === a.id}
                className={`br-button small ${aba === a.id ? 'primary' : 'secondary'}`}
                onClick={() => setAba(a.id)}
              >
                <i className={a.icon} aria-hidden="true" /> {a.label}
              </button>
            </li>
          ))}
        </ul>
      </nav>

      <Card>
        {aba === 'folha-secretaria' && <FolhaPorSecretariaSecao />}
        {aba === 'evolucao' && <EvolucaoDespesaSecao />}
        {aba === 'mapa-cargos' && <MapaCargosSecao />}
        {aba === 'demonstrativo-tce' && <DemonstrativoTceSecao />}
      </Card>
    </>
  );
}
