// Tela de DETALHE de um Credenciamento + central de AÇÕES (transições do ciclo —
// Lei 14.133/2021, art. 78, I e art. 79; contratação por inexigibilidade, art. 74, IV).
// Exibe condições do edital, itens credenciáveis (preço fixado) e o ROL de credenciados
// (inscrições com ingresso a qualquer tempo — art. 79, par. único), com ações por inscrição.
import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Select,
  Tag,
  Textarea,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import {
  useAdicionarItemCredenciamento,
  useAlterarChamamento,
  useAlterarCredenciado,
  useCredenciamento,
  useDeferirInscricao,
  useDescredenciar,
  useEncerrarCredenciamento,
  useIndeferirInscricao,
  useInscreverInteressado,
  usePublicarChamamento,
  useRemoverItemCredenciamento,
} from './credenciamento.api';
import type {
  CredenciadoDetalhe,
  CredenciamentoDetalhe,
  ItemCredenciamentoDetalhe,
  SituacaoCredenciamento,
} from './credenciamento.api';
import { credenciamentoEncerrado } from './credenciamento.api';
import {
  HIPOTESE_LABEL,
  SITUACAO_CREDENCIADO_LABEL,
  SITUACAO_LABEL,
  credenciadoTagVariant,
  situacaoTagVariant,
} from './credenciamento.helpers';

