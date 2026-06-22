// Tela de DETALHE de uma Solicitação de Regulação. Padrão-ouro: param de rota ->
// useQuery, QueryState para loading/erro, layout de Card com pares rótulo/valor.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useSolicitacaoRegulacao } from './api';
import type { SolicitacaoRegulacaoDetalhe } from './api';
import {
  prioridadeVariant,
  regulacaoAutorizada,
  regulacaoEmAnalise,
  situacaoRegulacaoVariant,
} from './saude.helpers';
import {
  AutorizarSolicitacaoModal,
  ExecutarSolicitacaoModal,
  MotivoRegulacaoModal,
} from './RegulacaoAcaoModals';

type AcaoRegulacao = 'autorizar' | 'negar' | 'devolver' | 'executar' | 'cancelar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function RegulacaoDetailPage() {
  const { solicitacaoId = '' } = useParams<{ solicitacaoId: string }>();
  const query = useSolicitacaoRegulacao(solicitacaoId);
  const [acao, setAcao] = useState<AcaoRegulacao>(null);

  return (
    <>
      <PageHeader
        title="Detalhe da solicitação de regulação"
        actions={
          <Link className="br-button secondary" to="/saude/regulacao">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à fila
          </Link>
        }
      />

      <QueryState<SolicitacaoRegulacaoDetalhe | null>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-share-from-square"
            title="Solicitação não encontrada"
            description="A solicitação informada não existe ou não está disponível."
          />
        }
      >
        {(solicitacao) =>
          solicitacao === null ? (
            <EmptyState
              icon="fas fa-share-from-square"
              title="Solicitação não encontrada"
              description="A solicitação informada não existe ou não está disponível."
            />
          ) : (
            <>
              <Card className="mb-4" header={<strong>Procedimento {solicitacao.codigoSigtap}</strong>}>
                <dl className="row">
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoRegulacaoVariant(solicitacao.situacao)}>
                      {solicitacao.situacao}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Prioridade">
                    <Tag variant={prioridadeVariant(solicitacao.prioridade)}>
                      {solicitacao.prioridade}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Código SIGTAP">{solicitacao.codigoSigtap}</Campo>
                  <Campo rotulo="Procedimento">{solicitacao.descricaoProcedimento}</Campo>
                  <Campo rotulo="Data da solicitação">{formatarData(solicitacao.dataSolicitacao)}</Campo>
                  <Campo rotulo="Data da autorização">
                    {solicitacao.dataAutorizacao ? formatarData(solicitacao.dataAutorizacao) : '—'}
                  </Campo>
                  <Campo rotulo="Protocolo SISREG">{solicitacao.protocoloSisreg ?? '—'}</Campo>
                  <Campo rotulo="Paciente">{solicitacao.pacienteId}</Campo>
                </dl>
              </Card>

              <Can permission="saude.gerenciar">
                <Card header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="primary"
                      onClick={() => setAcao('autorizar')}
                      disabled={!regulacaoEmAnalise(solicitacao.situacao)}
                    >
                      <i className="fas fa-check" aria-hidden="true" /> Autorizar
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('devolver')}
                      disabled={!regulacaoEmAnalise(solicitacao.situacao)}
                    >
                      <i className="fas fa-rotate-left" aria-hidden="true" /> Devolver
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('executar')}
                      disabled={!regulacaoAutorizada(solicitacao.situacao)}
                    >
                      <i className="fas fa-flag-checkered" aria-hidden="true" /> Registrar execução
                    </Button>
                    <Button
                      variant="danger"
                      onClick={() => setAcao('negar')}
                      disabled={!regulacaoEmAnalise(solicitacao.situacao)}
                    >
                      <i className="fas fa-xmark" aria-hidden="true" /> Negar
                    </Button>
                    <Button
                      variant="danger"
                      onClick={() => setAcao('cancelar')}
                      disabled={!regulacaoEmAnalise(solicitacao.situacao)}
                    >
                      <i className="fas fa-ban" aria-hidden="true" /> Cancelar
                    </Button>
                  </div>
                </Card>
              </Can>

              <AutorizarSolicitacaoModal
                open={acao === 'autorizar'}
                onClose={() => setAcao(null)}
                solicitacaoId={solicitacao.id}
              />
              <ExecutarSolicitacaoModal
                open={acao === 'executar'}
                onClose={() => setAcao(null)}
                solicitacaoId={solicitacao.id}
              />
              <MotivoRegulacaoModal
                open={acao === 'negar'}
                onClose={() => setAcao(null)}
                solicitacaoId={solicitacao.id}
                tipo="negar"
              />
              <MotivoRegulacaoModal
                open={acao === 'devolver'}
                onClose={() => setAcao(null)}
                solicitacaoId={solicitacao.id}
                tipo="devolver"
              />
              <MotivoRegulacaoModal
                open={acao === 'cancelar'}
                onClose={() => setAcao(null)}
                solicitacaoId={solicitacao.id}
                tipo="cancelar"
              />
            </>
          )
        }
      </QueryState>
    </>
  );
}
