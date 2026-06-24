// Fluxo A — DETALHE de um convenio federal recebido (query ObterConvenio).
// Mostra o CICLO completo (proposta/celebrar -> execucao fisico-financeira +
// contrapartida -> PC parcial/final -> analise/saneamento) e o SEMAFORO DE PRAZO
// de analise de cada prestacao de contas. Leitura (Can convenios.ver no roteamento).
import { Link, useParams } from 'react-router-dom';
import {
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useConvenio } from '../convenios.api';
import type { ConvenioDetalhe, PrestacaoConvenioDetalhe } from '../convenios.api';
import {
  situacaoConvenioLabel,
  situacaoConvenioTag,
  semaforoLabel,
  semaforoPrazo,
  semaforoTag,
} from '../convenios.helpers';
import { ConveniosSubNav } from '../ConveniosSubNav';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_PC: Column<PrestacaoConvenioDetalhe>[] = [
  { key: 'tipo', header: 'Tipo', render: (p) => p.tipo },
  { key: 'situacao', header: 'Situação', render: (p) => p.situacao },
  {
    key: 'submissao',
    header: 'Submissão',
    sortAccessor: (p) => p.dataSubmissao ?? '',
    render: (p) => formatarData(p.dataSubmissao),
  },
  {
    key: 'prazo',
    header: 'Prazo de análise',
    sortAccessor: (p) => p.prazoAnalise ?? '',
    render: (p) => {
      const sem = semaforoPrazo(p.prazoAnalise);
      if (!sem) return '—';
      return (
        <Tag variant={semaforoTag(sem)}>
          {formatarData(p.prazoAnalise)} · {semaforoLabel(sem)}
        </Tag>
      );
    },
  },
];

export function ConvenioDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useConvenio(id);

  return (
    <>
      <ConveniosSubNav />
      <PageHeader
        eyebrow="Convênios recebidos"
        title="Detalhe do convênio"
        actions={
          <Link className="br-button secondary" to="/convenios">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ConvenioDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(convenio) => (
          <>
            <Card
              className="mb-4"
              header={
                <div className="d-flex justify-content-between align-items-center">
                  <strong>{convenio.concedenteNome}</strong>
                  <Tag variant={situacaoConvenioTag(convenio.situacao)}>
                    {situacaoConvenioLabel(convenio.situacao)}
                  </Tag>
                </div>
              }
            >
              <dl className="row">
                <Campo rotulo="Concedente (CNPJ)">{convenio.concedenteCnpj}</Campo>
                <Campo rotulo="Nº Transferegov">{convenio.numeroTransferegov ?? '—'}</Campo>
                <Campo rotulo="Objeto">{convenio.objeto}</Campo>
                <Campo rotulo="Valor do repasse">{formatarMoeda(convenio.valorRepasse)}</Campo>
                <Campo rotulo="Contrapartida">{formatarMoeda(convenio.valorContrapartida)}</Campo>
                <Campo rotulo="Valor global">{formatarMoeda(convenio.valorGlobal)}</Campo>
                <Campo rotulo="Vigência">
                  {convenio.vigenciaInicio
                    ? `${formatarData(convenio.vigenciaInicio)} a ${formatarData(convenio.vigenciaFim)}`
                    : '—'}
                </Campo>
              </dl>
            </Card>

            <Card header={<strong>Prestações de contas</strong>}>
              <DataTable
                caption={`Prestações de contas do convênio ${convenio.concedenteNome}`}
                columns={COLUNAS_PC}
                rows={convenio.prestacoes}
                rowKey={(p) => p.id}
                empty={
                  <EmptyState
                    icon="fas fa-file-invoice"
                    title="Nenhuma prestação de contas"
                    description="Ainda não há prestação de contas (parcial ou final) registrada para este convênio."
                  />
                }
              />
            </Card>
          </>
        )}
      </QueryState>
    </>
  );
}
