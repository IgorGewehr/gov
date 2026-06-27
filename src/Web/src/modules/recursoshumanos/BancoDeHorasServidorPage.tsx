// BANCO DE HORAS de um servidor: saldo vivo (positivo a favor / negativo devido) + livro-razao de
// creditos/debitos/prescricao. Permite lancamento manual (compensacao/ajuste). O saldo e alimentado
// automaticamente pelo fechamento das apuracoes de ponto; a prescricao zera creditos fora da janela.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useParams, useLocation } from 'react-router-dom';
import {
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Modal,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData } from '../../i18n/format';
import {
  useExtratoBancoDeHoras,
  useLancarBancoDeHoras,
  formatarSaldoHoras,
} from './api';
import type { LancamentoBancoHorasView } from './api';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';

function LancarModal({
  open,
  onClose,
  servidorId,
}: {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}) {
  const toast = useToast();
  const mutation = useLancarBancoDeHoras(servidorId);
  const [horas, setHoras] = useState('');
  const [minutos, setMinutos] = useState('');
  const [credito, setCredito] = useState('true');
  const [data, setData] = useState('');
  const [descricao, setDescricao] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const totalMin = (Number(horas) || 0) * 60 + (Number(minutos) || 0);
    if (totalMin <= 0 || !data || !descricao.trim()) {
      setErro('Informe a quantidade (horas/minutos), a data e a descricao.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { minutos: totalMin, credito: credito === 'true', data, descricao: descricao.trim() },
      {
        onSuccess: () => {
          toast.success('Lancamento registrado.', 'Sucesso');
          onClose();
        },
        onError: (e) =>
          toast.error(e instanceof ApiError ? e.userMessage : 'Nao foi possivel lancar.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Lancamento manual no banco de horas"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-bh" loading={mutation.isPending}>
            Lancar
          </Button>
        </>
      }
    >
      <form id="form-bh" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Natureza" required error={erro}>
          {({ id }) => (
            <Select
              id={id}
              value={credito}
              onChange={(e) => setCredito(e.target.value)}
              options={[
                { value: 'true', label: 'Credito (a favor do servidor)' },
                { value: 'false', label: 'Debito (compensacao)' },
              ]}
            />
          )}
        </FormField>
        <FormRow>
          <FormField label="Horas">
            {({ id }) => (
              <Input id={id} type="number" min="0" value={horas} onChange={(e) => setHoras(e.target.value)} />
            )}
          </FormField>
          <FormField label="Minutos">
            {({ id }) => (
              <Input id={id} type="number" min="0" max="59" value={minutos} onChange={(e) => setMinutos(e.target.value)} />
            )}
          </FormField>
        </FormRow>
        <FormField label="Data" required>
          {({ id }) => <Input id={id} type="date" value={data} onChange={(e) => setData(e.target.value)} />}
        </FormField>
        <FormField label="Descricao" required>
          {({ id }) => <Input id={id} value={descricao} onChange={(e) => setDescricao(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

export function BancoDeHorasServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const location = useLocation();
  const nome = (location.state as { nome?: string } | null)?.nome ?? 'Servidor';

  const extrato = useExtratoBancoDeHoras(servidorId);
  const [aberto, setAberto] = useState(false);

  const saldo = extrato.data?.saldoMinutos ?? 0;

  const columns: Column<LancamentoBancoHorasView>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      render: (l) => (
        <Tag variant={l.tipo === 'Credito' ? 'success' : l.tipo === 'Prescricao' ? 'warning' : 'info'}>
          {l.tipo}
        </Tag>
      ),
    },
    { key: 'quantidade', header: 'Quantidade', align: 'end', render: (l) => formatarSaldoHoras(l.tipo === 'Credito' ? l.minutos : -l.minutos) },
    { key: 'data', header: 'Data', render: (l) => formatarData(l.data) },
    { key: 'descricao', header: 'Descricao', render: (l) => l.descricao },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title={`Banco de horas — ${nome}`}
        description="Saldo vivo de horas (extras a compensar/pagar e debitos). Alimentado pelo fechamento das apuracoes de ponto; ajustes manuais e prescricao por janela parametrizavel."
      />

      <CardSecao
        className="mb-4"
        titulo="Saldo atual"
        subtitulo="Positivo: horas a favor do servidor (a compensar/pagar). Negativo: horas devidas pelo servidor."
        acao={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Lancamento manual
              </Button>
            </Toolbar>
          </Can>
        }
      >
        <p className="tg-saldo-banco-horas" style={{ fontSize: '1.5rem', fontWeight: 600 }}>
          {extrato.isLoading ? '…' : formatarSaldoHoras(saldo)}
        </p>
      </CardSecao>

      <CardSecao titulo="Extrato (livro-razao)" subtitulo="Creditos, debitos e prescricoes, do mais recente para o mais antigo.">
        <DataTable
          caption="Lancamentos do banco de horas"
          columns={columns}
          rows={extrato.data?.lancamentos}
          rowKey={(l) => `${l.tipo}-${l.data}-${l.minutos}-${l.descricao}`}
          loading={extrato.isLoading}
          error={extrato.isError ? errorMessage(extrato.error) : null}
          empty={<EmptyState icon="fas fa-clock" title="Sem lancamentos" description="O banco de horas ainda nao tem movimentos." />}
        />
      </CardSecao>

      <LancarModal open={aberto} onClose={() => setAberto(false)} servidorId={servidorId} />
    </>
  );
}
