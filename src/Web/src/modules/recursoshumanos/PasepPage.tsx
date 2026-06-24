// Painel do PASEP: apuração da contribuição do ente sobre a folha (base = folha bruta da competência;
// valor = base x alíquota). Transmissão/recolhimento real = M10. Lista por ano + modal de apuração.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
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
import { formatarMoeda } from '../../i18n/format';
import { useApuracoesPasep, useApurarPasep } from './api';
import type { ApuracaoPasepResumo, ApurarPasepInput } from './api';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

const MESES = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
];

function ApurarPasepModal({ open, onClose, ano }: { open: boolean; onClose: () => void; ano: number }) {
  const toast = useToast();
  const mutation = useApurarPasep();
  const [mes, setMes] = useState('');
  const [aliquota, setAliquota] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const m = Number(mes);
    if (mes.trim() === '' || !Number.isInteger(m) || m < 1 || m > 12) {
      setErro('Selecione a competência (mês).');
      return;
    }
    setErro(undefined);

    const input: ApurarPasepInput = {
      ano,
      mes: m,
      aliquotaOverride: aliquota.trim() ? Number(aliquota) : null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('PASEP apurado.', 'Sucesso');
        setMes('');
        setAliquota('');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível apurar o PASEP.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Apurar PASEP — ${ano}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-apurar-pasep" loading={mutation.isPending}>
            Apurar
          </Button>
        </>
      }
    >
      <form id="form-apurar-pasep" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Competência (mês)"
          required
          error={erro}
          help="A base é a folha bruta apurada na competência."
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={mes}
              onChange={(e) => setMes(e.target.value)}
              placeholder="Selecione o mês"
              options={MESES.map((nome, i) => ({ value: String(i + 1), label: nome }))}
            />
          )}
        </FormField>

        <FormField
          label="Alíquota (%)"
          help="Opcional; em branco usa a parametrizada do tenant (1% padrão)."
        >
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.0001"
              inputMode="decimal"
              aria-describedby={describedBy}
              value={aliquota}
              onChange={(e) => setAliquota(e.target.value)}
              placeholder="1.00"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

export function PasepPage() {
  const anoCorrente = new Date().getFullYear();
  const [ano, setAno] = useState(anoCorrente);
  const [campoAno, setCampoAno] = useState(String(anoCorrente));
  const [apurando, setApurando] = useState(false);

  const query = useApuracoesPasep(ano);

  function buscar(event: FormEvent): void {
    event.preventDefault();
    const a = Number(campoAno);
    if (Number.isInteger(a) && a >= 2000 && a <= 2100) {
      setAno(a);
    }
  }

  const columns: Column<ApuracaoPasepResumo>[] = [
    {
      key: 'competencia',
      header: 'Competência',
      sortAccessor: (a) => a.mes,
      render: (a) => `${MESES[a.mes - 1]}/${a.ano}`,
    },
    {
      key: 'base',
      header: 'Base (folha bruta)',
      align: 'end',
      sortAccessor: (a) => a.baseContribuicao,
      render: (a) => formatarMoeda(a.baseContribuicao),
    },
    {
      key: 'aliquota',
      header: 'Alíquota',
      align: 'end',
      render: (a) => `${a.aliquota.toLocaleString('pt-BR', { maximumFractionDigits: 4 })}%`,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (a) => a.valor,
      render: (a) => formatarMoeda(a.valor),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (a) => (
        <Tag variant={a.situacao === 'Transmitida' ? 'success' : 'info'}>{a.situacao}</Tag>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="PASEP"
        description="Contribuição do ente sobre a folha de pagamento (LC 8/1970). Apuração da base e do valor; transmissão ao agente arrecadador será habilitada na fase de integrações."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setApurando(true)}>
                <i className="fas fa-calculator" aria-hidden="true" /> Apurar competência
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit">
                <i className="fas fa-search" aria-hidden="true" /> Buscar
              </Button>
            }
          >
            <FormField label="Exercício">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="number"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  value={campoAno}
                  onChange={(e) => setCampoAno(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption={`Apurações de PASEP de ${ano}`}
        columns={columns}
        rows={query.data}
        rowKey={(a) => a.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-calculator"
            title="Nenhuma apuração no exercício"
            description="Apure o PASEP de uma competência para vê-la aqui."
          />
        }
      />

      <ApurarPasepModal open={apurando} onClose={() => setApurando(false)} ano={ano} />
    </>
  );
}
