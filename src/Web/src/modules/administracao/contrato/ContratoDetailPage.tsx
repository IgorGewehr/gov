// Tela de DETALHE do Contrato (query ObterContratoPorId) + TODAS as ações de
// transição/registro do agregado, habilitadas conforme a máquina de estados:
//   - PublicarContratoNoPncp  (Assinado/Eficaz/EmExecucao, ≠ encerrado)
//   - IniciarExecucaoContrato (Eficaz — exige PNCP + dotação)
//   - CelebrarAditivo         (Assinado/Eficaz/EmExecucao)
//   - ApostilarContrato       (Assinado/Eficaz/EmExecucao)
//   - PrestarGarantia         (Assinado/Eficaz/EmExecucao)
//   - EncerrarContrato        (Eficaz/EmExecucao)
//   - RescindirContrato       (Assinado/Eficaz/EmExecucao — destrutivo)
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import {
  MODALIDADE_GARANTIA_ROTULO,
  ORIGEM_ROTULO,
  SITUACAO_ROTULO,
  TIPO_ADITIVO_ROTULO,
  useContrato,
  useEncerrarContrato,
  useIniciarExecucao,
} from './contrato.api';
import type {
  AditivoResumo,
  ContratoDetalhe,
  GarantiaResumo,
  SituacaoContrato,
} from './contrato.api';
import { situacaoTagVariant } from './contrato.helpers';
import {
  ApostilarModal,
  CelebrarAditivoModal,
  ConfirmacaoModal,
  PrestarGarantiaModal,
  PublicarNoPncpModal,
  RescindirModal,
} from './ContratoAcaoModals';

const ENCERRADO: SituacaoContrato[] = ['Encerrado', 'Rescindido'];
const APTO_ALTERACAO: SituacaoContrato[] = ['Assinado', 'Eficaz', 'EmExecucao'];
const APTO_ENCERRAR: SituacaoContrato[] = ['Eficaz', 'EmExecucao'];

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type AcaoAberta = 'pncp' | 'aditivo' | 'apostilar' | 'garantia' | 'rescindir' | 'iniciar' | 'encerrar' | null;

