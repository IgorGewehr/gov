// Tela de DETALHE de um Processo (query ObterProcessoPorNup). Param de rota (NUP) ->
// useQuery + QueryState para loading/erro. Expõe CADA command de transição como botão
// de ação, habilitado conforme as guardas da máquina de estados (em andamento /
// sobrestado / terminal), abrindo o respectivo Modal+form WIRED.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useProcessoPorNup } from './processo.api';
import type { ProcessoDetalhe } from './processo.api';
import {
  NIVEL_ACESSO_LABEL,
  SITUACAO_LABEL,
  arquivado,
  emAndamento,
  nivelAcessoTagVariant,
  situacaoTagVariant,
  sobrestado,
} from './processo.helpers';
import {
  ArquivarProcessoModal,
  DespacharProcessoModal,
  SobrestarProcessoModal,
  TramitarProcessoModal,
} from './ProcessoAcaoModals';

type AcaoAberta = 'tramitar' | 'despachar' | 'sobrestar' | 'arquivar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function ProcessoDetailPage() {
  const { nup = '' } = useParams<{ nup: string }>();
  const query = useProcessoPorNup(nup);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  return (
    <>
      <PageHeader
        title="Detalhe do processo"
        description={nup ? `NUP ${nup}` : undefined}
        actions={
          <Link className="br-button secondary" to="/protocolo">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ProcessoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(processo) => {
          const podeAndamento = emAndamento(processo.situacao);
          const eTerminal = arquivado(processo.situacao);
          const eSobrestado = sobrestado(processo.situacao);

          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center flex-wrap">
                    <strong>NUP {processo.nup}</strong>
                    <Tag variant={situacaoTagVariant(processo.situacao)}>
                      {SITUACAO_LABEL[processo.situacao]}
                    </Tag>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="Classificação documental">{processo.classificacao}</Campo>
                  <Campo rotulo="Nível de acesso">
                    <Tag variant={nivelAcessoTagVariant(processo.nivelAcesso)}>
                      {NIVEL_ACESSO_LABEL[processo.nivelAcesso]}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Data de autuação">{formatarData(processo.dataAutuacao)}</Campo>
                  <Campo rotulo="Setor atual">{processo.setorAtualId ?? '—'}</Campo>
                  <Campo rotulo="Módulo originador">{processo.origemModulo ?? '—'}</Campo>
                  <Campo rotulo="Identificador do processo">{processo.id}</Campo>
                </dl>
              </Card>

              {eTerminal && (
                <Alert variant="info" title="Processo arquivado.">
                  Estado terminal: não admite novas transições de andamento. A guarda segue a Tabela
                  de Temporalidade (TTD/CONARQ).
                </Alert>
              )}

              {eSobrestado && (
                <Alert variant="warning" title="Processo sobrestado.">
                  O andamento está suspenso. É necessário reativar o processo antes de tramitar,
                  despachar ou arquivar.
                </Alert>
              )}

              <Can permission="protocolo.gerenciar">
              <Card header={<strong>Ações</strong>}>
                <div className="d-flex gap-2 flex-wrap">
                  <Button
                    variant="primary"
                    onClick={() => setAcao('tramitar')}
                    disabled={!podeAndamento}
                  >
                    <i className="fas fa-arrow-right-arrow-left" aria-hidden="true" /> Tramitar
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => setAcao('despachar')}
                    disabled={!podeAndamento}
                  >
                    <i className="fas fa-feather-pointed" aria-hidden="true" /> Despachar
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => setAcao('sobrestar')}
                    disabled={!podeAndamento}
                  >
                    <i className="fas fa-pause" aria-hidden="true" /> Sobrestar
                  </Button>
                  <Button
                    variant="danger"
                    onClick={() => setAcao('arquivar')}
                    disabled={eTerminal || eSobrestado}
                  >
                    <i className="fas fa-box-archive" aria-hidden="true" /> Arquivar
                  </Button>
                </div>
              </Card>
              </Can>

              <TramitarProcessoModal
                open={acao === 'tramitar'}
                onClose={() => setAcao(null)}
                processoId={processo.id}
                nup={processo.nup}
              />
              <DespacharProcessoModal
                open={acao === 'despachar'}
                onClose={() => setAcao(null)}
                processoId={processo.id}
                nup={processo.nup}
              />
              <SobrestarProcessoModal
                open={acao === 'sobrestar'}
                onClose={() => setAcao(null)}
                processoId={processo.id}
                nup={processo.nup}
              />
              <ArquivarProcessoModal
                open={acao === 'arquivar'}
                onClose={() => setAcao(null)}
                processoId={processo.id}
                nup={processo.nup}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
