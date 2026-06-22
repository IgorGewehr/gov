// Tela de DETALHE de um Documento (a projecao DocumentoResumo e obtida da listagem do
// processo — Query 6.1 — e filtrada por id). Hospeda os botoes de ACAO por command/query:
//   - Assinar (Command 5.2)            -> visivel quando Juntado|Assinado
//   - Tornar sem efeito (Command 5.3)  -> visivel quando Juntado|Assinado (destrutivo)
//   - Verificar integridade (Query 6.2)-> sempre disponivel
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useDocumentosDoProcesso } from './documento.api';
import type { DocumentoResumo } from './documento.api';
import {
  criticidadeTagVariant,
  permiteTransicao,
  rotuloTipoAssinatura,
  situacaoTagVariant,
} from './documento.helpers';
import {
  AssinarDocumentoModal,
  TornarSemEfeitoModal,
  VerificarIntegridadeModal,
} from './DocumentoAcaoModais';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type AcaoAberta = 'assinar' | 'sem-efeito' | 'integridade' | null;

export function DocumentoDetailPage() {
  const { processoId = '', documentoId = '' } = useParams<{ processoId: string; documentoId: string }>();
  const query = useDocumentosDoProcesso(processoId);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  const documento: DocumentoResumo | undefined = query.data?.find((d) => d.id === documentoId);

  return (
    <>
      <PageHeader
        title="Detalhe do documento"
        actions={
          <Link className="br-button secondary" to={`/protocolo`}>
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<DocumentoResumo[]>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {() => {
          if (!documento) {
            return (
              <Card>
                <p className="mb-0">Documento nao encontrado neste processo (pode ser sigiloso ou de outro tenant).</p>
              </Card>
            );
          }

          const transicaoPermitida = permiteTransicao(documento.situacao);

          return (
            <>
              <Card
                header={<strong>Documento {documento.id}</strong>}
                footer={
                  <div className="d-flex" style={{ gap: '0.5rem', flexWrap: 'wrap' }}>
                    <Can permission="protocolo.gerenciar">
                      <Button
                        variant="primary"
                        onClick={() => setAcao('assinar')}
                        disabled={!transicaoPermitida}
                        title={
                          transicaoPermitida
                            ? undefined
                            : 'Disponivel apenas para documentos Juntado ou Assinado.'
                        }
                      >
                        <i className="fas fa-pen-nib" aria-hidden="true" /> Assinar
                      </Button>
                      <Button
                        variant="secondary"
                        onClick={() => setAcao('sem-efeito')}
                        disabled={!transicaoPermitida}
                        title={
                          transicaoPermitida
                            ? undefined
                            : 'Disponivel apenas para documentos Juntado ou Assinado.'
                        }
                      >
                        <i className="fas fa-ban" aria-hidden="true" /> Tornar sem efeito
                      </Button>
                    </Can>
                    <Button variant="tertiary" onClick={() => setAcao('integridade')}>
                      <i className="fas fa-shield-halved" aria-hidden="true" /> Verificar integridade
                    </Button>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="Situacao">
                    <Tag variant={situacaoTagVariant(documento.situacao)}>{documento.situacao}</Tag>
                  </Campo>
                  <Campo rotulo="Criticidade">
                    <Tag variant={criticidadeTagVariant(documento.criticidade)}>{documento.criticidade}</Tag>
                  </Campo>
                  <Campo rotulo="Tipo de assinatura">{rotuloTipoAssinatura(documento.tipoAssinatura)}</Campo>
                  <Campo rotulo="Data de juntada">
                    {documento.dataJuntada ? formatarData(documento.dataJuntada) : '—'}
                  </Campo>
                  <div className="col-12 mb-3">
                    <dt className="text-gray-60 text-down-01">Hash (SHA-256)</dt>
                    <dd className="mb-0">
                      <code className="text-break">{documento.hash}</code>
                    </dd>
                  </div>
                </dl>
              </Card>

              <AssinarDocumentoModal
                open={acao === 'assinar'}
                onClose={() => setAcao(null)}
                processoId={processoId}
                documento={documento}
              />
              <TornarSemEfeitoModal
                open={acao === 'sem-efeito'}
                onClose={() => setAcao(null)}
                processoId={processoId}
                documento={documento}
              />
              <VerificarIntegridadeModal
                open={acao === 'integridade'}
                onClose={() => setAcao(null)}
                documento={documento}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
