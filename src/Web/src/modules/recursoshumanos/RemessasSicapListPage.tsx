// SICAP-AP / SIAPESweb (auditoria de pessoal do TCE-RS): lista as remessas de atos de admissão e abre
// novas (sequencial do lote apurado por órgão no backend). O detalhe gera o arquivo de importação
// (leiaute estadual 57 posições) e marca a transmissão. Transmissão real ao SIAPESweb = M10.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
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
import { formatarData } from '../../i18n/format';
import { useRemessasSicap, useAbrirRemessaSicap } from './api';
import type { RemessaSicapResumo } from './api';
import {
  PERM_RH_GERENCIAR,
  SITUACOES_REMESSA_SICAP,
  situacaoRemessaSicapTagVariant,
} from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

function AbrirRemessaModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const mutation = useAbrirRemessaSicap();
  const [codigoOrgao, setCodigoOrgao] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const codigo = Number(codigoOrgao);
    if (!Number.isInteger(codigo) || codigo <= 0) {
      setErro('Informe o código do órgão (CD_ORGAO).');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      { codigoOrgao: codigo },
      {
        onSuccess: () => {
          toast.success('Remessa aberta.', 'Sucesso');
          setCodigoOrgao('');
          onClose();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a remessa.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Abrir remessa de pessoal"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-remessa" loading={mutation.isPending}>
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-remessa" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Código do órgão (CD_ORGAO)"
          required
          error={erro}
          help="O sequencial do lote (NRO_MOV) é atribuído automaticamente por órgão."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={codigoOrgao}
              onChange={(e) => setCodigoOrgao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

export function RemessasSicapListPage() {
  const [situacao, setSituacao] = useState('');
  const [filtroSituacao, setFiltroSituacao] = useState<number | null>(null);
  const [abrindo, setAbrindo] = useState(false);

  const query = useRemessasSicap(filtroSituacao);

  function buscar(event: FormEvent): void {
    event.preventDefault();
    const n = Number(situacao);
    setFiltroSituacao(situacao.trim() !== '' && !Number.isNaN(n) ? n : null);
  }

  const columns: Column<RemessaSicapResumo>[] = [
    {
      key: 'lote',
      header: 'Lote',
      render: (r) => (
        <span className="text-semi-bold">
          {String(r.codigoOrgao).padStart(6, '0')}/{String(r.sequencialLote).padStart(4, '0')}
        </span>
      ),
    },
    {
      key: 'data',
      header: 'Geração',
      sortAccessor: (r) => r.dataGeracaoLote,
      render: (r) => formatarData(r.dataGeracaoLote),
    },
    { key: 'atos', header: 'Atos', align: 'end', render: (r) => r.quantidadeAtos },
    {
      key: 'situacao',
      header: 'Situação',
      render: (r) => (
        <Tag variant={situacaoRemessaSicapTagVariant(r.situacao)}>{r.situacao}</Tag>
      ),
    },
    {
      key: 'protocolo',
      header: 'Protocolo',
      render: (r) => r.protocolo ?? '—',
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (r) => (
        <Link className="br-button secondary small" to={`/recursoshumanos/sicap-pessoal/${r.id}`}>
          Abrir
        </Link>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="SICAP-AP — Auditoria de Pessoal (TCE-RS)"
        description="Remessas de atos de admissão ao TCE-RS (SIAPESweb), no leiaute estadual de 57 posições. A transmissão real ao tribunal será habilitada na fase de integrações."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setAbrindo(true)}>
                <i className="fas fa-file-export" aria-hidden="true" /> Abrir remessa
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
            <FormField label="Situação">
              {({ id }) => (
                <Select
                  id={id}
                  value={situacao}
                  onChange={(e) => setSituacao(e.target.value)}
                  options={[{ value: '', label: 'Todas' }, ...SITUACOES_REMESSA_SICAP]}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Remessas de pessoal SICAP-AP"
        columns={columns}
        rows={query.data}
        rowKey={(r) => r.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-file-export"
            title="Nenhuma remessa"
            description="Abra uma remessa para incluir os atos de admissão a auditar pelo TCE-RS."
          />
        }
      />

      <AbrirRemessaModal open={abrindo} onClose={() => setAbrindo(false)} />
    </>
  );
}
