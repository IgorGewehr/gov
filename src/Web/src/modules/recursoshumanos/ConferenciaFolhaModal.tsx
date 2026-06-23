// Tela de CONFERÊNCIA de pré-fechamento da folha (P0-7). ANTES de fechar, o conferente
// vê os TOTAIS (geral + por rubrica) e as DIVERGÊNCIAS: (a) ativos sem lançamento,
// (b) líquido insuficiente (P0-5), (c) variação suspeita vs competência anterior, (d)
// totais não conferem. Cores gov.br (Alert/Tag) — nunca só cor. Somente leitura.
//
// O botão "Fechar competência" deixa claro o que confirma. Havendo líquido insuficiente,
// exige a confirmação EXPLÍCITA (checkbox); o backend recusa sem ela (422), que é tratado
// aqui mostrando a divergência. Contrato: ConferenciaFolha em ./folha.api.
import { useState } from 'react';
import {
  Alert,
  Button,
  DataTable,
  EmptyState,
  Modal,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { ApiError } from '../../api/problemDetails';
import { useConferenciaFolha } from './folha.api';
import type {
  ConferenciaFolha,
  ServidorLiquidoInsuficiente,
  ServidorSemLancamento,
  ServidorVariacaoLiquido,
} from './folha.api';

export interface ConferenciaFolhaModalProps {
  open: boolean;
  onClose: () => void;
  folhaId: string;
  /** Fecha a competência confirmando (ou não) o líquido insuficiente. */
  onConfirmarFechamento: (confirmarLiquidoInsuficiente: boolean) => void;
  /** Indica fechamento em andamento (bloqueia o botão). */
  fechando?: boolean;
  /** Erro do fechamento (ex.: 422 sem confirmação) para exibir no rodapé. */
  erroFechamento?: unknown;
}

function formatarPercentual(fracao: number | null): string {
  if (fracao === null) return '—';
  return `${fracao.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;
}

export function ConferenciaFolhaModal({
  open,
  onClose,
  folhaId,
  onConfirmarFechamento,
  fechando = false,
  erroFechamento,
}: ConferenciaFolhaModalProps) {
  const query = useConferenciaFolha(folhaId, open);
  const [confirmou, setConfirmou] = useState(false);

  const colsSemLancamento: Column<ServidorSemLancamento>[] = [
    { key: 'matricula', header: 'Matrícula', render: (s) => s.matricula },
    { key: 'nome', header: 'Servidor', render: (s) => s.nome },
  ];

  const colsLiquido: Column<ServidorLiquidoInsuficiente>[] = [
    { key: 'matricula', header: 'Matrícula', render: (s) => s.matricula || '—' },
    { key: 'nome', header: 'Servidor', render: (s) => s.nome || '(não localizado)' },
    {
      key: 'proventos',
      header: 'Proventos',
      align: 'end',
      render: (s) => formatarMoeda(s.totalProventos),
    },
    {
      key: 'descontos',
      header: 'Descontos',
      align: 'end',
      render: (s) => formatarMoeda(s.totalDescontos),
    },
  ];

  const colsVariacao: Column<ServidorVariacaoLiquido>[] = [
    { key: 'matricula', header: 'Matrícula', render: (s) => s.matricula || '—' },
    { key: 'nome', header: 'Servidor', render: (s) => s.nome || '(não localizado)' },
    {
      key: 'anterior',
      header: 'Líquido anterior',
      align: 'end',
      render: (s) => formatarMoeda(s.liquidoAnterior),
    },
    {
      key: 'atual',
      header: 'Líquido atual',
      align: 'end',
      render: (s) => formatarMoeda(s.liquidoAtual),
    },
    {
      key: 'variacao',
      header: 'Variação',
      align: 'end',
      render: (s) => (
        <Tag variant={s.variacao < 0 ? 'danger' : 'warning'}>
          {formatarMoeda(s.variacao)} ({formatarPercentual(s.variacaoPercentual)})
        </Tag>
      ),
    },
  ];

  function fechar(): void {
    setConfirmou(false);
    onClose();
  }

  function renderConteudo(c: ConferenciaFolha) {
    const temLiquidoInsuficiente = c.servidoresComLiquidoInsuficiente.length > 0;
    const exigeConfirmacao = temLiquidoInsuficiente;
    const podeFechar = !fechando && (!exigeConfirmacao || confirmou);
    const erroApi = erroFechamento instanceof ApiError ? erroFechamento.userMessage : null;

    return (
      <>
        {!c.temDivergencias ? (
          <Alert variant="success" title="Sem divergências.">
            A folha está pronta para fechamento. Revise os totais abaixo.
          </Alert>
        ) : (
          <Alert variant="warning" title="Divergências encontradas.">
            Revise os itens sinalizados antes de fechar a competência.
          </Alert>
        )}

        <dl className="row mt-3">
          <div className="col-sm-4 mb-2">
            <dt className="text-gray-60 text-down-01">Total de proventos</dt>
            <dd className="mb-0 text-semi-bold">{formatarMoeda(c.totalProventos)}</dd>
          </div>
          <div className="col-sm-4 mb-2">
            <dt className="text-gray-60 text-down-01">Total de descontos</dt>
            <dd className="mb-0 text-semi-bold">{formatarMoeda(c.totalDescontos)}</dd>
          </div>
          <div className="col-sm-4 mb-2">
            <dt className="text-gray-60 text-down-01">Total líquido</dt>
            <dd className="mb-0 text-semi-bold">{formatarMoeda(c.totalLiquido)}</dd>
          </div>
          <div className="col-sm-4 mb-2">
            <dt className="text-gray-60 text-down-01">Servidores</dt>
            <dd className="mb-0 text-semi-bold">{c.quantidadeServidores}</dd>
          </div>
          <div className="col-sm-4 mb-2">
            <dt className="text-gray-60 text-down-01">Lançamentos</dt>
            <dd className="mb-0 text-semi-bold">{c.quantidadeLancamentos}</dd>
          </div>
        </dl>

        {!c.totaisConferem && (
          <Alert variant="danger" title="Totais não conferem.">
            O líquido geral não corresponde a proventos menos descontos. Recalcule a folha.
          </Alert>
        )}

        {c.servidoresAtivosSemLancamento.length > 0 && (
          <section className="mt-4" aria-label="Servidores sem lançamento">
            <h3 className="text-up-01 mb-2">
              <i className="fas fa-user-slash text-warning" aria-hidden="true" /> Ativos sem
              lançamento ({c.servidoresAtivosSemLancamento.length})
            </h3>
            <DataTable
              caption="Servidores ativos sem nenhum lançamento na folha"
              columns={colsSemLancamento}
              rows={c.servidoresAtivosSemLancamento}
              rowKey={(s) => s.servidorId}
            />
          </section>
        )}

        {temLiquidoInsuficiente && (
          <section className="mt-4" aria-label="Servidores com líquido insuficiente">
            <h3 className="text-up-01 mb-2">
              <i className="fas fa-triangle-exclamation text-danger" aria-hidden="true" /> Líquido
              insuficiente ({c.servidoresComLiquidoInsuficiente.length})
            </h3>
            <DataTable
              caption="Servidores cujos descontos consomem os proventos (P0-5)"
              columns={colsLiquido}
              rows={c.servidoresComLiquidoInsuficiente}
              rowKey={(s) => s.servidorId}
            />
          </section>
        )}

        {c.servidoresComVariacaoSuspeita.length > 0 && (
          <section className="mt-4" aria-label="Servidores com variação suspeita">
            <h3 className="text-up-01 mb-2">
              <i className="fas fa-arrow-trend-up text-warning" aria-hidden="true" /> Variação vs{' '}
              {c.competenciaAnterior ?? 'mês anterior'} (|Δ| &gt;{' '}
              {formatarMoeda(c.limiteVariacaoLiquido)}) — {c.servidoresComVariacaoSuspeita.length}
            </h3>
            <DataTable
              caption="Servidores com variação de líquido acima do limiar frente à competência anterior"
              columns={colsVariacao}
              rows={c.servidoresComVariacaoSuspeita}
              rowKey={(s) => s.servidorId}
            />
          </section>
        )}

        {exigeConfirmacao && (
          <div className="br-checkbox mt-4">
            <input
              id="confirmar-liquido-insuficiente"
              type="checkbox"
              checked={confirmou}
              onChange={(e) => setConfirmou(e.target.checked)}
            />
            <label htmlFor="confirmar-liquido-insuficiente">
              Revisei a folha (margem/consignados) e confirmo o fechamento mesmo com líquido
              insuficiente.
            </label>
          </div>
        )}

        {erroApi && (
          <div className="mt-3">
            <Alert variant="danger" title="Fechamento não concluído.">
              {erroApi}
            </Alert>
          </div>
        )}

        <div className="d-flex flex-wrap justify-content-end mt-4" style={{ gap: '1rem' }}>
          <Button variant="secondary" onClick={fechar} disabled={fechando}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={() => onConfirmarFechamento(confirmou)}
            loading={fechando}
            disabled={!podeFechar}
          >
            <i className="fas fa-lock" aria-hidden="true" /> Fechar competência {c.competencia}
          </Button>
        </div>
        <p className="text-down-01 text-gray-60 mt-2 mb-0">
          O fechamento é irreversível: gera os eventos do eSocial e a despesa de pessoal. Confira os
          totais e as divergências acima antes de confirmar.
        </p>
      </>
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Conferência de pré-fechamento da folha"
      size="large"
    >
      <QueryState<ConferenciaFolha>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-file-circle-question"
            title="Conferência indisponível"
            description="Não foi possível carregar a conferência desta folha."
          />
        }
      >
        {(c) => renderConteudo(c)}
      </QueryState>
    </Modal>
  );
}
