// Tela de DETALHE de uma Licitacao + central de AÇÕES (transições da máquina de
// estados). Cada command vira um botão gated pela situação atual, abrindo o modal
// correspondente. Também exibe lotes e a tabela de propostas (ListarPropostasDaLicitacao).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { useLicitacao, usePropostasDaLicitacao, situacaoEncerrada } from './licitacao.api';
import type { LicitacaoDetalhe, PropostaResumo } from './licitacao.api';
import {
  CRITERIO_LABEL,
  MODALIDADE_LABEL,
  SITUACAO_LABEL,
  SITUACAO_PROPOSTA_LABEL,
  propostaTagVariant,
  situacaoTagVariant,
} from './licitacao.helpers';
import {
  AnularLicitacaoModal,
  DeclararDesertaModal,
  DeclararFracassadaModal,
  HabilitarLicitanteModal,
  HomologarLicitacaoModal,
  JulgarPropostasModal,
  PublicarEditalPncpModal,
  RevogarLicitacaoModal,
} from './LicitacaoActionModals';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type AcaoAberta =
  | 'pncp'
  | 'julgar'
  | 'habilitar'
  | 'homologar'
  | 'deserta'
  | 'fracassada'
  | 'revogar'
  | 'anular'
  | null;

export function LicitacaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useLicitacao(id);
  const propostasQuery = usePropostasDaLicitacao(id);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  const fechar = () => setAcao(null);

  const propostaColumns: Column<PropostaResumo>[] = [
    {
      key: 'fornecedor',
      header: 'Fornecedor',
      sortAccessor: (p) => p.fornecedorId,
      render: (p) => `${p.fornecedorId.slice(0, 8)}…`,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (p) => p.valor,
      render: (p) => formatarMoeda(p.valor),
    },
    {
      key: 'classificacao',
      header: 'Classificação',
      align: 'center',
      sortAccessor: (p) => p.classificacao ?? Number.MAX_SAFE_INTEGER,
      render: (p) => (p.classificacao == null ? '—' : `${p.classificacao}º`),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={propostaTagVariant(p.situacao)}>{SITUACAO_PROPOSTA_LABEL[p.situacao]}</Tag>,
    },
  ];

  return (
    <>
      <PageHeader
        title="Detalhe da licitação"
        actions={
          <Link className="br-button secondary" to="/administracao">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<LicitacaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(licitacao) => {
          const encerrada = situacaoEncerrada(licitacao.situacao);
          const emAndamento = licitacao.situacao === 'Aberta' || licitacao.situacao === 'EmJulgamento';
          const propostas = propostasQuery.data ?? licitacao.propostas;
          const fornecedores = Array.from(new Set(propostas.map((p) => p.fornecedorId)));

          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center">
                    <strong>{licitacao.objeto}</strong>
                    <Tag variant={situacaoTagVariant(licitacao.situacao)}>{SITUACAO_LABEL[licitacao.situacao]}</Tag>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="Modalidade">{MODALIDADE_LABEL[licitacao.modalidade]}</Campo>
                  <Campo rotulo="Critério de julgamento">{CRITERIO_LABEL[licitacao.criterioJulgamento]}</Campo>
                  <Campo rotulo="Valor estimado">{formatarMoeda(licitacao.valorEstimado)}</Campo>
                  <Campo rotulo="Edital no PNCP">{licitacao.numeroEditalPncp ?? '—'}</Campo>
                  <Campo rotulo="Proposta vencedora">
                    {licitacao.propostaVencedoraId ? `${licitacao.propostaVencedoraId.slice(0, 8)}…` : '—'}
                  </Campo>
                </dl>
              </Card>

              {/* Central de ações — gated por "administracao.gerenciar" (§6) e pela
                  máquina de estados (§4). */}
              <Can permission="administracao.gerenciar">
              <Card className="mb-4" header={<strong>Ações</strong>}>
                {encerrada ? (
                  <p className="mb-0 text-gray-60">
                    Certame encerrado ({SITUACAO_LABEL[licitacao.situacao]}). Estados terminais não admitem novas
                    transições (I-12).
                  </p>
                ) : (
                  <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                    {licitacao.situacao === 'Aberta' && (
                      <>
                        <Button variant="secondary" onClick={() => setAcao('pncp')}>
                          <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar no PNCP
                        </Button>
                        <Button variant="primary" onClick={() => setAcao('julgar')}>
                          <i className="fas fa-gavel" aria-hidden="true" /> Julgar propostas
                        </Button>
                        <Button variant="secondary" onClick={() => setAcao('deserta')}>
                          <i className="fas fa-user-slash" aria-hidden="true" /> Declarar deserta
                        </Button>
                      </>
                    )}

                    {emAndamento && (
                      <Button variant="secondary" onClick={() => setAcao('habilitar')}>
                        <i className="fas fa-user-check" aria-hidden="true" /> Habilitar licitante
                      </Button>
                    )}

                    {licitacao.situacao === 'EmJulgamento' && (
                      <Button variant="primary" onClick={() => setAcao('homologar')}>
                        <i className="fas fa-stamp" aria-hidden="true" /> Homologar
                      </Button>
                    )}

                    {emAndamento && (
                      <>
                        <Button variant="secondary" onClick={() => setAcao('fracassada')}>
                          <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Declarar fracassada
                        </Button>
                        <Button variant="danger" onClick={() => setAcao('revogar')}>
                          <i className="fas fa-ban" aria-hidden="true" /> Revogar
                        </Button>
                        <Button variant="danger" onClick={() => setAcao('anular')}>
                          <i className="fas fa-circle-xmark" aria-hidden="true" /> Anular
                        </Button>
                      </>
                    )}
                  </div>
                )}
              </Card>
              </Can>

              {/* Lotes */}
              <Card className="mb-4" header={<strong>Lotes</strong>}>
                {licitacao.lotes.length === 0 ? (
                  <EmptyState icon="fas fa-boxes-stacked" title="Nenhum lote cadastrado" />
                ) : (
                  <DataTable
                    caption="Lotes do certame"
                    columns={[
                      { key: 'numero', header: 'Nº', align: 'center', render: (l) => l.numero },
                      { key: 'descricao', header: 'Descrição', render: (l) => l.descricao },
                      {
                        key: 'valor',
                        header: 'Valor estimado',
                        align: 'end',
                        render: (l) => formatarMoeda(l.valorEstimado),
                      },
                    ]}
                    rows={licitacao.lotes}
                    rowKey={(l) => l.loteId}
                  />
                )}
              </Card>

              {/* Propostas (ListarPropostasDaLicitacao) */}
              <Card header={<strong>Propostas recebidas</strong>}>
                <DataTable
                  caption="Propostas da licitação"
                  columns={propostaColumns}
                  rows={propostas}
                  rowKey={(p) => p.propostaId}
                  loading={propostasQuery.isLoading}
                  error={propostasQuery.isError ? errorMessage(propostasQuery.error) : null}
                  empty={<EmptyState icon="fas fa-inbox" title="Nenhuma proposta recebida" />}
                />
              </Card>

              {/* Modais de ação */}
              <PublicarEditalPncpModal open={acao === 'pncp'} onClose={fechar} licitacaoId={licitacao.id} />
              <JulgarPropostasModal
                open={acao === 'julgar'}
                onClose={fechar}
                licitacaoId={licitacao.id}
                propostas={propostas}
              />
              <HabilitarLicitanteModal
                open={acao === 'habilitar'}
                onClose={fechar}
                licitacaoId={licitacao.id}
                fornecedores={fornecedores}
              />
              <HomologarLicitacaoModal
                open={acao === 'homologar'}
                onClose={fechar}
                licitacaoId={licitacao.id}
                licitacao={licitacao}
              />
              <DeclararDesertaModal
                open={acao === 'deserta'}
                onClose={fechar}
                licitacaoId={licitacao.id}
                temPropostas={propostas.length > 0}
              />
              <DeclararFracassadaModal open={acao === 'fracassada'} onClose={fechar} licitacaoId={licitacao.id} />
              <RevogarLicitacaoModal open={acao === 'revogar'} onClose={fechar} licitacaoId={licitacao.id} />
              <AnularLicitacaoModal open={acao === 'anular'} onClose={fechar} licitacaoId={licitacao.id} />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
