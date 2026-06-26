// Tela de DETALHE de uma Dispensa Eletrônica + central de AÇÕES (transições da
// máquina de estados do procedimento — Lei 14.133/2021, art. 75; IN SEGES/ME 67/2021).
// Cada command vira um botão gated pela situação atual, abrindo o modal correspondente.
// Exibe itens, cotações (lances) e o limite legal vigente aplicado (parametrizável por tenant).
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
import { errorMessage } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { formatarDataHora, formatarMoeda } from '../../../i18n/format';
import {
  useAbrirDisputaDispensa,
  useAdicionarItemDispensa,
  useAnularDispensa,
  useDeclararDispensaDeserta,
  useDeclararDispensaFracassada,
  useDispensa,
  useEncerrarDisputaDispensa,
  useHomologarDispensa,
  usePublicarAvisoDispensa,
  useRegistrarLanceDispensa,
  useRevogarDispensa,
} from './dispensa.api';
import type { CotacaoDispensaResumo, DispensaDetalhe, ItemDispensaResumo } from './dispensa.api';
import {
  CRITERIO_LABEL,
  FUNDAMENTO_LABEL,
  SITUACAO_COTACAO_LABEL,
  SITUACAO_LABEL,
  cotacaoTagVariant,
  situacaoTagVariant,
} from './dispensa.helpers';