function Campo({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type AcaoAberta = 'item' | 'publicar' | 'inscrever' | 'encerrar' | null;

export function CredenciamentoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useCredenciamento(id);
  const [acao, setAcao] = useState<AcaoAberta>(null);
  const fechar = () => setAcao(null);
  const toast = useToast();

  const alterarChamamento = useAlterarChamamento(id);
  const removerItem = useRemoverItemCredenciamento(id);
  const deferir = useDeferirInscricao(id);

  return (
    <QueryState<CredenciamentoDetalhe>
      isLoading={query.isLoading}
      isError={query.isError}
      error={query.error}
      data={query.data}
    >
      {(c) => {
        const terminal = credenciamentoEncerrado(c.situacao);
        const emElaboracao = c.situacao === 'EmElaboracao';
        const aberto = c.situacao === 'ChamamentoAberto';

        const itemColumns: Column<ItemCredenciamentoDetalhe>[] = [
          { key: 'numero', header: 'Nº', align: 'center', render: (i) => i.numero },
          { key: 'descricao', header: 'Descrição', render: (i) => i.descricao },
          { key: 'unidade', header: 'Unidade', render: (i) => i.unidadeMedida },
          { key: 'preco', header: 'Preço fixado', align: 'end', render: (i) => formatarMoeda(i.precoFixado) },
          {
            key: 'acoes',
            header: 'Ações',
            render: (i) =>
              emElaboracao ? (
                <Can permission="administracao.gerenciar">
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={removerItem.isPending}
                    onClick={() =>
                      removerItem.mutate(i.itemId, {
                        onSuccess: () => toast.success('Item removido.', 'Sucesso'),
                        onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível remover.'),
                      })
                    }
                  >
                    Remover
                  </Button>
                </Can>
              ) : (
                '—'
              ),
          },
        ];

        const credColumns: Column<CredenciadoDetalhe>[] = [
          { key: 'fornecedor', header: 'Fornecedor', render: (r) => `${r.fornecedorId.slice(0, 8)}…` },
          { key: 'inscricao', header: 'Inscrição', render: (r) => formatarData(r.dataInscricao) },
          {
            key: 'situacao',
            header: 'Situação',
            render: (r) => <Tag variant={credenciadoTagVariant(r.situacao)}>{SITUACAO_CREDENCIADO_LABEL[r.situacao]}</Tag>,
          },
          { key: 'credenciamento', header: 'Credenciado em', render: (r) => formatarData(r.dataCredenciamento) },
          { key: 'motivo', header: 'Observação', render: (r) => r.motivo ?? '—' },
          {
            key: 'acoes',
            header: 'Ações',
            sticky: true,
            render: (r) => <AcoesCredenciado credenciamentoId={id} credenciado={r} deferir={deferir} />,
          },
        ];

        return (
          <>
            <PageHeader
              eyebrow="Credenciamento"
              title={c.objeto}
              description={HIPOTESE_LABEL[c.hipotese]}
              actions={
                <Link className="br-button secondary" to="/administracao/credenciamentos">
                  <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
                </Link>
              }
            />

            <Card className="mb-4">
              <dl className="row mb-0">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoTagVariant(c.situacao)}>{SITUACAO_LABEL[c.situacao]}</Tag>
                </Campo>
                <Campo rotulo="Edital de chamamento">{c.numeroEdital ?? '—'}</Campo>
                <Campo rotulo="Vigência">
                  {formatarData(c.vigenciaInicio)} a {formatarData(c.vigenciaFim)}
                </Campo>
                <Campo rotulo="Credenciados aptos">{c.quantidadeCredenciadosAptos}</Campo>
                <Campo rotulo="Fundamentação legal">{c.fundamentacaoLegal}</Campo>
                <Campo rotulo="PNCP">{c.numeroPncp ?? '—'}</Campo>
              </dl>
            </Card>

            <Can permission="administracao.gerenciar">
              {!terminal && (
                <Card className="mb-4" header={<strong>Ações do edital</strong>}>
                  <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                    {emElaboracao && (
                      <>
                        <Button variant="primary" onClick={() => setAcao('item')}>
                          <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
                        </Button>
                        <Button variant="secondary" onClick={() => setAcao('publicar')} disabled={c.itens.length === 0}>
                          <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar chamamento
                        </Button>
                      </>
                    )}
                    {aberto && (
                      <>
                        <Button variant="primary" onClick={() => setAcao('inscrever')}>
                          <i className="fas fa-user-plus" aria-hidden="true" /> Inscrever interessado
                        </Button>
                        <Button
                          variant="secondary"
                          loading={alterarChamamento.isPending}
                          onClick={() =>
                            alterarChamamento.mutate(
                              { suspender: true, motivo: 'Suspensão temporária do recebimento de inscrições.' },
                              {
                                onSuccess: () => toast.success('Chamamento suspenso.', 'Sucesso'),
                                onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível suspender.'),
                              },
                            )
                          }
                        >
                          <i className="fas fa-pause" aria-hidden="true" /> Suspender
                        </Button>
                      </>
                    )}
                    {c.situacao === 'Suspenso' && (
                      <Button
                        variant="primary"
                        loading={alterarChamamento.isPending}
                        onClick={() =>
                          alterarChamamento.mutate(
                            { suspender: false },
                            {
                              onSuccess: () => toast.success('Chamamento reaberto.', 'Sucesso'),
                              onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível reabrir.'),
                            },
                          )
                        }
                      >
                        <i className="fas fa-play" aria-hidden="true" /> Reabrir
                      </Button>
                    )}
                    <Button variant="danger" onClick={() => setAcao('encerrar')}>
                      <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar / anular / revogar
                    </Button>
                  </div>
                </Card>
              )}
            </Can>

            <Card className="mb-4" header={<strong>Itens credenciáveis (preço fixado)</strong>}>
              <DataTable
                caption="Itens do credenciamento"
                columns={itemColumns}
                rows={c.itens}
                rowKey={(i) => i.itemId}
                empty={
                  <EmptyState
                    icon="fas fa-box"
                    title="Sem itens"
                    description="Adicione ao menos um item para publicar o chamamento."
                  />
                }
              />
            </Card>

            <Card header={<strong>Rol de credenciados</strong>}>
              <DataTable
                caption="Inscrições no credenciamento"
                columns={credColumns}
                rows={c.credenciados}
                rowKey={(r) => r.credenciadoId}
                empty={
                  <EmptyState
                    icon="fas fa-users"
                    title="Sem inscrições"
                    description="Nenhum interessado inscrito até o momento."
                  />
                }
              />
            </Card>

            <AdicionarItemModal open={acao === 'item'} onClose={fechar} credenciamentoId={id} />
            <PublicarChamamentoModal open={acao === 'publicar'} onClose={fechar} credenciamentoId={id} />
            <InscreverModal open={acao === 'inscrever'} onClose={fechar} credenciamentoId={id} />
            <EncerrarModal open={acao === 'encerrar'} onClose={fechar} credenciamentoId={id} />
          </>
        );
      }}
    </QueryState>
  );
}

// --- Ações por inscrição (deferir/indeferir/suspender/reabilitar/descredenciar) ---

