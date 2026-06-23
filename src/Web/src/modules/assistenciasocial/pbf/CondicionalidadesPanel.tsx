// Painel de condicionalidades de UM acompanhamento (linha expandida da busca ativa).
// Lista os registros do periodo com status e oferta as acoes: registrar condicionalidade
// e justificar um descumprimento (gating por permissao gerenciar).
import { useState } from 'react';
import { Button, DataTable, EmptyState, Tag } from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import type { AcompanhamentoResultado, CondicionalidadeResultado } from './pbf.api';
import { statusLabel, statusTagVariant, tipoCondicionalidadeLabel } from './pbf.helpers';
import { RegistrarCondicionalidadeModal } from './RegistrarCondicionalidadeModal';
import { JustificarDescumprimentoModal } from './JustificarDescumprimentoModal';

export interface CondicionalidadesPanelProps {
  acompanhamento: AcompanhamentoResultado;
}

export function CondicionalidadesPanel({ acompanhamento }: CondicionalidadesPanelProps) {
  const [registrarAberto, setRegistrarAberto] = useState(false);
  const [justificar, setJustificar] = useState<CondicionalidadeResultado | null>(null);

  const columns: Column<CondicionalidadeResultado>[] = [
    {
      key: 'tipo',
      header: 'Eixo',
      sortAccessor: (c) => c.tipo,
      render: (c) => tipoCondicionalidadeLabel(c.tipo),
    },
    {
      key: 'status',
      header: 'Status',
      sortAccessor: (c) => c.status,
      render: (c) => <Tag variant={statusTagVariant(c.status)}>{statusLabel(c.status)}</Tag>,
    },
    {
      key: 'membro',
      header: 'Membro',
      render: (c) => c.membroId,
    },
    {
      key: 'observacao',
      header: 'Observação',
      render: (c) => c.observacao ?? '—',
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) =>
        c.status === 'Descumprida' ? (
          <Can permission="assistenciasocial.gerenciar">
            <Button variant="tertiary" size="sm" onClick={() => setJustificar(c)}>
              Justificar
            </Button>
          </Can>
        ) : (
          '—'
        ),
    },
  ];

  return (
    <div className="p-2">
      <Can permission="assistenciasocial.gerenciar">
        <div className="mb-2 d-flex justify-content-end">
          <Button variant="secondary" size="sm" onClick={() => setRegistrarAberto(true)}>
            <i className="fas fa-plus" aria-hidden="true" /> Registrar condicionalidade
          </Button>
        </div>
      </Can>

      <DataTable
        caption={`Condicionalidades do acompanhamento ${acompanhamento.competencia}`}
        columns={columns}
        rows={acompanhamento.condicionalidades}
        rowKey={(c) => c.registroId}
        empty={
          <EmptyState
            icon="fas fa-list-check"
            title="Sem condicionalidades registradas"
            description="Nenhuma condicionalidade foi registrada neste acompanhamento."
          />
        }
      />

      <RegistrarCondicionalidadeModal
        open={registrarAberto}
        onClose={() => setRegistrarAberto(false)}
        acompanhamento={acompanhamento}
      />

      {justificar && (
        <JustificarDescumprimentoModal
          open
          onClose={() => setJustificar(null)}
          acompanhamento={acompanhamento}
          registro={justificar}
        />
      )}
    </div>
  );
}
