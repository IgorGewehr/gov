// Tela de LISTA/CADASTRO das Atas de Registro de Precos (ARP) — modulo Administracao (art. 82-86).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
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
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { AdministracaoSubNav } from '../AdministracaoSubNav';
import { SITUACAO_ATA_LABEL, useAtas, useRegistrarAta } from './ata.api';
import type { AtaResumo, SituacaoAta } from './ata.api';

function situacaoVariant(situacao: SituacaoAta): 'success' | 'default' | 'danger' {
  if (situacao === 'Vigente') return 'success';
  if (situacao === 'Cancelada') return 'danger';
  return 'default';
}

export function AtaListPage() {
  const navigate = useNavigate();
  const atas = useAtas();
  const [formAberto, setFormAberto] = useState(false);

  const columns: Column<AtaResumo>[] = [
    { key: 'numero', header: 'Número', sortAccessor: (a) => a.numero, render: (a) => a.numero },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (a) => a.situacao,
      render: (a) => <Tag variant={situacaoVariant(a.situacao)}>{SITUACAO_ATA_LABEL[a.situacao]}</Tag>,
    },
    { key: 'inicio', header: 'Início', render: (a) => formatarData(a.vigenciaInicio) },
    { key: 'fim', header: 'Fim', render: (a) => formatarData(a.vigenciaFim) },
    { key: 'itens', header: 'Itens', render: (a) => a.quantidadeItens },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (a) => (
        <Link className="br-button secondary small" to={`/administracao/atas/${a.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <PageHeader
        eyebrow="Compras e Licitações"
        title="Atas de Registro de Preços"
        description="Sistema de Registro de Preços — preços registrados, vigência, contratação e adesão/carona (art. 82-86, Lei 14.133/2021)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Registrar ata
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card header={<strong>Atas de Registro de Preços</strong>}>
        <DataTable
          caption="Atas de Registro de Preços"
          columns={columns}
          rows={atas.data}
          rowKey={(a) => a.id}
          loading={atas.isLoading}
          error={atas.isError ? errorMessage(atas.error) : null}
          onRowClick={(a) => navigate(`/administracao/atas/${a.id}`)}
          empty={
            <EmptyState
              icon="fas fa-file-contract"
              title="Nenhuma ata registrada"
              description="Registre uma Ata de Registro de Preços para controlar preços, saldos e adesões."
            />
          }
        />
      </Card>

      <AtaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        onRegistrada={(id) => navigate(`/administracao/atas/${id}`)}
      />
    </>
  );
}

interface AtaFormModalProps {
  open: boolean;
  onClose: () => void;
  onRegistrada: (id: string) => void;
}

function AtaFormModal({ open, onClose, onRegistrada }: AtaFormModalProps) {
  const toast = useToast();
  const registrar = useRegistrarAta();

  const [numero, setNumero] = useState('');
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      const { id } = await registrar.mutateAsync({
        numero: numero.trim(),
        vigenciaInicio: inicio,
        vigenciaFim: fim,
      });
      toast.success('Ata registrada.');
      setNumero('');
      setInicio('');
      setFim('');
      onClose();
      onRegistrada(id);
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const valido = numero.trim() !== '' && inicio !== '' && fim !== '' && fim > inicio;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar Ata de Registro de Preços"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="ata-form" disabled={!valido} loading={registrar.isPending}>
            Registrar
          </Button>
        </Toolbar>
      }
    >
      <form id="ata-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Número da ata" required>
          {({ id }) => <Input id={id} value={numero} onChange={(e) => setNumero(e.target.value)} maxLength={60} />}
        </FormField>
        <FormField label="Início da vigência" required>
          {({ id }) => <Input id={id} type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} />}
        </FormField>
        <FormField label="Fim da vigência" required help="Vigência máxima de 1 ano + prorrogação (art. 84).">
          {({ id }) => <Input id={id} type="date" value={fim} onChange={(e) => setFim(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}
