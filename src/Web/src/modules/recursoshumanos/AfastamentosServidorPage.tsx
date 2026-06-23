// AFASTAMENTOS / LICENÇAS de um servidor (Onda 1 — tipados). Lista o histórico via
// GET /recursoshumanos/servidores/{id}/afastamentos, destaca o afastamento VIGENTE e
// seu efeito na folha, e permite lançar (tipo + período), encerrar (retorno) e cancelar.
// O efeito na folha (suspende/reduz proventos, conta tempo) vem da regra do tipo no backend.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';
import { useAfastamentosDoServidor } from './afastamento.api';
import type { AfastamentoResumo } from './afastamento.api';
import {
  descreverEfeitoFolha,
  formatarSituacaoAfastamento,
  formatarTipoAfastamento,
  situacaoAfastamentoTagVariant,
} from './afastamento.helpers';
import {
  CancelarAfastamentoModal,
  EncerrarAfastamentoModal,
  LancarAfastamentoModal,
} from './AfastamentoModais';

type AcaoAlvo = { kind: 'encerrar' | 'cancelar'; afastamento: AfastamentoResumo } | null;

function periodo(a: AfastamentoResumo): string {
  const fim = a.fimEfetivo ?? a.fimPrevisto;
  const inicio = formatarData(a.inicio);
  if (!fim) return `${inicio} → (indeterminado)`;
  const rotuloFim = a.fimEfetivo ? `${formatarData(a.fimEfetivo)} (efetivo)` : `${formatarData(a.fimPrevisto)} (previsto)`;
  return `${inicio} → ${rotuloFim}`;
}

export function AfastamentosServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const query = useAfastamentosDoServidor(servidorId);
  const [lancarAberto, setLancarAberto] = useState(false);
  const [acao, setAcao] = useState<AcaoAlvo>(null);

  const colunas: Column<AfastamentoResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      render: (a) => formatarTipoAfastamento(a.tipo),
    },
    {
      key: 'periodo',
      header: 'Período',
      sortAccessor: (a) => a.inicio,
      render: (a) => periodo(a),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (a) => (
        <Tag variant={situacaoAfastamentoTagVariant(a.situacao)}>
          {formatarSituacaoAfastamento(a.situacao)}
        </Tag>
      ),
    },
    {
      key: 'efeito',
      header: 'Efeito na folha',
      render: (a) => (
        <span className="text-down-01">
          {descreverEfeitoFolha({
            suspendeProventos: a.suspendeProventos,
            percentualRemuneracao: a.percentualRemuneracao,
            diasPagosPeloEnte: a.diasPagosPeloEnte,
            contaTempo: a.contaTempo,
          })}
        </span>
      ),
    },
    { key: 'documento', header: 'Documento', render: (a) => a.documento ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (a) =>
        a.situacao === 'Vigente' ? (
          <Can permission={PERM_RH_GERENCIAR}>
            <div className="d-flex" style={{ gap: '0.5rem' }}>
              <Button variant="secondary" size="sm" onClick={() => setAcao({ kind: 'encerrar', afastamento: a })}>
                Encerrar
              </Button>
              <Button variant="ghost" size="sm" onClick={() => setAcao({ kind: 'cancelar', afastamento: a })}>
                Cancelar
              </Button>
            </div>
          </Can>
        ) : (
          '—'
        ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Afastamentos e licenças"
        actions={
          <Link className="br-button secondary" to={`/recursoshumanos/servidores/${servidorId}/ficha`}>
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à ficha
          </Link>
        }
      />

      <QueryState<AfastamentoResumo[]>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(afastamentos) => {
          const vigente = afastamentos.find((a) => a.situacao === 'Vigente');
          return (
            <>
              {vigente ? (
                <Alert variant="info" title="Afastamento vigente" className="mb-4">
                  {formatarTipoAfastamento(vigente.tipo)} — {periodo(vigente)}.{' '}
                  {descreverEfeitoFolha({
                    suspendeProventos: vigente.suspendeProventos,
                    percentualRemuneracao: vigente.percentualRemuneracao,
                    diasPagosPeloEnte: vigente.diasPagosPeloEnte,
                    contaTempo: vigente.contaTempo,
                  })}
                  .
                </Alert>
              ) : (
                <Alert variant="success" title="Sem afastamento vigente" className="mb-4">
                  O servidor está em atividade plena (nenhum afastamento em curso).
                </Alert>
              )}

              <CardSecao
                titulo="Histórico de afastamentos"
                subtitulo="Lançamentos do servidor (mais recente primeiro). Um afastamento vigente por vez."
                acao={
                  <Can permission={PERM_RH_GERENCIAR}>
                    <Button
                      variant="primary"
                      onClick={() => setLancarAberto(true)}
                      disabled={vigente !== undefined}
                      title={vigente ? 'Encerre o afastamento vigente antes de lançar outro.' : undefined}
                    >
                      <i className="fas fa-user-clock" aria-hidden="true" /> Lançar afastamento
                    </Button>
                  </Can>
                }
              >
                <DataTable
                  caption="Histórico de afastamentos do servidor"
                  columns={colunas}
                  rows={afastamentos}
                  rowKey={(a) => a.id}
                  empty={
                    <EmptyState
                      icon="fas fa-user-clock"
                      title="Sem afastamentos"
                      description="O servidor não possui afastamentos ou licenças registrados."
                    />
                  }
                />
              </CardSecao>

              <Can permission={PERM_RH_GERENCIAR}>
                <LancarAfastamentoModal
                  open={lancarAberto}
                  onClose={() => setLancarAberto(false)}
                  servidorId={servidorId}
                />
                <EncerrarAfastamentoModal
                  open={acao?.kind === 'encerrar'}
                  onClose={() => setAcao(null)}
                  servidorId={servidorId}
                  afastamentoId={acao?.afastamento.id ?? ''}
                  tipo={acao?.afastamento.tipo ?? ''}
                  inicio={acao?.afastamento.inicio ?? ''}
                />
                <CancelarAfastamentoModal
                  open={acao?.kind === 'cancelar'}
                  onClose={() => setAcao(null)}
                  servidorId={servidorId}
                  afastamentoId={acao?.afastamento.id ?? ''}
                  tipo={acao?.afastamento.tipo ?? ''}
                />
              </Can>
            </>
          );
        }}
      </QueryState>
    </>
  );
}
