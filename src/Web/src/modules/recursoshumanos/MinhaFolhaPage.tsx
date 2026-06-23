// Área de AUTOSSERVIÇO "Minha Folha" do servidor. Reúne, em abas internas, os dados do
// PRÓPRIO usuário: contracheque, espelho de ponto, férias e informe de rendimentos.
// Gated por 'autosservico.proprio' (Can) — espelha o RequirePermission do backend. A UI
// NUNCA envia servidorId: é dado-próprio resolvido do JWT no handler.
import { useState } from 'react';
import { Card, EmptyState, PageHeader } from '../../components/ui';
import { useHasPermission } from '../../auth/Can';
import { PERM_RH_AUTOSSERVICO } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';
import { MeuContrachequeSecao, MeuEspelhoPontoSecao } from './MinhaFolhaSecoes';
import { MeuInformeRendimentosSecao, MinhasFeriasSecao } from './MinhaFolhaSecoesAnuais';

type Aba = 'contracheque' | 'ponto' | 'ferias' | 'informe';

interface AbaDef {
  id: Aba;
  label: string;
  icon: string;
}

const ABAS: AbaDef[] = [
  { id: 'contracheque', label: 'Meu contracheque', icon: 'fas fa-receipt' },
  { id: 'ponto', label: 'Meu espelho de ponto', icon: 'fas fa-clock' },
  { id: 'ferias', label: 'Minhas férias', icon: 'fas fa-umbrella-beach' },
  { id: 'informe', label: 'Informe de rendimentos', icon: 'fas fa-file-invoice-dollar' },
];

export function MinhaFolhaPage() {
  const [aba, setAba] = useState<Aba>('contracheque');
  const podeAutosservico = useHasPermission(PERM_RH_AUTOSSERVICO);

  if (!podeAutosservico) {
    return (
      <>
        <RhSubNav />
        <PageHeader
          title="Minha Folha"
          description="Autosserviço do servidor: seus próprios dados de folha e ponto."
        />
        <EmptyState
          icon="fas fa-lock"
          title="Acesso restrito"
          description="Você não tem permissão de autosserviço (autosservico.proprio)."
        />
      </>
    );
  }

  return (
    <>
      <RhSubNav />
      <PageHeader
        title="Minha Folha"
        description="Autosserviço do servidor: consulte SEUS próprios contracheque, ponto, férias e informe de rendimentos."
      />

      <nav className="mb-4" aria-label="Seções da Minha Folha">
        <ul
          className="d-flex flex-wrap list-style-none p-0 m-0"
          style={{ gap: '0.5rem' }}
          role="tablist"
        >
          {ABAS.map((a) => (
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
        {aba === 'contracheque' && <MeuContrachequeSecao />}
        {aba === 'ponto' && <MeuEspelhoPontoSecao />}
        {aba === 'ferias' && <MinhasFeriasSecao />}
        {aba === 'informe' && <MeuInformeRendimentosSecao />}
      </Card>
    </>
  );
}