export function ContratoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useContrato(id);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  const iniciarMutation = useIniciarExecucao(id);
  const encerrarMutation = useEncerrarContrato(id);

  function fechar(): void {
    setAcao(null);
  }

  function iniciarExecucao(): void {
    iniciarMutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Execução iniciada (situação: Em execução).', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível iniciar a execução.'),
    });
  }

  function encerrar(): void {
    encerrarMutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Contrato encerrado.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o contrato.'),
    });
  }

  const aditivoColumns: Column<AditivoResumo>[] = [
    { key: 'numero', header: 'Nº', align: 'end', sortAccessor: (a) => a.numero, render: (a) => a.numero },
    { key: 'tipo', header: 'Tipo', render: (a) => TIPO_ADITIVO_ROTULO[a.tipo] ?? a.tipo },
    {
      key: 'percentual',
      header: 'Percentual',
      align: 'end',
      sortAccessor: (a) => a.percentual,
      render: (a) => `${a.percentual.toLocaleString('pt-BR')}%`,
    },
    {
      key: 'pncp',
      header: 'PNCP',
      render: (a) =>
        a.publicadoNoPncp ? (
          <Tag variant="success">Publicado</Tag>
        ) : (
          <Tag variant="warning">Pendente</Tag>
        ),
    },
  ];

  const garantiaColumns: Column<GarantiaResumo>[] = [
    { key: 'modalidade', header: 'Modalidade', render: (g) => MODALIDADE_GARANTIA_ROTULO[g.modalidade] ?? g.modalidade },
    {
      key: 'percentual',
      header: 'Percentual',
      align: 'end',
      sortAccessor: (g) => g.percentual,
      render: (g) => `${g.percentual.toLocaleString('pt-BR')}%`,
    },
    { key: 'valor', header: 'Valor', align: 'end', sortAccessor: (g) => g.valor, render: (g) => formatarMoeda(g.valor) },
    {
      key: 'validade',
      header: 'Validade',
      sortAccessor: (g) => g.validadeFim,
      render: (g) => formatarData(g.validadeFim),
    },
  ];

  return (
    <>
      <PageHeader
        title="Detalhe do contrato"
        actions={
          <Link className="br-button secondary" to="/administracao/contratos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ContratoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(contrato) => {
          const encerrado = ENCERRADO.includes(contrato.situacao);
          const podeAlterar = APTO_ALTERACAO.includes(contrato.situacao);
          const podeIniciar = contrato.situacao === 'Eficaz' && contrato.publicadoNoPncp && contrato.dotacaoConfirmada;
          const podeEncerrar = APTO_ENCERRAR.includes(contrato.situacao);

          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center">
                    <strong>Objeto: {contrato.objeto}</strong>
                    <Tag variant={situacaoTagVariant(contrato.situacao)}>{SITUACAO_ROTULO[contrato.situacao]}</Tag>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="Fornecedor">
                    <span className="text-mono text-down-01">{contrato.fornecedorId}</span>
                  </Campo>
                  <Campo rotulo="Origem da contratação">{ORIGEM_ROTULO[contrato.origem] ?? contrato.origem}</Campo>
                  <Campo rotulo="Licitação de origem">
                    {contrato.licitacaoId ? (
                      <span className="text-mono text-down-01">{contrato.licitacaoId}</span>
                    ) : (
                      '— (contratação direta)'
                    )}
                  </Campo>
                  <Campo rotulo="Valor contratado">{formatarMoeda(contrato.valorContratado)}</Campo>
                  <Campo rotulo="Valor atual">{formatarMoeda(contrato.valorAtual)}</Campo>
                  <Campo rotulo="Vigência">
                    {formatarData(contrato.vigenciaInicio)} a {formatarData(contrato.vigenciaFim)}
                  </Campo>
                  <Campo rotulo="Publicado no PNCP">
                    {contrato.publicadoNoPncp ? (
                      <Tag variant="success">Sim{contrato.numeroContratoPncp ? ` (${contrato.numeroContratoPncp})` : ''}</Tag>
                    ) : (
                      <Tag variant="warning">Não</Tag>
                    )}
                  </Campo>
                  <Campo rotulo="Dotação confirmada">
                    {contrato.dotacaoConfirmada ? <Tag variant="success">Sim</Tag> : <Tag variant="warning">Não</Tag>}
                  </Campo>
                </dl>
              </Card>

              {/* Ações — gated por "administracao.gerenciar" (§6) + máquina de estados */}
              <Can permission="administracao.gerenciar">
              <Card className="mb-4" header={<strong>Ações</strong>}>
                {encerrado ? (
                  <Alert variant="info">
                    Contrato {SITUACAO_ROTULO[contrato.situacao].toLowerCase()}: não admite novas operações (estado terminal).
                  </Alert>
                ) : (
                  <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                    <Button
                      variant="primary"
                      onClick={() => setAcao('pncp')}
                      disabled={!podeAlterar}
                      title="Publicar no PNCP — condição de eficácia (art. 174)"
                    >
                      <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar no PNCP
                    </Button>
                    <Button
                      variant="secondary"
                      onClick={() => setAcao('iniciar')}
                      disabled={!podeIniciar}
                      title={
                        podeIniciar
                          ? 'Iniciar execução'
                          : 'Exige situação Eficaz, publicação no PNCP e dotação confirmada (I-7/I-8)'
                      }
                    >
                      <i className="fas fa-play" aria-hidden="true" /> Iniciar execução
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('aditivo')} disabled={!podeAlterar}>
                      <i className="fas fa-file-pen" aria-hidden="true" /> Celebrar aditivo
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('apostilar')} disabled={!podeAlterar}>
                      <i className="fas fa-pen-nib" aria-hidden="true" /> Apostilar
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('garantia')} disabled={!podeAlterar}>
                      <i className="fas fa-shield-halved" aria-hidden="true" /> Prestar garantia
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('encerrar')} disabled={!podeEncerrar}>
                      <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar
                    </Button>
                    <Button variant="danger" onClick={() => setAcao('rescindir')} disabled={!podeAlterar}>
                      <i className="fas fa-ban" aria-hidden="true" /> Rescindir
                    </Button>
                  </div>
                )}
              </Card>
              </Can>

              <Card className="mb-4" header={<strong>Aditivos</strong>}>
                <DataTable
                  caption={`Aditivos do contrato ${contrato.id}`}
                  columns={aditivoColumns}
                  rows={contrato.aditivos}
                  rowKey={(a) => a.aditivoId}
                  empty={<EmptyState icon="fas fa-file-pen" title="Nenhum aditivo registrado." />}
                />
              </Card>

              <Card header={<strong>Garantias</strong>}>
                <DataTable
                  caption={`Garantias do contrato ${contrato.id}`}
                  columns={garantiaColumns}
                  rows={contrato.garantias}
                  rowKey={(g) => g.garantiaId}
                  empty={<EmptyState icon="fas fa-shield-halved" title="Nenhuma garantia registrada." />}
                />
              </Card>

              {/* Modais de ação */}
              <PublicarNoPncpModal contratoId={id} open={acao === 'pncp'} onClose={fechar} />
              <CelebrarAditivoModal contratoId={id} open={acao === 'aditivo'} onClose={fechar} />
              <ApostilarModal contratoId={id} open={acao === 'apostilar'} onClose={fechar} />
              <PrestarGarantiaModal contratoId={id} open={acao === 'garantia'} onClose={fechar} />
              <RescindirModal contratoId={id} open={acao === 'rescindir'} onClose={fechar} />

              <ConfirmacaoModal
                open={acao === 'iniciar'}
                onClose={fechar}
                onConfirm={iniciarExecucao}
                title="Iniciar execução"
                confirmLabel="Iniciar execução"
                loading={iniciarMutation.isPending}
              >
                <p>
                  Confirmar o início da execução do contrato? Exige eficácia (publicação no PNCP) e cobertura
                  orçamentária (dotação confirmada).
                </p>
              </ConfirmacaoModal>

              <ConfirmacaoModal
                open={acao === 'encerrar'}
                onClose={fechar}
                onConfirm={encerrar}
                title="Encerrar contrato"
                confirmLabel="Encerrar"
                loading={encerrarMutation.isPending}
                destrutivo
              >
                <Alert variant="warning">
                  O encerramento marca o contrato como concluído (estado terminal). Esta ação não pode ser desfeita.
                </Alert>
              </ConfirmacaoModal>
            </>
          );
        }}
      </QueryState>
    </>
  );
}
