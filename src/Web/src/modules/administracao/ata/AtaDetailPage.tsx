// Tela de DETALHE de uma Ata de Registro de Precos (ARP): itens com saldo, adesoes (carona),
// registro de item, contratacao direta, adesao e cancelamento.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { AdministracaoSubNav } from '../AdministracaoSubNav';
import {
  SITUACAO_ATA_LABEL,
  useAta,
  useCancelarAta,
  useContratarItemAta,
  useRegistrarAdesao,
  useRegistrarItemAta,
} from './ata.api';
import type { AdesaoDetalhe, ItemAtaDetalhe } from './ata.api';

export function AtaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const ata = useAta(id);

  const registrarItem = useRegistrarItemAta(id);
  const registrarAdesao = useRegistrarAdesao(id);
  const contratar = useContratarItemAta(id);
  const cancelar = useCancelarAta(id);

  const [itemModal, setItemModal] = useState(false);
  const [adesaoItem, setAdesaoItem] = useState<ItemAtaDetalhe | null>(null);
  const [contratarItem, setContratarItem] = useState<ItemAtaDetalhe | null>(null);
  const [cancelarModal, setCancelarModal] = useState(false);

  const itensColumns: Column<ItemAtaDetalhe>[] = [
    { key: 'item', header: 'Item (catálogo)', render: (i) => i.itemCatalogoId },
    { key: 'preco', header: 'Preço registrado', render: (i) => formatarMoeda(i.precoRegistrado) },
    { key: 'reg', header: 'Qtd. registrada', render: (i) => i.quantidadeRegistrada },
    { key: 'contr', header: 'Qtd. contratada', render: (i) => i.quantidadeContratada },
    { key: 'saldo', header: 'Saldo', render: (i) => <strong>{i.saldoDisponivel}</strong> },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Can permission="administracao.gerenciar">
          <Toolbar>
            <Button variant="secondary" size="sm" onClick={() => setContratarItem(i)}>
              Contratar
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setAdesaoItem(i)}>
              Adesão
            </Button>
          </Toolbar>
        </Can>
      ),
    },
  ];

  const adesoesColumns: Column<AdesaoDetalhe>[] = [
    { key: 'orgao', header: 'Órgão aderente', render: (a) => a.orgaoAderente },
    { key: 'item', header: 'Item (catálogo)', render: (a) => a.itemCatalogoId },
    { key: 'qtd', header: 'Quantidade', render: (a) => a.quantidade },
    { key: 'data', header: 'Data', render: (a) => formatarData(a.data) },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <QueryState
        isLoading={ata.isLoading}
        isError={ata.isError}
        error={ata.error}
        data={ata.data}
      >
        {(detalhe) => (
          <>
            <PageHeader
              eyebrow="Atas de Registro de Preços"
              title={`Ata ${detalhe.numero}`}
              description={`Vigência: ${formatarData(detalhe.vigenciaInicio)} a ${formatarData(detalhe.vigenciaFim)}.`}
              actions={
                <Toolbar>
                  <Tag variant={detalhe.situacao === 'Vigente' ? 'success' : 'default'}>
                    {SITUACAO_ATA_LABEL[detalhe.situacao]}
                  </Tag>
                  {detalhe.situacao === 'Vigente' && (
                    <Can permission="administracao.gerenciar">
                      <Button variant="primary" onClick={() => setItemModal(true)}>
                        <i className="fas fa-plus" aria-hidden="true" /> Registrar item
                      </Button>
                      <Button variant="secondary" onClick={() => setCancelarModal(true)}>
                        Cancelar ata
                      </Button>
                    </Can>
                  )}
                </Toolbar>
              }
            />

            <Card className="mb-4" header={<strong>Itens registrados</strong>}>
              <DataTable
                caption="Itens registrados na ata"
                columns={itensColumns}
                rows={detalhe.itens}
                rowKey={(i) => i.itemAtaId}
                empty={
                  <EmptyState
                    icon="fas fa-list"
                    title="Nenhum item registrado"
                    description="Registre itens (preço + quantidade) para esta ata."
                  />
                }
              />
            </Card>

            <Card header={<strong>Adesões (carona)</strong>}>
              <DataTable
                caption="Adesões registradas"
                columns={adesoesColumns}
                rows={detalhe.adesoes}
                rowKey={(a) => `${a.itemCatalogoId}-${a.orgaoAderente}-${a.data}`}
                empty={
                  <EmptyState
                    icon="fas fa-handshake"
                    title="Nenhuma adesão"
                    description="Não há adesões (carona) registradas nesta ata."
                  />
                }
              />
            </Card>

            <RegistrarItemModal
              open={itemModal}
              onClose={() => setItemModal(false)}
              onSubmit={async (input) => {
                await registrarItem.mutateAsync(input);
                toast.success('Item registrado na ata.');
                setItemModal(false);
              }}
              pending={registrarItem.isPending}
            />

            <AdesaoModal
              item={adesaoItem}
              onClose={() => setAdesaoItem(null)}
              onSubmit={async (input) => {
                await registrarAdesao.mutateAsync(input);
                toast.success('Adesão registrada.');
                setAdesaoItem(null);
              }}
              pending={registrarAdesao.isPending}
            />

            <ContratarModal
              item={contratarItem}
              onClose={() => setContratarItem(null)}
              onSubmit={async (quantidade) => {
                await contratar.mutateAsync({ itemAtaId: contratarItem!.itemAtaId, quantidade });
                toast.success('Contratação registrada.');
                setContratarItem(null);
              }}
              pending={contratar.isPending}
            />

            <CancelarModal
              open={cancelarModal}
              onClose={() => setCancelarModal(false)}
              onSubmit={async (motivo) => {
                await cancelar.mutateAsync(motivo);
                toast.success('Ata cancelada.');
                setCancelarModal(false);
              }}
              pending={cancelar.isPending}
            />
          </>
        )}
      </QueryState>
    </>
  );
}

