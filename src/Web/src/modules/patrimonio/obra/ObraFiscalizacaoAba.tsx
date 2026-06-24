// Aba "Fiscalização" da ficha da obra (art. 117): fiscal designado, ocorrências
// (notificação/advertência/registro técnico) e o controle de estado de execução —
// ordem de início (Planejada -> EmExecucao), paralisação (I-15) e reinício. Gated.
import { useState } from 'react';
import { Button, Card, DataTable, EmptyState, Tag, Toolbar } from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import type { ObraDetalhe, OcorrenciaDto } from './obra.api';
import { ocorrenciaTipoLabel } from './obra.helpers';
import { ObraFiscalizacaoModal } from './ObraFiscalizacaoModal';
import type { FiscalizacaoAcao } from './ObraFiscalizacaoModal';
import { ObraEstadoModal } from './ObraEstadoModal';
import type { EstadoAcao } from './ObraEstadoModal';

const COLUNAS: Column<OcorrenciaDto>[] = [
  { key: 'data', header: 'Data', sortAccessor: (o) => o.data, render: (o) => formatarData(o.data) },
  { key: 'tipo', header: 'Tipo', render: (o) => <Tag variant="info">{ocorrenciaTipoLabel(o.tipo)}</Tag> },
  { key: 'descricao', header: 'Descrição', render: (o) => o.descricao },
];

export function ObraFiscalizacaoAba({ obra }: { obra: ObraDetalhe }) {
  const [fiscalizacao, setFiscalizacao] = useState<FiscalizacaoAcao | null>(null);
  const [estado, setEstado] = useState<EstadoAcao | null>(null);

  const planejada = obra.situacao === 'Planejada';
  const emExecucao = obra.situacao === 'EmExecucao';
  const paralisada = obra.situacao === 'Paralisada';

  return (
    <>
      <Card
        className="mb-4"
        header={<strong>Designação de fiscal e controle de execução</strong>}
        footer={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="secondary" size="sm" onClick={() => setFiscalizacao('fiscal')}>
                <i className="fas fa-user-shield" aria-hidden="true" /> Designar fiscal
              </Button>
              {planejada && (
                <Button variant="primary" size="sm" onClick={() => setEstado('ordemInicio')}>
                  <i className="fas fa-play" aria-hidden="true" /> Ordem de início
                </Button>
              )}
              {emExecucao && (
                <Button variant="danger" size="sm" onClick={() => setEstado('paralisar')}>
                  <i className="fas fa-pause" aria-hidden="true" /> Paralisar
                </Button>
              )}
              {paralisada && (
                <Button variant="primary" size="sm" onClick={() => setEstado('reiniciar')}>
                  <i className="fas fa-rotate-right" aria-hidden="true" /> Reiniciar
                </Button>
              )}
            </Toolbar>
          </Can>
        }
      >
        <dl className="row mb-0">
          <div className="col-sm-6 mb-2">
            <dt className="text-gray-60 text-down-01">Fiscal designado</dt>
            <dd className="mb-0 text-semi-bold">{obra.fiscalDesignadoId ?? 'Nenhum fiscal designado'}</dd>
          </div>
          <div className="col-sm-6 mb-2">
            <dt className="text-gray-60 text-down-01">Situação da execução</dt>
            <dd className="mb-0 text-semi-bold">
              {obra.dataInicioOrdemServico
                ? `Em execução desde ${formatarData(obra.dataInicioOrdemServico)}`
                : 'Sem ordem de início'}
            </dd>
          </div>
        </dl>
      </Card>

      <Card
        header={
          <div className="d-flex justify-content-between align-items-center flex-wrap">
            <strong>Ocorrências de fiscalização (art. 117)</strong>
            <Can permission="patrimonio.gerenciar">
              <Toolbar>
                <Button variant="primary" size="sm" onClick={() => setFiscalizacao('ocorrencia')}>
                  <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Registrar ocorrência
                </Button>
              </Toolbar>
            </Can>
          </div>
        }
      >
        <DataTable
          caption="Ocorrências registradas pela fiscalização"
          columns={COLUNAS}
          rows={obra.ocorrencias}
          rowKey={(o) => o.id}
          empty={
            <EmptyState
              icon="fas fa-user-shield"
              title="Sem ocorrências"
              description="Nenhuma notificação, advertência ou registro técnico foi lançado."
            />
          }
        />
      </Card>

      {fiscalizacao && (
        <ObraFiscalizacaoModal
          open
          onClose={() => setFiscalizacao(null)}
          obraId={obra.id}
          acao={fiscalizacao}
        />
      )}
      {estado && (
        <ObraEstadoModal open onClose={() => setEstado(null)} obraId={obra.id} acao={estado} />
      )}
    </>
  );
}
