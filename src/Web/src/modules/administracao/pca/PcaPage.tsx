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
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { AdministracaoSubNav } from '../AdministracaoSubNav';
import {
  SITUACAO_PCA_LABEL,
  useAbrirPca,
  useAprovarPca,
  useIncluirItemPca,
  usePcaPorExercicio,
  usePublicarPca,
  useRemoverItemPca,
} from './pca.api';
import type { ItemPcaDetalhe, SituacaoPca } from './pca.api';

function situacaoVariant(situacao: SituacaoPca): 'success' | 'warning' | 'default' {
  if (situacao === 'Publicado') return 'success';
  if (situacao === 'Aprovado') return 'warning';
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

  const [itemModal, setItemModal] = useState(false);
  const [publicarModal, setPublicarModal] = useState(false);

  const emElaboracao = plano?.situacao === 'EmElaboracao';

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
      toast.success('PCA aprovado.');
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
    { key: 'just', header: 'Justificativa', render: (i) => i.justificativa ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) =>
        emElaboracao ? (
          <Can permission="administracao.gerenciar">
            <Button variant="secondary" size="sm" onClick={() => void removerItem(i)}>
              Remover
            </Button>
          </Can>
        ) : (
          '—'
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
              {plano.numeroPncp && <span>PNCP: {plano.numeroPncp}</span>}
            </Toolbar>
          }
        >
          <Alert variant="info" title="Valor total estimado">
            {formatarMoeda(plano.valorTotalEstimado)} em {plano.itens.length} item(ns).
          </Alert>

          <Toolbar className="mb-3">
            <Can permission="administracao.gerenciar">
              {emElaboracao && (
                <>
                  <Button variant="primary" onClick={() => setItemModal(true)}>
                    <i className="fas fa-plus" aria-hidden="true" /> Incluir item
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => void aprovarPlano()}
                    disabled={plano.itens.length === 0}
                    loading={aprovar.isPending}
                  >
                    Aprovar PCA
                  </Button>
                </>
              )}
              {plano.situacao === 'Aprovado' && (
                <Button variant="primary" onClick={() => setPublicarModal(true)}>
                  Publicar no PNCP
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
