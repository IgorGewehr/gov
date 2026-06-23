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
import {
  useApurarDescontosLegais,
  useCalcularFolha,
  useFecharFolha,
  useFolhaPorCompetencia,
} from './api';
import type { FolhaResumo } from './api';
import { PERM_RH_GERENCIAR, situacaoFolhaTagVariant } from './recursosHumanos.helpers';
import { AdicionarEventoFormModal } from './AdicionarEventoFormModal';
import { EfetuarPagamentoFormModal } from './EfetuarPagamentoFormModal';
import { ContrachequeModal } from './ContrachequeModal';
import { ConferenciaFolhaModal } from './ConferenciaFolhaModal';

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
  const [conferenciaAberta, setConferenciaAberta] = useState(false);

  const temCompetencia = Number.isInteger(state.ano) && Number.isInteger(state.mes);
  const query = useFolhaPorCompetencia(state.ano ?? 0, state.mes ?? 0, temCompetencia);

  const apurar = useApurarDescontosLegais();
  const calcular = useCalcularFolha();
  const fechar = useFecharFolha();

  function executarApuracao(): void {
    apurar.mutate(folhaId, {
      onSuccess: () => toast.success('Descontos legais (INSS/RPPS/IRRF) apurados.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError
            ? error.userMessage
            : 'Não foi possível apurar os descontos legais.',
        ),
    });
  }

  function executarCalculo(): void {
    calcular.mutate(folhaId, {
      onSuccess: () => toast.success('Folha calculada.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível calcular a folha.',
        ),
    });
  }

  // P0-7: o fechamento passa pela conferência. Confirmar dispara a mutation; havendo líquido
  // insuficiente sem confirmação o backend recusa (422) e o erro fica visível no próprio modal,
  // que permanece aberto para o conferente marcar a confirmação explícita.
  function confirmarFechamento(confirmarLiquidoInsuficiente: boolean): void {
    fechar.mutate(
      { folhaId, confirmarLiquidoInsuficiente },
      {
        onSuccess: () => {
          toast.success('Folha fechada.', 'Sucesso');
          fechar.reset();
          setConferenciaAberta(false);
        },
      },
    );
  }

  function abrirConferencia(): void {
    fechar.reset();
    setConferenciaAberta(true);
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
                        onClick={executarApuracao}
                        loading={apurar.isPending}
                        disabled={folha.situacao !== 'Aberta'}
                      >
                        <i className="fas fa-scale-balanced" aria-hidden="true" /> Apurar
                        descontos legais
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
                        onClick={abrirConferencia}
                        disabled={folha.situacao !== 'Calculada'}
                      >
                        <i className="fas fa-clipboard-check" aria-hidden="true" /> Conferir e fechar
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
                    Com a folha Aberta: lance os eventos, apure os descontos legais (INSS/RPPS/IRRF)
                    e calcule. Com a folha Calculada, &ldquo;Conferir e fechar&rdquo; abre a
                    conferência (totais e divergências) antes do fechamento; o pagamento exige a
                    folha Fechada.
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
                <ConferenciaFolhaModal
                  open={conferenciaAberta}
                  onClose={() => setConferenciaAberta(false)}
                  folhaId={folhaId}
                  onConfirmarFechamento={confirmarFechamento}
                  fechando={fechar.isPending}
                  erroFechamento={fechar.error}
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