function Campo({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type AcaoAberta =
  | 'item'
  | 'aviso'
  | 'lance'
  | 'homologar'
  | 'fracassar'
  | 'revogar'
  | 'anular'
  | null;

export function DispensaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useDispensa(id);
  const [acao, setAcao] = useState<AcaoAberta>(null);
  const fechar = () => setAcao(null);

  const abrirDisputa = useAbrirDisputaDispensa(id);
  const encerrarDisputa = useEncerrarDisputaDispensa(id);
  const deserta = useDeclararDispensaDeserta(id);
  const toast = useToast();

  return (
    <QueryState<DispensaDetalhe> isLoading={query.isLoading} isError={query.isError} error={query.error} data={query.data}>
      {(dispensa) => {
        const encerrada = ['Homologada', 'Fracassada', 'Deserta', 'Revogada', 'Anulada'].includes(dispensa.situacao);

        const itemColumns: Column<ItemDispensaResumo>[] = [
          { key: 'numero', header: 'Nº', align: 'center', render: (i) => i.numero },
          { key: 'descricao', header: 'Descrição', render: (i) => i.descricao },
          { key: 'qtd', header: 'Qtd.', align: 'end', render: (i) => i.quantidade },
          { key: 'unit', header: 'Valor unitário', align: 'end', render: (i) => formatarMoeda(i.valorUnitarioEstimado) },
          { key: 'total', header: 'Valor total', align: 'end', render: (i) => formatarMoeda(i.valorTotalEstimado) },
        ];

        const cotacaoColumns: Column<CotacaoDispensaResumo>[] = [
          { key: 'fornecedor', header: 'Fornecedor', render: (c) => `${c.fornecedorId.slice(0, 8)}…` },
          { key: 'item', header: 'Item', render: (c) => `${c.itemId.slice(0, 8)}…` },
          { key: 'valor', header: 'Valor', align: 'end', sortAccessor: (c) => c.valor, render: (c) => formatarMoeda(c.valor) },
          {
            key: 'classificacao',
            header: 'Classificação',
            align: 'center',
            sortAccessor: (c) => c.classificacao ?? Number.MAX_SAFE_INTEGER,
            render: (c) => (c.classificacao == null ? '—' : `${c.classificacao}º`),
          },
          {
            key: 'situacao',
            header: 'Situação',
            render: (c) => <Tag variant={cotacaoTagVariant(c.situacao)}>{SITUACAO_COTACAO_LABEL[c.situacao]}</Tag>,
          },
        ];

        return (
          <>
            <PageHeader
              eyebrow="Dispensa eletrônica"
              title={dispensa.objeto}
              description={`${FUNDAMENTO_LABEL[dispensa.fundamento]} · ${CRITERIO_LABEL[dispensa.criterioJulgamento]}`}
              actions={
                <Link className="br-button secondary" to="/administracao/dispensas">
                  <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
                </Link>
              }
            />

            <Card className="mb-4">
              <dl className="row mb-0">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoTagVariant(dispensa.situacao)}>{SITUACAO_LABEL[dispensa.situacao]}</Tag>
                </Campo>
                <Campo rotulo="Valor total estimado">{formatarMoeda(dispensa.valorTotalEstimado)}</Campo>
                <Campo rotulo="Limite legal vigente">
                  {formatarMoeda(dispensa.limiteLegalVigente)}
                  <div className="text-down-02 text-gray-60 text-weight-regular">{dispensa.limiteLegalNormaFonte}</div>
                </Campo>
                <Campo rotulo="Aviso de contratação direta">{dispensa.numeroAviso ?? '—'}</Campo>
                <Campo rotulo="Abertura da disputa">{formatarDataHora(dispensa.aberturaDisputa)}</Campo>
                <Campo rotulo="PNCP">{dispensa.numeroPncp ?? '—'}</Campo>
              </dl>
            </Card>

            <Can permission="administracao.gerenciar">
              {!encerrada && (
                <Card className="mb-4" header={<strong>Ações do procedimento</strong>}>
                  <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                    {dispensa.situacao === 'Aberta' && (
                      <>
                        <Button variant="primary" onClick={() => setAcao('item')}>
                          <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
                        </Button>
                        <Button variant="secondary" onClick={() => setAcao('aviso')} disabled={dispensa.itens.length === 0}>
                          <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar aviso
                        </Button>
                      </>
                    )}
                    {dispensa.situacao === 'AvisoPublicado' && (
                      <Button
                        variant="primary"
                        loading={abrirDisputa.isPending}
                        onClick={() =>
                          abrirDisputa.mutate(undefined, {
                            onSuccess: () => toast.success('Disputa aberta.', 'Sucesso'),
                            onError: (e) =>
                              toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível abrir a disputa.'),
                          })
                        }
                      >
                        <i className="fas fa-gavel" aria-hidden="true" /> Abrir disputa
                      </Button>
                    )}
                    {dispensa.situacao === 'EmDisputa' && (
                      <>
                        <Button variant="primary" onClick={() => setAcao('lance')}>
                          <i className="fas fa-hand-holding-dollar" aria-hidden="true" /> Registrar lance
                        </Button>
                        <Button
                          variant="secondary"
                          loading={encerrarDisputa.isPending}
                          onClick={() =>
                            encerrarDisputa.mutate(undefined, {
                              onSuccess: () => toast.success('Disputa encerrada e julgada.', 'Sucesso'),
                              onError: (e) =>
                                toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível encerrar a disputa.'),
                            })
                          }
                        >
                          <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar e julgar
                        </Button>
                      </>
                    )}
                    {dispensa.situacao === 'EmJulgamento' && (
                      <Button variant="primary" onClick={() => setAcao('homologar')}>
                        <i className="fas fa-check-double" aria-hidden="true" /> Homologar
                      </Button>
                    )}
                    {(dispensa.situacao === 'EmDisputa' || dispensa.situacao === 'EmJulgamento') && (
                      <Button variant="secondary" onClick={() => setAcao('fracassar')}>
                        Declarar fracassada
                      </Button>
                    )}
                    {(dispensa.situacao === 'AvisoPublicado' || dispensa.situacao === 'EmDisputa') && (
                      <Button
                        variant="secondary"
                        loading={deserta.isPending}
                        onClick={() =>
                          deserta.mutate(undefined, {
                            onSuccess: () => toast.success('Dispensa declarada deserta.', 'Sucesso'),
                            onError: (e) =>
                              toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível declarar deserta.'),
                          })
                        }
                      >
                        Declarar deserta
                      </Button>
                    )}
                    <Button variant="danger" onClick={() => setAcao('revogar')}>
                      Revogar
                    </Button>
                    <Button variant="danger" onClick={() => setAcao('anular')}>
                      Anular
                    </Button>
                  </div>
                </Card>
              )}
            </Can>

            <Card className="mb-4" header={<strong>Itens</strong>}>
              <DataTable
                caption="Itens da dispensa"
                columns={itemColumns}
                rows={dispensa.itens}
                rowKey={(i) => i.itemId}
                empty={<EmptyState icon="fas fa-box" title="Sem itens" description="Adicione ao menos um item para publicar o aviso." />}
              />
            </Card>

            <Card header={<strong>Cotações (lances)</strong>}>
              <DataTable
                caption="Cotações recebidas"
                columns={cotacaoColumns}
                rows={dispensa.cotacoes}
                rowKey={(c) => c.cotacaoId}
                error={query.isError ? errorMessage(query.error) : null}
                empty={<EmptyState icon="fas fa-hand-holding-dollar" title="Sem cotações" description="Nenhum lance registrado." />}
              />
            </Card>

            <AdicionarItemModal open={acao === 'item'} onClose={fechar} dispensaId={id} />
            <PublicarAvisoModal open={acao === 'aviso'} onClose={fechar} dispensaId={id} />
            <RegistrarLanceModal open={acao === 'lance'} onClose={fechar} dispensaId={id} itens={dispensa.itens} />
            <HomologarModal open={acao === 'homologar'} onClose={fechar} dispensaId={id} />
            <MotivoModal acao="fracassar" open={acao === 'fracassar'} onClose={fechar} dispensaId={id} />
            <MotivoModal acao="revogar" open={acao === 'revogar'} onClose={fechar} dispensaId={id} />
            <MotivoModal acao="anular" open={acao === 'anular'} onClose={fechar} dispensaId={id} />
          </>
        );
      }}
    </QueryState>
  );
}

