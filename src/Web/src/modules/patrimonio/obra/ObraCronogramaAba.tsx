// Aba "Cronograma" da ficha da obra: curva S (etapas × valores previstos, % físico
// previsto/executado, valor medido por etapa, situação) e a ação de DEFINIR o
// cronograma físico-financeiro (gated; só na obra Planejada — ObraCronogramaModal).
import { useState } from 'react';
import { Button, Card, DataTable, EmptyState, Tag, Toolbar } from '../../../components/ui';
import type { Column, TagVariant } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import type { EtapaCronogramaDto, ObraDetalhe } from './obra.api';
import { ObraCronogramaModal } from './ObraCronogramaModal';

function etapaTagVariant(situacao: EtapaCronogramaDto['situacao']): TagVariant {
  switch (situacao) {
    case 'Concluida':
      return 'success';
    case 'EmAndamento':
      return 'warning';
    default:
      return 'info';
  }
}

const COLUNAS: Column<EtapaCronogramaDto>[] = [
  { key: 'ordem', header: '#', align: 'end', sortAccessor: (e) => e.ordem, render: (e) => e.ordem },
  { key: 'descricao', header: 'Etapa', render: (e) => e.descricao },
  {
    key: 'previstoFisico',
    header: '% físico previsto',
    align: 'end',
    render: (e) => `${e.percentualFisicoPrevisto.toFixed(2)}%`,
  },
  {
    key: 'execFisico',
    header: '% físico executado',
    align: 'end',
    render: (e) => `${e.percentualFisicoExecutado.toFixed(2)}%`,
  },
  {
    key: 'valorPrevisto',
    header: 'Valor previsto',
    align: 'end',
    render: (e) => formatarMoeda(e.valorPrevisto),
  },
  {
    key: 'valorMedido',
    header: 'Valor medido',
    align: 'end',
    render: (e) => formatarMoeda(e.valorMedido),
  },
  {
    key: 'situacao',
    header: 'Situação',
    render: (e) => <Tag variant={etapaTagVariant(e.situacao)}>{e.situacao}</Tag>,
  },
];

export function ObraCronogramaAba({ obra }: { obra: ObraDetalhe }) {
  const [aberto, setAberto] = useState(false);
  const planejada = obra.situacao === 'Planejada';

  return (
    <>
      <Card
        header={
          <div className="d-flex justify-content-between align-items-center flex-wrap">
            <strong>Cronograma físico-financeiro (curva S)</strong>
            {planejada && (
              <Can permission="patrimonio.gerenciar">
                <Toolbar>
                  <Button variant="primary" size="sm" onClick={() => setAberto(true)}>
                    <i className="fas fa-pen" aria-hidden="true" /> Definir cronograma
                  </Button>
                </Toolbar>
              </Can>
            )}
          </div>
        }
      >
        <DataTable
          caption="Etapas do cronograma físico-financeiro"
          columns={COLUNAS}
          rows={obra.etapas}
          rowKey={(e) => e.id}
          empty={
            <EmptyState
              icon="fas fa-chart-line"
              title="Sem cronograma"
              description={
                planejada
                  ? 'Defina o cronograma físico-financeiro (curva S) antes da ordem de início.'
                  : 'Esta obra não possui etapas de cronograma cadastradas.'
              }
            />
          }
        />
      </Card>

      <ObraCronogramaModal
        open={aberto}
        onClose={() => setAberto(false)}
        obraId={obra.id}
        valorContratado={obra.valorContratado}
      />
    </>
  );
}