function AcoesCredenciado({
  credenciamentoId,
  credenciado,
  deferir,
}: {
  credenciamentoId: string;
  credenciado: CredenciadoDetalhe;
  deferir: ReturnType<typeof useDeferirInscricao>;
}) {
  const toast = useToast();
  const [modal, setModal] = useState<'indeferir' | 'descredenciar' | null>(null);
  const alterar = useAlterarCredenciado(credenciamentoId);

  if (credenciado.situacao === 'Indeferido' || credenciado.situacao === 'Descredenciado') {
    return <>—</>;
  }

  return (
    <Can permission="administracao.gerenciar">
      <div className="d-flex flex-wrap" style={{ gap: '0.25rem' }}>
        {credenciado.situacao === 'EmAnalise' && (
          <>
            <Button
              variant="primary"
              size="sm"
              loading={deferir.isPending}
              onClick={() =>
                deferir.mutate(credenciado.credenciadoId, {
                  onSuccess: () => toast.success('Inscrição deferida.', 'Sucesso'),
                  onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível deferir.'),
                })
              }
            >
              Deferir
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setModal('indeferir')}>
              Indeferir
            </Button>
          </>
        )}
        {credenciado.situacao === 'Credenciado' && (
          <Button
            variant="secondary"
            size="sm"
            loading={alterar.isPending}
            onClick={() =>
              alterar.mutate(
                { credenciadoId: credenciado.credenciadoId, suspender: true, motivo: 'Suspensão por descumprimento sanável.' },
                {
                  onSuccess: () => toast.success('Credenciado suspenso.', 'Sucesso'),
                  onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível suspender.'),
                },
              )
            }
          >
            Suspender
          </Button>
        )}
        {credenciado.situacao === 'Suspenso' && (
          <Button
            variant="primary"
            size="sm"
            loading={alterar.isPending}
            onClick={() =>
              alterar.mutate(
                { credenciadoId: credenciado.credenciadoId, suspender: false },
                {
                  onSuccess: () => toast.success('Credenciado reabilitado.', 'Sucesso'),
                  onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível reabilitar.'),
                },
              )
            }
          >
            Reabilitar
          </Button>
        )}
        <Button variant="danger" size="sm" onClick={() => setModal('descredenciar')}>
          Descredenciar
        </Button>
      </div>

      <MotivoInscricaoModal
        acao="indeferir"
        open={modal === 'indeferir'}
        onClose={() => setModal(null)}
        credenciamentoId={credenciamentoId}
        credenciadoId={credenciado.credenciadoId}
      />
      <MotivoInscricaoModal
        acao="descredenciar"
        open={modal === 'descredenciar'}
        onClose={() => setModal(null)}
        credenciamentoId={credenciamentoId}
        credenciadoId={credenciado.credenciadoId}
      />
    </Can>
  );
}

// --- Modais de ação (inline) -------------------------------------------------

interface AcaoProps {
  open: boolean;
  onClose: () => void;
  credenciamentoId: string;
}

