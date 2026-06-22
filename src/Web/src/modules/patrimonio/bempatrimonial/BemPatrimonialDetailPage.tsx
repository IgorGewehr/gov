// Tela de DETALHE do bem patrimonial. Cobre as queries:
//   - ObterBemPatrimonial (detalhe);
//   - ListarMovimentacoesDoBem (movimentações via DataTable + ordenação).
// E expõe TODAS as transições de estado como ações WIRED (botão -> Modal + form),
// habilitadas conforme a máquina de estados (situação corrente).
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
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useBemPatrimonial, useMovimentacoesDoBem } from './bempatrimonial.api';
import type { BemPatrimonialDetalhe, MovimentacaoResumo, SituacaoBemPatrimonial } from './bempatrimonial.api';
import { ativoNoAcervo, encerrado, situacaoLabel, situacaoTagVariant } from './bemPatrimonial.helpers';
import {
  AlienarBemModal,
  BaixarBemModal,
  CederBemModal,
  DepreciarBemModal,
  RegistrarImpairmentModal,
  ReavaliarBemModal,
  TombarBemModal,
  TransferirBemModal,
} from './BemPatrimonialAcoesModais';

type Acao = 'tombar' | 'depreciar' | 'reavaliar' | 'impairment' | 'transferir' | 'ceder' | 'baixar' | 'alienar';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

/** Botões de ação habilitados conforme a situação corrente (máquina de estados). */
function AcoesBem({
  situacao,
  onAcao,
}: {
  situacao: SituacaoBemPatrimonial;
  onAcao: (acao: Acao) => void;
}) {
  const emIncorporacao = situacao === 'EmIncorporacao';
  const tombado = situacao === 'Tombado';
  const ativo = ativoNoAcervo(situacao);
  const fim = encerrado(situacao);

  if (fim) {
    return (
      <p className="text-gray-60 text-down-01 mb-0">
        Bem em situação terminal ({situacaoLabel(situacao)}). Não admite novas transições.
      </p>
    );
  }

  return (
    <Can permission="patrimonio.gerenciar">
    <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
      {emIncorporacao && (
        <Button variant="primary" onClick={() => onAcao('tombar')}>
          <i className="fas fa-tag" aria-hidden="true" /> Tombar
        </Button>
      )}
      {tombado && (
        <Button variant="secondary" onClick={() => onAcao('depreciar')}>
          <i className="fas fa-arrow-trend-down" aria-hidden="true" /> Depreciar
        </Button>
      )}
      {ativo && (
        <Button variant="secondary" onClick={() => onAcao('reavaliar')}>
          <i className="fas fa-scale-balanced" aria-hidden="true" /> Reavaliar
        </Button>
      )}
      {ativo && (
        <Button variant="secondary" onClick={() => onAcao('impairment')}>
          <i className="fas fa-arrow-down-wide-short" aria-hidden="true" /> Impairment
        </Button>
      )}
      {ativo && (
        <Button variant="secondary" onClick={() => onAcao('transferir')}>
          <i className="fas fa-right-left" aria-hidden="true" /> Transferir
        </Button>
      )}
      {tombado && (
        <Button variant="secondary" onClick={() => onAcao('ceder')}>
          <i className="fas fa-handshake" aria-hidden="true" /> Ceder
        </Button>
      )}
      {ativo && (
        <Button variant="danger" onClick={() => onAcao('baixar')}>
          <i className="fas fa-box-archive" aria-hidden="true" /> Baixar
        </Button>
      )}
      {ativo && (
        <Button variant="danger" onClick={() => onAcao('alienar')}>
          <i className="fas fa-gavel" aria-hidden="true" /> Alienar
        </Button>
      )}
    </div>
    </Can>
  );
}

function Movimentacoes({ bemId }: { bemId: string }) {
  const query = useMovimentacoesDoBem(bemId);

  const columns: Column<MovimentacaoResumo>[] = [
    {
      key: 'data',
      header: 'Data',
      sortAccessor: (m) => m.data,
      render: (m) => formatarData(m.data),
    },
    { key: 'origem', header: 'Origem', render: (m) => m.localizacaoOrigem || '—' },
    { key: 'destino', header: 'Destino', render: (m) => m.localizacaoDestino || '—' },
    { key: 'responsavel', header: 'Responsável', render: (m) => m.responsavelId },
  ];

  return (
    <DataTable
      caption="Movimentações patrimoniais do bem"
      columns={columns}
      rows={query.data}
      rowKey={(m) => m.id}
      loading={query.isLoading}
      error={query.isError ? errorMessage(query.error) : null}
      empty={
        <EmptyState
          icon="fas fa-route"
          title="Nenhuma movimentação"
          description="Este bem ainda não registrou transferências de localização ou responsável."
        />
      }
    />
  );
}

export function BemPatrimonialDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useBemPatrimonial(id);
  const [acao, setAcao] = useState<Acao | null>(null);

  const fechar = (): void => setAcao(null);

  return (
    <>
      <PageHeader
        title="Detalhe do bem patrimonial"
        actions={
          <Link className="br-button secondary" to="/patrimonio">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<BemPatrimonialDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(bem) => (
          <>
            <Card
              className="mb-4"
              header={<strong>{bem.descricao}</strong>}
            >
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoTagVariant(bem.situacao)}>{situacaoLabel(bem.situacao)}</Tag>
                </Campo>
                <Campo rotulo="Número de tombo">{bem.numeroTombamento ?? '—'}</Campo>
                <Campo rotulo="Tipo">{bem.tipo}</Campo>
                <Campo rotulo="Vida útil (meses)">{bem.vidaUtilMeses}</Campo>
                <Campo rotulo="Valor inicial">{formatarMoeda(bem.valorInicial)}</Campo>
                <Campo rotulo="Valor residual">{formatarMoeda(bem.valorResidual)}</Campo>
                <Campo rotulo="Valor contábil">{formatarMoeda(bem.valorContabil)}</Campo>
                <Campo rotulo="Data de incorporação">{formatarData(bem.dataIncorporacao)}</Campo>
              </dl>
            </Card>

            <Card className="mb-4" header={<strong>Ações</strong>}>
              <AcoesBem situacao={bem.situacao} onAcao={setAcao} />
            </Card>

            <Card header={<strong>Movimentações</strong>}>
              <Movimentacoes bemId={bem.id} />
            </Card>

            {/* Modais de ação por command — montados sob demanda. */}
            <TombarBemModal bemId={bem.id} open={acao === 'tombar'} onClose={fechar} />
            <DepreciarBemModal bemId={bem.id} open={acao === 'depreciar'} onClose={fechar} />
            <ReavaliarBemModal bemId={bem.id} open={acao === 'reavaliar'} onClose={fechar} />
            <RegistrarImpairmentModal bemId={bem.id} open={acao === 'impairment'} onClose={fechar} />
            <TransferirBemModal bemId={bem.id} open={acao === 'transferir'} onClose={fechar} />
            <CederBemModal bemId={bem.id} open={acao === 'ceder'} onClose={fechar} />
            <BaixarBemModal bemId={bem.id} open={acao === 'baixar'} onClose={fechar} />
            <AlienarBemModal bemId={bem.id} open={acao === 'alienar'} onClose={fechar} />
          </>
        )}
      </QueryState>
    </>
  );
}