// --- Modais de ação (inline) -------------------------------------------------

interface AcaoProps {
  open: boolean;
  onClose: () => void;
  dispensaId: string;
}

function AdicionarItemModal({ open, onClose, dispensaId }: AcaoProps) {
  const toast = useToast();
  const mutation = useAdicionarItemDispensa(dispensaId);
  const [descricao, setDescricao] = useState('');
  const [quantidade, setQuantidade] = useState('');
  const [valorUnitario, setValorUnitario] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setDescricao('');
    setQuantidade('');
    setValorUnitario('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const qtd = Number(quantidade);
    const unit = Number(valorUnitario);
    if (descricao.trim() === '' || Number.isNaN(qtd) || qtd <= 0 || Number.isNaN(unit) || unit <= 0) {
      setErro('Informe descrição, quantidade e valor unitário positivos.');
      return;
    }
    mutation.mutate(
      { descricao: descricao.trim(), quantidade: qtd, valorUnitarioEstimado: unit, itemCatalogoId: null },
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
      title="Adicionar item"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-item-dispensa" loading={mutation.isPending}>
            Adicionar
          </Button>
        </>
      }
    >
      <form id="form-item-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          O valor total da dispensa não pode exceder o limite legal vigente; a inclusão que estouraria o teto é recusada
          (art. 75 Lei 14.133/2021).
        </p>
        <FormField label="Descrição" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={500} value={descricao} onChange={(e) => setDescricao(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-md-6">
            <FormField label="Quantidade" required>
              {({ id }) => (
                <Input id={id} type="number" min="0" step="0.0001" inputMode="decimal" value={quantidade} onChange={(e) => setQuantidade(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Valor unitário (R$)" required>
              {({ id }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={valorUnitario} onChange={(e) => setValorUnitario(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

function PublicarAvisoModal({ open, onClose, dispensaId }: AcaoProps) {
  const toast = useToast();
  const mutation = usePublicarAvisoDispensa(dispensaId);
  const [numeroAviso, setNumeroAviso] = useState('');
  const [abertura, setAbertura] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumeroAviso('');
    setAbertura('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numeroAviso.trim() === '' || abertura.trim() === '') {
      setErro('Informe o número do aviso e a data/hora de abertura da disputa.');
      return;
    }
    mutation.mutate(
      { numeroAviso: numeroAviso.trim(), aberturaDisputa: new Date(abertura).toISOString() },
      {
        onSuccess: () => {
          toast.success('Aviso publicado.', 'Sucesso');
          fechar();
        },
        onError: (e) => {
          if (e instanceof ApiError) setErro(e.userMessage);
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível publicar o aviso.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar aviso de contratação direta"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-aviso-dispensa" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-aviso-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          A abertura da disputa deve respeitar o prazo mínimo de divulgação do aviso (IN SEGES/ME 67/2021).
        </p>
        <FormField label="Número do aviso" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={60} value={numeroAviso} onChange={(e) => setNumeroAviso(e.target.value)} />
          )}
        </FormField>
        <FormField label="Abertura da disputa">
          {({ id }) => <Input id={id} type="datetime-local" value={abertura} onChange={(e) => setAbertura(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

function RegistrarLanceModal({ open, onClose, dispensaId, itens }: AcaoProps & { itens: ItemDispensaResumo[] }) {
  const toast = useToast();
  const mutation = useRegistrarLanceDispensa(dispensaId);
  const [itemId, setItemId] = useState('');
  const [fornecedorId, setFornecedorId] = useState('');
  const [valor, setValor] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const opcoesItens = itens.map((i) => ({ value: i.itemId, label: `${i.numero} — ${i.descricao}` }));

  function fechar(): void {
    setItemId('');
    setFornecedorId('');
    setValor('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const v = Number(valor);
    if (itemId === '' || fornecedorId.trim() === '' || Number.isNaN(v) || v <= 0) {
      setErro('Informe o item, o fornecedor e um valor positivo.');
      return;
    }
    mutation.mutate(
      { itemId, fornecedorId: fornecedorId.trim(), valor: v },
      {
        onSuccess: () => {
          toast.success('Lance registrado.', 'Sucesso');
          fechar();
        },
        onError: (e) => {
          if (e instanceof ApiError) setErro(e.userMessage);
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível registrar o lance.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar lance"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-lance-dispensa" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-lance-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">Lances sucessivos do mesmo fornecedor para o mesmo item devem melhorar a oferta.</p>
        <FormField label="Item" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid} placeholder="Selecione…" options={opcoesItens} value={itemId} onChange={(e) => setItemId(e.target.value)} />
          )}
        </FormField>
        <FormField label="Fornecedor (identificador)" required>
          {({ id }) => <Input id={id} value={fornecedorId} onChange={(e) => setFornecedorId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />}
        </FormField>
        <FormField label="Valor unitário ofertado (R$)" required>
          {({ id }) => <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={valor} onChange={(e) => setValor(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

function HomologarModal({ open, onClose, dispensaId }: AcaoProps) {
  const toast = useToast();
  const mutation = useHomologarDispensa(dispensaId);
  const [habilitado, setHabilitado] = useState(true);

  function fechar(): void {
    setHabilitado(true);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      { vencedorHabilitado: habilitado },
      {
        onSuccess: () => {
          toast.success('Dispensa homologada.', 'Sucesso');
          fechar();
        },
        onError: (e) => toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível homologar.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Homologar dispensa"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-homologar-dispensa" loading={mutation.isPending}>
            Homologar
          </Button>
        </>
      }
    >
      <form id="form-homologar-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          A homologação exige vencedor habilitado (regularidade fiscal, social e trabalhista — IN SEGES/ME 67/2021) e sem
          sanção impeditiva vigente (art. 14/156).
        </p>
        <FormField label="Vencedor habilitado?">
          {({ id }) => (
            <Select
              id={id}
              options={[
                { value: 'sim', label: 'Sim — habilitado' },
                { value: 'nao', label: 'Não — inabilitado' },
              ]}
              value={habilitado ? 'sim' : 'nao'}
              onChange={(e) => setHabilitado(e.target.value === 'sim')}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

function MotivoModal({ acao, open, onClose, dispensaId }: AcaoProps & { acao: 'fracassar' | 'revogar' | 'anular' }) {
  const toast = useToast();
  const fracassar = useDeclararDispensaFracassada(dispensaId);
  const revogar = useRevogarDispensa(dispensaId);
  const anular = useAnularDispensa(dispensaId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const config = {
    fracassar: { title: 'Declarar fracassada', mutation: fracassar, sucesso: 'Dispensa declarada fracassada.' },
    revogar: { title: 'Revogar dispensa', mutation: revogar, sucesso: 'Dispensa revogada.' },
    anular: { title: 'Anular dispensa', mutation: anular, sucesso: 'Dispensa anulada.' },
  }[acao];

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
      { motivo: motivo.trim() },
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
          <Button variant="danger" type="submit" form="form-motivo-dispensa" loading={config.mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-motivo-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Motivo" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