function AdicionarItemModal({ open, onClose, credenciamentoId }: AcaoProps) {
  const toast = useToast();
  const mutation = useAdicionarItemCredenciamento(credenciamentoId);
  const [descricao, setDescricao] = useState('');
  const [unidade, setUnidade] = useState('');
  const [preco, setPreco] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setDescricao('');
    setUnidade('');
    setPreco('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const v = Number(preco);
    if (descricao.trim() === '' || unidade.trim() === '' || Number.isNaN(v) || v <= 0) {
      setErro('Informe descrição, unidade e preço fixado positivo.');
      return;
    }
    mutation.mutate(
      { descricao: descricao.trim(), unidadeMedida: unidade.trim(), precoFixado: v, itemCatalogoId: null },
      {
        onSuccess: () => {
          toast.success('Item adicionado.', 'Sucesso');
          fechar();
        },
        onError: (e) => {
          if (e instanceof ApiError) setErro(e.userMessage);
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível adicionar o item.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Adicionar item credenciável"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-item-credenciamento" loading={mutation.isPending}>
            Adicionar
          </Button>
        </>
      }
    >
      <form id="form-item-credenciamento" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          No credenciamento não há disputa de preço: o valor é fixado pela Administração e o credenciado adere a ele
          (Lei 14.133/2021, art. 79).
        </p>
        <FormField label="Descrição" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={1000} value={descricao} onChange={(e) => setDescricao(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-md-6">
            <FormField label="Unidade de medida" required>
              {({ id }) => <Input id={id} maxLength={50} value={unidade} onChange={(e) => setUnidade(e.target.value)} placeholder="Ex.: consulta, exame, km" />}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Preço fixado (R$)" required>
              {({ id }) => <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={preco} onChange={(e) => setPreco(e.target.value)} />}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

function PublicarChamamentoModal({ open, onClose, credenciamentoId }: AcaoProps) {
  const toast = useToast();
  const mutation = usePublicarChamamento(credenciamentoId);
  const [numeroEdital, setNumeroEdital] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumeroEdital('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numeroEdital.trim() === '') {
      setErro('Informe o número do edital de chamamento.');
      return;
    }
    mutation.mutate(numeroEdital.trim(), {
      onSuccess: () => {
        toast.success('Chamamento publicado — inscrições abertas.', 'Sucesso');
        fechar();
      },
      onError: (e) => {
        if (e instanceof ApiError) setErro(e.userMessage);
        toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível publicar o chamamento.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar chamamento público"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-publicar-chamamento" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-publicar-chamamento" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          O chamamento fica permanentemente aberto a inscrições enquanto vigente o edital (Lei 14.133/2021, art. 79, par.
          único).
        </p>
        <FormField label="Número do edital" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={60} value={numeroEdital} onChange={(e) => setNumeroEdital(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

function InscreverModal({ open, onClose, credenciamentoId }: AcaoProps) {
  const toast = useToast();
  const mutation = useInscreverInteressado(credenciamentoId);
  const [fornecedorId, setFornecedorId] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setFornecedorId('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (fornecedorId.trim() === '') {
      setErro('Informe o identificador do fornecedor interessado.');
      return;
    }
    mutation.mutate(fornecedorId.trim(), {
      onSuccess: () => {
        toast.success('Interessado inscrito.', 'Sucesso');
        fechar();
      },
      onError: (e) => {
        if (e instanceof ApiError) setErro(e.userMessage);
        toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível inscrever.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Inscrever interessado"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-inscrever-credenciamento" loading={mutation.isPending}>
            Inscrever
          </Button>
        </>
      }
    >
      <form id="form-inscrever-credenciamento" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">O fornecedor deve estar cadastrado no módulo. A inscrição nasce em análise documental.</p>
        <FormField label="Fornecedor (identificador)" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fornecedorId} onChange={(e) => setFornecedorId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

function EncerrarModal({ open, onClose, credenciamentoId }: AcaoProps) {
  const toast = useToast();
  const mutation = useEncerrarCredenciamento(credenciamentoId);
  const [situacao, setSituacao] = useState<SituacaoCredenciamento>('Encerrado');
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setSituacao('Encerrado');
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('A motivação do ato administrativo é obrigatória.');
      return;
    }
    mutation.mutate(
      { situacao, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Credenciamento encerrado.', 'Sucesso');
          fechar();
        },
        onError: (e) => {
          if (e instanceof ApiError) setErro(e.userMessage);
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível encerrar.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar credenciamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-encerrar-credenciamento" loading={mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-encerrar-credenciamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de encerramento" required>
          {({ id }) => (
            <Select
              id={id}
              options={[
                { value: 'Encerrado', label: 'Encerrar (decurso de vigência/conveniência)' },
                { value: 'Anulado', label: 'Anular (ilegalidade)' },
                { value: 'Revogado', label: 'Revogar (conveniência/oportunidade)' },
              ]}
              value={situacao}
              onChange={(e) => setSituacao(e.target.value as SituacaoCredenciamento)}
            />
          )}
        </FormField>
        <FormField label="Motivo" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

function MotivoInscricaoModal({
  acao,
  open,
  onClose,
  credenciamentoId,
  credenciadoId,
}: {
  acao: 'indeferir' | 'descredenciar';
  open: boolean;
  onClose: () => void;
  credenciamentoId: string;
  credenciadoId: string;
}) {
  const toast = useToast();
  const indeferir = useIndeferirInscricao(credenciamentoId);
  const descredenciar = useDescredenciar(credenciamentoId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const config =
    acao === 'indeferir'
      ? { title: 'Indeferir inscrição', mutation: indeferir, sucesso: 'Inscrição indeferida.' }
      : { title: 'Descredenciar', mutation: descredenciar, sucesso: 'Interessado descredenciado.' };

  function fechar(): void {
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('A motivação do ato administrativo é obrigatória.');
      return;
    }
    config.mutation.mutate(
      { credenciadoId, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success(config.sucesso, 'Sucesso');
          fechar();
        },
        onError: (e: unknown) => {
          if (e instanceof ApiError) setErro(e.userMessage);
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível concluir o ato.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={config.title}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={config.mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-motivo-inscricao" loading={config.mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-motivo-inscricao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Motivo" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
