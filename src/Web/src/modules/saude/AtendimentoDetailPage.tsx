// Tela de DETALHE de um Atendimento. Padrão-ouro: param de rota -> useQuery,
// QueryState para loading/erro, cabeçalho com pares rótulo/valor e seções de
// evoluções SOAP, prescrições e exames. Dado sensível — LGPD art. 11.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useAtendimento } from './api';
import type { AtendimentoDetalhe } from './api';
import {
  atendimentoAssinado,
  atendimentoCompartilhado,
  atendimentoEmAndamento,
  situacaoAtendimentoVariant,
} from './saude.helpers';
import { AdicionarAdendoModal, AdicionarEvolucaoModal } from './AtendimentoEvolucaoModals';
import {
  AssinarAtendimentoModal,
  CancelarAtendimentoModal,
  CompartilharRndsModal,
  LancarSisabModal,
} from './AtendimentoAcaoModals';

type AcaoAtendimento =
  | 'evolucao'
  | 'assinar'
  | 'adendo'
  | 'rnds'
  | 'sisab'
  | 'cancelar'
  | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function AtendimentoDetailPage() {
  const { atendimentoId = '' } = useParams<{ atendimentoId: string }>();
  const query = useAtendimento(atendimentoId);
  const [acao, setAcao] = useState<AcaoAtendimento>(null);

  return (
    <>
      <PageHeader
        title="Detalhe do atendimento"
        actions={
          <Link className="br-button secondary" to="/saude">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<AtendimentoDetalhe | null>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Atendimento não encontrado"
            description="O atendimento informado não existe ou não está disponível."
          />
        }
      >
        {(atendimento) =>
          atendimento === null ? (
            <EmptyState
              icon="fas fa-folder-open"
              title="Atendimento não encontrado"
              description="O atendimento informado não existe ou não está disponível."
            />
          ) : (
            <>
              <Card className="mb-4">
                <dl className="row">
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoAtendimentoVariant(atendimento.situacao)}>
                      {atendimento.situacao}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Modalidade">{atendimento.modalidade}</Campo>
                  <Campo rotulo="Data/hora">{formatarData(atendimento.dataHora)}</Campo>
                  <Campo rotulo="Competência">{atendimento.competencia}</Campo>
                  <Campo rotulo="Nível de garantia">{atendimento.nivelGarantia}</Campo>
                  <Campo rotulo="Assinatura">
                    <Tag variant={atendimento.assinado ? 'success' : 'warning'}>
                      {atendimento.assinado ? 'Assinado (ICP-Brasil)' : 'Não assinado'}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Estabelecimento (CNES)">{atendimento.estabelecimentoId}</Campo>
                  <Campo rotulo="Profissional">{atendimento.profissionalId}</Campo>
                </dl>
              </Card>

              <Can permission="saude.gerenciar">
                <Card className="mb-4" header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="primary"
                      onClick={() => setAcao('evolucao')}
                      disabled={!atendimentoEmAndamento(atendimento.situacao)}
                    >
                      <i className="fas fa-pen-to-square" aria-hidden="true" /> Adicionar evolução
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('assinar')}
                      disabled={!atendimentoEmAndamento(atendimento.situacao)}
                    >
                      <i className="fas fa-signature" aria-hidden="true" /> Assinar
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('adendo')}
                      disabled={!atendimentoAssinado(atendimento.situacao)}
                    >
                      <i className="fas fa-paperclip" aria-hidden="true" /> Adendo
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('rnds')}
                      disabled={!atendimentoAssinado(atendimento.situacao)}
                    >
                      <i className="fas fa-cloud-arrow-up" aria-hidden="true" /> Compartilhar (RNDS)
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('sisab')}
                      disabled={!atendimentoCompartilhado(atendimento.situacao)}
                    >
                      <i className="fas fa-database" aria-hidden="true" /> Lançar (SISAB)
                    </Button>
                    <Button
                      variant="danger"
                      onClick={() => setAcao('cancelar')}
                      disabled={!atendimentoEmAndamento(atendimento.situacao)}
                    >
                      <i className="fas fa-ban" aria-hidden="true" /> Cancelar
                    </Button>
                  </div>
                </Card>
              </Can>

              <Card className="mb-4" header={<strong>Evoluções (SOAP)</strong>}>
                {atendimento.evolucoes.length === 0 ? (
                  <p className="text-gray-60 mb-0">Nenhuma evolução registrada.</p>
                ) : (
                  <ul className="br-list">
                    {atendimento.evolucoes.map((e) => (
                      <li key={e.id} className="br-item">
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <span className="text-down-01 text-gray-60">{formatarData(e.dataHora)}</span>
                          <span>
                            {e.ehAdendo && <Tag variant="info">Adendo</Tag>}{' '}
                            <Tag variant={e.assinada ? 'success' : 'warning'}>
                              {e.assinada ? 'Assinada' : 'Pendente'}
                            </Tag>
                          </span>
                        </div>
                        <dl className="row mb-0">
                          <Campo rotulo="Subjetivo">{e.subjetivo || '—'}</Campo>
                          <Campo rotulo="Objetivo">{e.objetivo || '—'}</Campo>
                          <Campo rotulo="Avaliação">{e.avaliacao || '—'}</Campo>
                          <Campo rotulo="Plano">{e.plano || '—'}</Campo>
                        </dl>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>

              <div className="row">
                <div className="col-md-6">
                  <Card header={<strong>Prescrições</strong>}>
                    {atendimento.prescricoes.length === 0 ? (
                      <p className="text-gray-60 mb-0">Nenhuma prescrição.</p>
                    ) : (
                      <ul className="br-list">
                        {atendimento.prescricoes.map((p) => (
                          <li key={p.id} className="br-item">
                            <strong>{p.item}</strong> — {p.posologia}
                          </li>
                        ))}
                      </ul>
                    )}
                  </Card>
                </div>
                <div className="col-md-6">
                  <Card header={<strong>Solicitações de exame</strong>}>
                    {atendimento.exames.length === 0 ? (
                      <p className="text-gray-60 mb-0">Nenhuma solicitação de exame.</p>
                    ) : (
                      <ul className="br-list">
                        {atendimento.exames.map((s) => (
                          <li key={s.id} className="br-item">
                            <strong>{s.procedimento}</strong> — {s.justificativa}
                          </li>
                        ))}
                      </ul>
                    )}
                  </Card>
                </div>
              </div>

              <AdicionarEvolucaoModal
                open={acao === 'evolucao'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
              />
              <AssinarAtendimentoModal
                open={acao === 'assinar'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
              />
              <AdicionarAdendoModal
                open={acao === 'adendo'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
                evolucoes={atendimento.evolucoes}
              />
              <CompartilharRndsModal
                open={acao === 'rnds'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
              />
              <LancarSisabModal
                open={acao === 'sisab'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
              />
              <CancelarAtendimentoModal
                open={acao === 'cancelar'}
                onClose={() => setAcao(null)}
                atendimentoId={atendimento.id}
              />
            </>
          )
        }
      </QueryState>
    </>
  );
}
