// Ficha (detalhe) de uma OBRA (W9.3, Lei 14.133/2021) com ABAS internas:
//   - Visão geral: identificação, valores, % físico e STATUS do prazo art. 94 §3;
//   - Cronograma: curva S (etapas × valores, % físico previsto/executado) + definir;
//   - Medição/RDO: registrar RDO diário, registrar medição por etapa e aprovar/rejeitar
//     (gated) — a aprovação libera a liquidação e a medição acumulada não excede o teto;
//   - Fiscalização: fiscal designado, ocorrências (art. 117) e paralisação/reinício.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
} from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { useObra } from './obra.api';
import type { ObraDetalhe } from './obra.api';
import { situacaoObraLabel, situacaoObraTagVariant } from './obra.helpers';
import { ObraVisaoGeral } from './ObraVisaoGeral';
import { ObraCronogramaAba } from './ObraCronogramaAba';
import { ObraMedicaoRdoAba } from './ObraMedicaoRdoAba';
import { ObraFiscalizacaoAba } from './ObraFiscalizacaoAba';

type Aba = 'geral' | 'cronograma' | 'medicao' | 'fiscalizacao';

const ABAS: ReadonlyArray<{ id: Aba; label: string; icon: string }> = [
  { id: 'geral', label: 'Visão geral', icon: 'fas fa-circle-info' },
  { id: 'cronograma', label: 'Cronograma', icon: 'fas fa-chart-line' },
  { id: 'medicao', label: 'Medição / RDO', icon: 'fas fa-ruler-combined' },
  { id: 'fiscalizacao', label: 'Fiscalização', icon: 'fas fa-user-shield' },
];

export function ObraDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useObra(id);
  const [aba, setAba] = useState<Aba>('geral');

  return (
    <>
      <PageHeader
        eyebrow="Patrimônio"
        title="Obra / serviço de engenharia"
        actions={
          <Link className="br-button secondary" to="/patrimonio/obras">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ObraDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-helmet-safety"
            title="Obra não encontrada"
            description="Verifique o identificador informado."
          />
        }
      >
        {(obra) => (
          <>
            <div className="d-flex justify-content-between align-items-center flex-wrap mb-3">
              <h2 className="mb-0">{obra.objeto}</h2>
              <Tag variant={situacaoObraTagVariant(obra.situacao)}>
                {situacaoObraLabel(obra.situacao)}
              </Tag>
            </div>

            <nav aria-label="Seções da obra" className="mb-4">
              <Toolbar>
                {ABAS.map((item) => (
                  <Button
                    key={item.id}
                    variant={aba === item.id ? 'primary' : 'secondary'}
                    size="sm"
                    aria-current={aba === item.id ? 'true' : undefined}
                    onClick={() => setAba(item.id)}
                  >
                    <i className={item.icon} aria-hidden="true" /> {item.label}
                  </Button>
                ))}
              </Toolbar>
            </nav>

            {aba === 'geral' && <ObraVisaoGeral obra={obra} />}
            {aba === 'cronograma' && (
              <Can permission="patrimonio.ver">
                <ObraCronogramaAba obra={obra} />
              </Can>
            )}
            {aba === 'medicao' && <ObraMedicaoRdoAba obra={obra} />}
            {aba === 'fiscalizacao' && <ObraFiscalizacaoAba obra={obra} />}
          </>
        )}
      </QueryState>
    </>
  );
}
