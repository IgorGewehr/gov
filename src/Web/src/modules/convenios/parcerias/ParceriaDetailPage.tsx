// Fluxo B — DETALHE de uma parceria OSC / MROSC (query ObterParceria).
// Mostra o CICLO completo (chamamento/dispensa -> termo -> plano -> repasses ->
// PC OSC -> analise) e deixa CLARO o estado Inadimplente, com aviso de que os
// repasses ficam BLOQUEADOS (B-INV-9 — LRF art. 48). Leitura (Can convenios.ver).
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useParceria } from '../convenios.api';
import type { ParceriaDetalhe, RepasseOscDetalhe } from '../convenios.api';
import {
  parceriaBloqueada,
  situacaoParceriaLabel,
  situacaoParceriaTag,
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

const COLUNAS_REPASSE: Column<RepasseOscDetalhe>[] = [
  {
    key: 'ordem',
    header: 'Parcela',
    align: 'end',
    sortAccessor: (r) => r.numeroOrdem,
    render: (r) => `#${r.numeroOrdem}`,
  },
  {
    key: 'valor',
    header: 'Valor',
    align: 'end',
    sortAccessor: (r) => r.valor,
    render: (r) => formatarMoeda(r.valor),
  },
  { key: 'situacao', header: 'Situação', render: (r) => r.situacao },
  {
    key: 'execucao',
    header: 'Execução orçamentária',
    render: (r) =>
      r.execucaoCompleta ? (
        <Tag variant="success">Empenho + liquidação + pagamento</Tag>
      ) : (
        <Tag variant="warning">Incompleta</Tag>
      ),
  },
];

export function ParceriaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useParceria(id);

  return (
    <>
      <ConveniosSubNav />
      <PageHeader
        eyebrow="Parcerias OSC (MROSC)"
        title="Detalhe da parceria"
        actions={
          <Link className="br-button secondary" to="/convenios/parcerias">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ParceriaDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(parceria) => {
          const bloqueada = parceriaBloqueada(parceria.situacao);
          return (
            <>
              {bloqueada && (
                <Alert variant="danger" title="Parceria inadimplente" className="mb-4">
                  Esta parceria está <strong>Inadimplente</strong>. Enquanto a inadimplência
                  persistir, <strong>novos repasses ficam BLOQUEADOS</strong> (B-INV-9 — LRF
                  art. 48). É necessário sanar a pendência para retomar a execução.
                </Alert>
              )}

              <Card
                className="mb-4"
                accent={bloqueada ? 'danger' : undefined}
                header={
                  <div className="d-flex justify-content-between align-items-center">
                    <strong>{parceria.oscRazaoSocial}</strong>
                    <Tag variant={situacaoParceriaTag(parceria.situacao)}>
                      {situacaoParceriaLabel(parceria.situacao)}
                    </Tag>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="OSC (CNPJ)">{parceria.oscCnpj}</Campo>
                  <Campo rotulo="Instrumento">{parceria.tipoInstrumento}</Campo>
                  <Campo rotulo="Forma de seleção">{parceria.formaSelecao}</Campo>
                  <Campo rotulo="Valor global">{formatarMoeda(parceria.valorGlobal)}</Campo>
                  <Campo rotulo="Vigência">
                    {parceria.vigenciaInicio
                      ? `${formatarData(parceria.vigenciaInicio)} a ${formatarData(parceria.vigenciaFim)}`
                      : '—'}
                  </Campo>
                  <Campo rotulo="Prestação de contas">
                    {parceria.prestacaoSituacao ?? 'Não aberta'}
                  </Campo>
                  <Campo rotulo="Repasses">
                    {bloqueada ? (
                      <Tag variant="danger">
                        <i className="fas fa-ban" aria-hidden="true" /> Bloqueados
                      </Tag>
                    ) : (
                      <Tag variant="info">Liberáveis conforme cronograma</Tag>
                    )}
                  </Campo>
                </dl>
              </Card>

              <Card header={<strong>Repasses (cronograma de desembolso)</strong>}>
                <DataTable
                  caption={`Repasses da parceria com ${parceria.oscRazaoSocial}`}
                  columns={COLUNAS_REPASSE}
                  rows={parceria.repasses}
                  rowKey={(r) => String(r.numeroOrdem)}
                  empty={
                    <EmptyState
                      icon="fas fa-money-bill-transfer"
                      title="Nenhum repasse"
                      description="Ainda não há repasses no plano de trabalho desta parceria."
                    />
                  }
                />
              </Card>
            </>
          );
        }}
      </QueryState>
    </>
  );
}
