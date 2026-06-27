// Tela do Plano de Contratacoes Anual (PCA) por exercicio — modulo Administracao (art. 12, VII, Lei 14.133/2021).
// Abre o plano do exercicio, inclui/remove itens, aprova e publica no PNCP.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  FormRow,
  Input,
  Modal,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { AdministracaoSubNav } from '../AdministracaoSubNav';
import {
  FONTE_CONTRATACAO_LABEL,
  SITUACAO_PCA_LABEL,
  useAbrirPca,
  useAprovarPca,
  useIncluirItemPca,
  usePcaPorExercicio,
  usePublicarPca,
  useRemoverItemPca,
  useRevisarPca,
  useVincularContratacaoItemPca,
} from './pca.api';
import type { FonteContratacaoPca, ItemPcaDetalhe, SituacaoPca, VincularContratacaoItemInput } from './pca.api';

function situacaoVariant(situacao: SituacaoPca): 'success' | 'warning' | 'info' | 'default' {
  if (situacao === 'Publicado') return 'success';
  if (situacao === 'Aprovado') return 'warning';
  if (situacao === 'EmRevisao') return 'info';
  return 'default';
}

export function PcaPage() {
  const toast = useToast();
  const anoAtual = new Date().getFullYear();
  const [exercicio, setExercicio] = useState(anoAtual + 1);

  const pca = usePcaPorExercicio(exercicio);
  const abrir = useAbrirPca(exercicio);

  const plano = pca.data ?? null;
  const pcaId = plano?.id ?? '';

  const incluir = useIncluirItemPca(pcaId, exercicio);
  const remover = useRemoverItemPca(pcaId, exercicio);
  const aprovar = useAprovarPca(pcaId, exercicio);
  const publicar = usePublicarPca(pcaId, exercicio);
  const revisar = useRevisarPca(pcaId, exercicio);
  const vincular = useVincularContratacaoItemPca(pcaId, exercicio);

  const [itemModal, setItemModal] = useState(false);
  const [publicarModal, setPublicarModal] = useState(false);
  const [revisarModal, setRevisarModal] = useState(false);
  const [vincularItem, setVincularItem] = useState<ItemPcaDetalhe | null>(null);

  const emElaboracao = plano?.situacao === 'EmElaboracao';
  const emRevisao = plano?.situacao === 'EmRevisao';
  // Itens são editáveis em elaboração OU durante uma revisão formal.
  const editavel = emElaboracao || emRevisao;
  // O vínculo de execução só faz sentido com o plano já vigente (não em rascunho).
  const podeVincular = plano != null && plano.situacao !== 'EmElaboracao';

  async function abrirPlano(): Promise<void> {
    try {
      await abrir.mutateAsync();
      toast.success('PCA aberto.');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  async function aprovarPlano(): Promise<void> {
    try {
      await aprovar.mutateAsync();
      toast.success(emRevisao ? 'Revisão concluída e PCA reaprovado.' : 'PCA aprovado.');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  async function removerItem(item: ItemPcaDetalhe): Promise<void> {
    try {
      await remover.mutateAsync(item.itemPcaId);
      toast.success('Item removido.');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const columns: Column<ItemPcaDetalhe>[] = [
    { key: 'item', header: 'Item (catálogo)', render: (i) => i.itemCatalogoId },
    { key: 'qtd', header: 'Quantidade', render: (i) => i.quantidade },
    { key: 'valor', header: 'Valor estimado', render: (i) => formatarMoeda(i.valorEstimado) },
    { key: 'trim', header: 'Trimestre', render: (i) => `${i.trimestreDesejado}º` },
    {
      key: 'fonte',
      header: 'Contratação gerada',
      render: (i) =>
        i.fonteContratacao ? (
          <span title={i.contratacaoReferenciaId ?? undefined}>
            <Tag variant="success">{FONTE_CONTRATACAO_LABEL[i.fonteContratacao]}</Tag>
            {i.contratacaoIdentificacao ? ` ${i.contratacaoIdentificacao}` : ''}
          </span>
        ) : (
          <Tag variant="default">Não contratado</Tag>
        ),
    },
    { key: 'just', header: 'Justificativa', render: (i) => i.justificativa ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Can permission="administracao.gerenciar">
          <Toolbar>
            {editavel && !i.fonteContratacao && (
              <Button variant="secondary" size="sm" onClick={() => void removerItem(i)}>
                Remover
              </Button>
            )}
            {podeVincular && !i.fonteContratacao && (
              <Button variant="secondary" size="sm" onClick={() => setVincularItem(i)}>
                Vincular contratação
              </Button>
            )}
            {!editavel && i.fonteContratacao && '—'}
          </Toolbar>
        </Can>
      ),
    },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <PageHeader
        eyebrow="Compras e Licitações"
        title="Plano de Contratações Anual (PCA)"
        description="Consolida as contratações pretendidas do exercício para subsidiar o orçamento (art. 12, VII, Lei 14.133/2021)."
      />

      <Card className="mb-4" header={<strong>Exercício</strong>}>
        <FormRow>
          <FormField label="Exercício (ano)">
            {({ id }) => (
              <Input
                id={id}
                type="number"
                min="2000"
                max="2100"
                value={String(exercicio)}
                onChange={(e) => setExercicio(Number(e.target.value))}
              />
            )}
          </FormField>
        </FormRow>
      </Card>

      {pca.isLoading && <Alert variant="info" title="Carregando">Consultando o PCA do exercício…</Alert>}
      {pca.isError && <Alert variant="danger" title="Erro">{errorMessage(pca.error)}</Alert>}

      {!pca.isLoading && !pca.isError && plano === null && (
        <Card>
          <EmptyState
            icon="fas fa-calendar-plus"
            title={`Nenhum PCA para ${exercicio}`}
            description="Abra o Plano de Contratações Anual deste exercício para começar a incluir itens."
            action={
              <Can permission="administracao.gerenciar">
                <Button variant="primary" onClick={() => void abrirPlano()} loading={abrir.isPending}>
                  Abrir PCA {exercicio}
                </Button>
              </Can>
            }
          />
        </Card>
      )}

      {plano !== null && (
        <Card
          header={
            <Toolbar>
              <strong>PCA {plano.exercicio}</strong>
              <Tag variant={situacaoVariant(plano.situacao)}>{SITUACAO_PCA_LABEL[plano.situacao]}</Tag>
              {plano.numeroRevisao > 0 && <span>Revisão nº {plano.numeroRevisao}</span>}
              {plano.numeroPncp && <span>PNCP: {plano.numeroPncp}</span>}
            </Toolbar>
          }
        >
          <Alert variant="info" title="Valor total estimado">
            {formatarMoeda(plano.valorTotalEstimado)} em {plano.itens.length} item(ns).
          </Alert>

          {emRevisao && (
            <Alert variant="warning" title="Plano em revisão">
              {plano.motivoRevisaoAtual ?? 'Revisão formal em curso.'} Conclua a revisão para reaprovar o plano.
            </Alert>
          )}

          <Toolbar className="mb-3">
            <Can permission="administracao.gerenciar">
              {editavel && (
                <Button variant="primary" onClick={() => setItemModal(true)}>
                  <i className="fas fa-plus" aria-hidden="true" /> Incluir item
                </Button>
              )}
              {editavel && (
                <Button
                  variant="secondary"
                  onClick={() => void aprovarPlano()}
                  disabled={plano.itens.length === 0}
                  loading={aprovar.isPending}
                >
                  {emRevisao ? 'Concluir revisão' : 'Aprovar PCA'}
                </Button>
              )}
              {plano.situacao === 'Aprovado' && (
                <Button variant="primary" onClick={() => setPublicarModal(true)}>
                  Publicar no PNCP
                </Button>
              )}
              {(plano.situacao === 'Aprovado' || plano.situacao === 'Publicado') && (
                <Button variant="secondary" onClick={() => setRevisarModal(true)}>
                  <i className="fas fa-pen-to-square" aria-hidden="true" /> Revisar
                </Button>
              )}
            </Can>
          </Toolbar>

          <DataTable
            caption={`Itens do PCA ${plano.exercicio}`}
            columns={columns}
            rows={plano.itens}
            rowKey={(i) => i.itemPcaId}
            empty={
              <EmptyState
                icon="fas fa-list"
                title="Nenhum item"
                description="Inclua itens de contratação pretendida no plano."
              />
            }
          />
        </Card>
      )}

      <IncluirItemModal
        open={itemModal}
        onClose={() => setItemModal(false)}
        onSubmit={async (input) => {
          await incluir.mutateAsync(input);
          toast.success('Item incluído.');
          setItemModal(false);
        }}
        pending={incluir.isPending}
      />

      <PublicarModal
        open={publicarModal}
        onClose={() => setPublicarModal(false)}
        onSubmit={async (numeroPncp) => {
          await publicar.mutateAsync(numeroPncp);
          toast.success('PCA publicado no PNCP.');
          setPublicarModal(false);
        }}
        pending={publicar.isPending}
      />

      <RevisarModal
        open={revisarModal}
        onClose={() => setRevisarModal(false)}
        onSubmit={async (motivo) => {
          await revisar.mutateAsync(motivo);
          toast.success('Plano reaberto para revisão.');
          setRevisarModal(false);
        }}
        pending={revisar.isPending}
      />

      <VincularContratacaoModal
        item={vincularItem}
        onClose={() => setVincularItem(null)}
        onSubmit={async (itemPcaId, input) => {
          await vincular.mutateAsync({ itemPcaId, input });
          toast.success('Contratação vinculada ao item.');
          setVincularItem(null);
        }}
        pending={vincular.isPending}
      />
    </>
  );
}

interface IncluirItemModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (input: {
    itemCatalogoId: string;
    quantidade: number;
    valorEstimado: number;
    trimestreDesejado: number;
    justificativa?: string | null;
  }) => Promise<void>;
  pending: boolean;
}

function IncluirItemModal({ open, onClose, onSubmit, pending }: IncluirItemModalProps) {
  const toast = useToast();
  const [itemCatalogoId, setItemCatalogoId] = useState('');
  const [qtd, setQtd] = useState('');
  const [valor, setValor] = useState('');
  const [trimestre, setTrimestre] = useState('1');
  const [justificativa, setJustificativa] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await onSubmit({
        itemCatalogoId: itemCatalogoId.trim(),
        quantidade: Number(qtd),
        valorEstimado: Number(valor),
        trimestreDesejado: Number(trimestre),
        justificativa: justificativa.trim() === '' ? null : justificativa.trim(),
      });
      setItemCatalogoId('');
      setQtd('');
      setValor('');
      setTrimestre('1');
      setJustificativa('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const valido =
    itemCatalogoId.trim() !== '' && Number(qtd) > 0 && Number(valor) > 0 && [1, 2, 3, 4].includes(Number(trimestre));

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Incluir item no PCA"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="pca-item-form" disabled={!valido} loading={pending}>
            Incluir
          </Button>
        </Toolbar>
      }
    >
      <form id="pca-item-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Id do item de catálogo" required>
          {({ id }) => <Input id={id} value={itemCatalogoId} onChange={(e) => setItemCatalogoId(e.target.value)} />}
        </FormField>
        <FormField label="Quantidade" required>
          {({ id }) => <Input id={id} type="number" min="0" step="0.0001" value={qtd} onChange={(e) => setQtd(e.target.value)} />}
        </FormField>
        <FormField label="Valor estimado (R$)" required>
          {({ id }) => <Input id={id} type="number" min="0" step="0.01" value={valor} onChange={(e) => setValor(e.target.value)} />}
        </FormField>
        <FormField label="Trimestre desejado (1 a 4)" required>
          {({ id }) => <Input id={id} type="number" min="1" max="4" value={trimestre} onChange={(e) => setTrimestre(e.target.value)} />}
        </FormField>
        <FormField label="Justificativa">
          {({ id }) => <Input id={id} value={justificativa} onChange={(e) => setJustificativa(e.target.value)} maxLength={2000} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface PublicarModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (numeroPncp: string) => Promise<void>;
  pending: boolean;
}

function PublicarModal({ open, onClose, onSubmit, pending }: PublicarModalProps) {
  const toast = useToast();
  const [numero, setNumero] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await onSubmit(numero.trim());
      setNumero('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Publicar PCA no PNCP"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="publicar-form" disabled={numero.trim() === ''} loading={pending}>
            Publicar
          </Button>
        </Toolbar>
      }
    >
      <form id="publicar-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <Alert variant="warning" title="Transmissão real">
          A transmissão automática ao PNCP será habilitada com credencial (M10). Informe o número de controle do PNCP.
        </Alert>
        <FormField label="Número de controle PNCP" required>
          {({ id }) => <Input id={id} value={numero} onChange={(e) => setNumero(e.target.value)} maxLength={60} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface RevisarModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (motivo: string) => Promise<void>;
  pending: boolean;
}

function RevisarModal({ open, onClose, onSubmit, pending }: RevisarModalProps) {
  const toast = useToast();
  const [motivo, setMotivo] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await onSubmit(motivo.trim());
      setMotivo('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Revisar PCA"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="revisar-form" disabled={motivo.trim() === ''} loading={pending}>
            Reabrir para revisão
          </Button>
        </Toolbar>
      }
    >
      <form id="revisar-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <Alert variant="info" title="Revisão formal">
          A revisão reabre o plano vigente para alteração de itens. Ao concluir, o plano é reaprovado (a versão de
          revisão é incrementada) e, se necessário, republicado no PNCP.
        </Alert>
        <FormField label="Motivo da revisão" required>
          {({ id }) => <Input id={id} value={motivo} onChange={(e) => setMotivo(e.target.value)} maxLength={1000} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface VincularContratacaoModalProps {
  item: ItemPcaDetalhe | null;
  onClose: () => void;
  onSubmit: (itemPcaId: string, input: VincularContratacaoItemInput) => Promise<void>;
  pending: boolean;
}

const FONTE_OPTIONS: { value: FonteContratacaoPca; label: string }[] = [
  { value: 'Licitacao', label: FONTE_CONTRATACAO_LABEL.Licitacao },
  { value: 'AtaRegistroPrecos', label: FONTE_CONTRATACAO_LABEL.AtaRegistroPrecos },
  { value: 'Dispensa', label: FONTE_CONTRATACAO_LABEL.Dispensa },
  { value: 'Inexigibilidade', label: FONTE_CONTRATACAO_LABEL.Inexigibilidade },
];

function VincularContratacaoModal({ item, onClose, onSubmit, pending }: VincularContratacaoModalProps) {
  const toast = useToast();
  const [fonte, setFonte] = useState<FonteContratacaoPca>('Licitacao');
  const [referenciaId, setReferenciaId] = useState('');
  const [identificacao, setIdentificacao] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    if (item === null) return;
    try {
      await onSubmit(item.itemPcaId, {
        fonte,
        referenciaId: referenciaId.trim(),
        identificacao: identificacao.trim() === '' ? null : identificacao.trim(),
      });
      setFonte('Licitacao');
      setReferenciaId('');
      setIdentificacao('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  return (
    <Modal
      open={item !== null}
      onClose={onClose}
      title="Vincular contratação ao item"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="vincular-form" disabled={referenciaId.trim() === ''} loading={pending}>
            Vincular
          </Button>
        </Toolbar>
      }
    >
      <form id="vincular-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <Alert variant="info" title="Rastreabilidade planejamento → execução">
          Vincule o item planejado ao instrumento que o concretizou (licitação, ata, dispensa ou inexigibilidade),
          para aferir o cumprimento do PCA (art. 12, VII; Dec. 11.246/2022).
        </Alert>
        <FormField label="Natureza da contratação" required>
          {({ id }) => (
            <Select
              id={id}
              options={FONTE_OPTIONS}
              value={fonte}
              onChange={(e) => setFonte(e.target.value as FonteContratacaoPca)}
            />
          )}
        </FormField>
        <FormField label="Identificador do instrumento gerado (GUID)" required>
          {({ id }) => (
            <Input
              id={id}
              value={referenciaId}
              onChange={(e) => setReferenciaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Identificação legível (opcional)">
          {({ id }) => (
            <Input id={id} value={identificacao} onChange={(e) => setIdentificacao(e.target.value)} maxLength={200} placeholder="Ex.: Edital PE 12/2027" />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
