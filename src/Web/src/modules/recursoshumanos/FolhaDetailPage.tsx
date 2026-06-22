// Tela de DETALHE/operação de uma folha de pagamento. Recebe a competência via state
// do router (a partir da consulta da FolhaListPage) e revalida por competência, mantendo
// os totais vivos após as mutations (calcular/fechar) e o lançamento de eventos.
//
// O backend não expõe GET folha por id — a navegação direta sem state é tratada com um
// EmptyState orientando a consulta. Padrão-ouro: QueryState + Card + ações com mutation.
import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useCalcularFolha, useFecharFolha, useFolhaPorCompetencia } from './api';
import type { FolhaResumo } from './api';
import { PERM_RH_GERENCIAR, situacaoFolhaTagVariant } from './recursosHumanos.helpers';
import { AdicionarEventoFormModal } from './AdicionarEventoFormModal';
import { EfetuarPagamentoFormModal } from './EfetuarPagamentoFormModal';
import { ContrachequeModal } from './ContrachequeModal';

/** Competência propagada via router state (AAAA-MM partido em números). */
interface FolhaLocationState {
  ano?: number;
  mes?: number;
}

function competenciaDoTexto(competencia: string): { ano: number; mes: number } | null {
  const [ano, mes] = competencia.split('-').map(Number);
  if (!Number.isInteger(ano) || !Number.isInteger(mes)) return null;
  return { ano, mes };
}

export function FolhaDetailPage() {
  const { folhaId = '' } = useParams<{ folhaId: string }>();
  const location = useLocation();
  const state = (location.state ?? {}) as FolhaLocationState;
  const toast = useToast();

  const [eventoAberto, setEventoAberto] = useState(false);
  const [pagamentoAberto, setPagamentoAberto] = useState(false);
  const [contrachequeAberto, setContrachequeAberto] = useState(false);

  const temCompetencia = Number.isInteger(state.ano) && Number.isInteger(state.mes);
  const query = useFolhaPorCompetencia(state.ano ?? 0, state.mes ?? 0, temCompetencia);

  const calcular = useCalcularFolha();
  const fechar = useFecharFolha();

  function executarCalculo(): void {
    calcular.mutate(folhaId, {
      onSuccess: () => toast.success('Folha calculada.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível calcular a folha.',
        ),
    });
  }

  function executarFechamento(): void {
    fechar.mutate(folhaId, {
      onSuccess: () => toast.success('Folha fechada.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível fechar a folha.',
        ),
    });
  }

  return (
    <>
      <PageHeader
        title="Folha de Pagamento"
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/folhas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      {!temCompetencia ? (
        <EmptyState
          icon="fas fa-file-circle-question"
          title="Abra a folha pela consulta"
          description="Acesse a folha a partir da consulta por competência para ver o detalhe."
        />
      ) : (
        <QueryState<FolhaResumo>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-file-circle-question"
              title="Folha não encontrada"
              description="A folha desta competência não está mais disponível."
            />
          }
        >
          {(folha) => (
            <>
                <Card
                  className="mb-4"
                  header={<strong>Competência {folha.competencia}</strong>}
                >
                  <dl className="row">
                    <div className="col-sm-6 mb-3">
                      <dt className="text-gray-60 text-down-01">Situação</dt>
                      <dd className="mb-0">
                        <Tag variant={situacaoFolhaTagVariant(folha.situacao)}>{folha.situacao}</Tag>
                      </dd>
                    </div>
                    <div className="col-sm-6 mb-3">
                      <dt className="text-gray-60 text-down-01">Data de fechamento</dt>
                      <dd className="mb-0 text-semi-bold">{formatarData(folha.dataFechamento)}</dd>
                    </div>
                    <div className="col-sm-4 mb-3">
                      <dt className="text-gray-60 text-down-01">Total de proventos</dt>
                      <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalProventos)}</dd>
                    </div>
                    <div className="col-sm-4 mb-3">
                      <dt className="text-gray-60 text-down-01">Total de descontos</dt>
                      <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalDescontos)}</dd>
                    </div>
                    <div className="col-sm-4 mb-3">
                      <dt className="text-gray-60 text-down-01">Total líquido</dt>
                      <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalLiquido)}</dd>
                    </div>
                  </dl>
                </Card>

                <Card header={<strong>Operações da competência</strong>}>
                  <div className="d-flex flex-wrap" style={{ gap: '1rem' }}>
                    <Can permission={PERM_RH_GERENCIAR}>
                      <Button
                        variant="secondary"
                        onClick={() => setEventoAberto(true)}
                        disabled={folha.situacao !== 'Aberta'}
                      >
                        <i className="fas fa-plus" aria-hidden="true" /> Lançar evento
                      </Button>
                      <Button
                        variant="secondary"
                        onClick={executarCalculo}
                        loading={calcular.isPending}
                        disabled={folha.situacao !== 'Aberta'}
                      >
                        <i className="fas fa-calculator" aria-hidden="true" /> Calcular folha
                      </Button>
                      <Button
                        variant="secondary"
                        onClick={executarFechamento}
                        loading={fechar.isPending}
                        disabled={folha.situacao !== 'Calculada'}
                      >
                        <i className="fas fa-lock" aria-hidden="true" /> Fechar competência
                      </Button>
                      <Button
                        variant="primary"
                        onClick={() => setPagamentoAberto(true)}
                        disabled={folha.situacao !== 'Fechada'}
                      >
                        <i className="fas fa-money-bill-wave" aria-hidden="true" /> Efetuar pagamento
                      </Button>
                    </Can>
                    <Button variant="tertiary" onClick={() => setContrachequeAberto(true)}>
                      <i className="fas fa-receipt" aria-hidden="true" /> Ver contracheque
                    </Button>
                  </div>
                  <p className="text-down-01 text-gray-60 mt-3 mb-0">
                    Lançamentos e cálculo só são permitidos com a folha Aberta; o fechamento exige
                    a folha Calculada; o pagamento exige a folha Fechada.
                  </p>
                </Card>

                <AdicionarEventoFormModal
                  open={eventoAberto}
                  onClose={() => setEventoAberto(false)}
                  folhaId={folhaId}
                />
                <EfetuarPagamentoFormModal
                  open={pagamentoAberto}
                  onClose={() => setPagamentoAberto(false)}
                  folhaId={folhaId}
                />
                <ContrachequeModal
                  open={contrachequeAberto}
                  onClose={() => setContrachequeAberto(false)}
                  folhaId={folhaId}
                />
              </>
          )}
        </QueryState>
      )}
    </>
  );
}

// Mantém a competência disponível para a página de detalhe ao navegar.
export function competenciaParaState(folha: FolhaResumo): FolhaLocationState {
  return competenciaDoTexto(folha.competencia) ?? {};
}