interface RegistrarItemModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (input: {
    itemCatalogoId: string;
    fornecedorBeneficiarioId: string;
    precoRegistrado: number;
    quantidadeRegistrada: number;
  }) => Promise<void>;
  pending: boolean;
}

function RegistrarItemModal({ open, onClose, onSubmit, pending }: RegistrarItemModalProps) {
  const toast = useToast();
  const [itemCatalogoId, setItemCatalogoId] = useState('');
  const [fornecedorId, setFornecedorId] = useState('');
  const [preco, setPreco] = useState('');
  const [qtd, setQtd] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await onSubmit({
        itemCatalogoId: itemCatalogoId.trim(),
        fornecedorBeneficiarioId: fornecedorId.trim(),
        precoRegistrado: Number(preco),
        quantidadeRegistrada: Number(qtd),
      });
      setItemCatalogoId('');
      setFornecedorId('');
      setPreco('');
      setQtd('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const valido = itemCatalogoId.trim() !== '' && fornecedorId.trim() !== '' && Number(preco) > 0 && Number(qtd) > 0;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar item na ata"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="ata-item-form" disabled={!valido} loading={pending}>
            Registrar
          </Button>
        </Toolbar>
      }
    >
      <form id="ata-item-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Id do item de catálogo" required help="Identificador (GUID) do item do catálogo.">
          {({ id }) => <Input id={id} value={itemCatalogoId} onChange={(e) => setItemCatalogoId(e.target.value)} />}
        </FormField>
        <FormField label="Id do fornecedor beneficiário" required>
          {({ id }) => <Input id={id} value={fornecedorId} onChange={(e) => setFornecedorId(e.target.value)} />}
        </FormField>
        <FormField label="Preço registrado (R$)" required>
          {({ id }) => <Input id={id} type="number" min="0" step="0.01" value={preco} onChange={(e) => setPreco(e.target.value)} />}
        </FormField>
        <FormField label="Quantidade registrada" required>
          {({ id }) => <Input id={id} type="number" min="0" step="0.0001" value={qtd} onChange={(e) => setQtd(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface AdesaoModalProps {
  item: ItemAtaDetalhe | null;
  onClose: () => void;
  onSubmit: (input: { itemAtaId: string; orgaoAderente: string; quantidade: number }) => Promise<void>;
  pending: boolean;
}

function AdesaoModal({ item, onClose, onSubmit, pending }: AdesaoModalProps) {
  const toast = useToast();
  const [orgao, setOrgao] = useState('');
  const [qtd, setQtd] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    if (!item) return;
    try {
      await onSubmit({ itemAtaId: item.itemAtaId, orgaoAderente: orgao.trim(), quantidade: Number(qtd) });
      setOrgao('');
      setQtd('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const valido = orgao.trim() !== '' && Number(qtd) > 0;

  return (
    <Modal
      open={item !== null}
      onClose={onClose}
      title="Registrar adesão (carona)"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="adesao-form" disabled={!valido} loading={pending}>
            Registrar adesão
          </Button>
        </Toolbar>
      }
    >
      <form id="adesao-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Órgão aderente" required>
          {({ id }) => <Input id={id} value={orgao} onChange={(e) => setOrgao(e.target.value)} maxLength={200} />}
        </FormField>
        <FormField label="Quantidade" required help={item ? `Saldo disponível: ${item.saldoDisponivel}.` : undefined}>
          {({ id }) => <Input id={id} type="number" min="0" step="0.0001" value={qtd} onChange={(e) => setQtd(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface ContratarModalProps {
  item: ItemAtaDetalhe | null;
  onClose: () => void;
  onSubmit: (quantidade: number) => Promise<void>;
  pending: boolean;
}

function ContratarModal({ item, onClose, onSubmit, pending }: ContratarModalProps) {
  const toast = useToast();
  const [qtd, setQtd] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await onSubmit(Number(qtd));
      setQtd('');
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  return (
    <Modal
      open={item !== null}
      onClose={onClose}
      title="Contratar item (uso direto da ata)"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="contratar-form" disabled={Number(qtd) <= 0} loading={pending}>
            Contratar
          </Button>
        </Toolbar>
      }
    >
      <form id="contratar-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Quantidade" required help={item ? `Saldo disponível: ${item.saldoDisponivel}.` : undefined}>
          {({ id }) => <Input id={id} type="number" min="0" step="0.0001" value={qtd} onChange={(e) => setQtd(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

interface CancelarModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (motivo: string) => Promise<void>;
  pending: boolean;
}

function CancelarModal({ open, onClose, onSubmit, pending }: CancelarModalProps) {
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
      title="Cancelar ata"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" type="submit" form="cancelar-form" disabled={motivo.trim() === ''} loading={pending}>
            Cancelar ata
          </Button>
        </Toolbar>
      }
    >
      <form id="cancelar-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Motivo" required>
          {({ id }) => <Input id={id} value={motivo} onChange={(e) => setMotivo(e.target.value)} maxLength={500} />}
        </FormField>
      </form>
    </Modal>
  );
}
