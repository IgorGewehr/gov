// Processos Trabalhistas: cadastro/acompanhamento (número CNJ, vara, reclamante, objeto, valores,
// situação) com a PROVISÃO contábil vigente (NBC TG 25) em destaque. Busca paginada + cadastro via
// modal. As movimentações (reavaliar prognóstico, acordo, condenação, improcedência, arquivar) ficam
// no modal de ações da linha.
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
import {
  useBuscarProcessos,
  useCadastrarProcesso,
  useProvisaoTrabalhista,
} from './api';
import type {
  CadastrarProcessoInput,
  FiltroProcessos,
  ProcessoTrabalhistaResumo,
} from './api';
import {
  PERM_RH_GERENCIAR,
  PROGNOSTICOS_PERDA,
  prognosticoTagVariant,
  SITUACOES_PROCESSO,
  situacaoProcessoTagVariant,
} from './recursosHumanos.helpers';
import { ProcessoAcoesModal } from './ProcessoAcoesModal';
import { RhSubNav } from './RhSubNav';

const FILTRO_INICIAL: FiltroProcessos = {
  situacao: null,
  prognostico: null,
  termo: null,
  pagina: 1,
};

function paraNumeroOuNull(valor: string): number | null {
  const n = Number(valor);
  return valor.trim() !== '' && !Number.isNaN(n) ? n : null;
}

function CadastrarProcessoModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const mutation = useCadastrarProcesso();
  const [form, setForm] = useState<CadastrarProcessoInput>({
    numeroProcesso: '',
    vara: '',
    reclamante: '',
    objeto: '',
    valorCausa: 0,
    dataAjuizamento: new Date().toISOString().slice(0, 10),
    prognostico: 2,
  });
  const [erro, setErro] = useState<string | undefined>();

  function set<K extends keyof CadastrarProcessoInput>(
    chave: K,
    valor: CadastrarProcessoInput[K],
  ): void {
    setForm((atual) => ({ ...atual, [chave]: valor }));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (
      form.numeroProcesso.trim() === '' ||
      form.vara.trim() === '' ||
      form.reclamante.trim() === '' ||
      form.objeto.trim() === ''
    ) {
      setErro('Número, vara, reclamante e objeto são obrigatórios.');
      return;
    }
    setErro(undefined);

    mutation.mutate(form, {
      onSuccess: () => {
        toast.success('Processo cadastrado.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o processo.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Cadastrar processo trabalhista"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-processo" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-processo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Número do processo (CNJ)" required error={erro}>
          {({ id }) => (
            <Input
              id={id}
              value={form.numeroProcesso}
              onChange={(e) => set('numeroProcesso', e.target.value)}
              placeholder="0000000-00.0000.0.00.0000"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-md-6">
            <FormField label="Vara / órgão julgador" required>
              {({ id }) => (
                <Input id={id} value={form.vara} onChange={(e) => set('vara', e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Reclamante" required>
              {({ id }) => (
                <Input
                  id={id}
                  value={form.reclamante}
                  onChange={(e) => set('reclamante', e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Objeto / pedidos" required>
          {({ id }) => (
            <Input id={id} value={form.objeto} onChange={(e) => set('objeto', e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-md-4">
            <FormField label="Valor da causa (R$)">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  value={String(form.valorCausa)}
                  onChange={(e) => set('valorCausa', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Data de ajuizamento">
              {({ id }) => (
                <Input
                  id={id}
                  type="date"
                  value={form.dataAjuizamento}
                  onChange={(e) => set('dataAjuizamento', e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Prognóstico de perda" help="Provável gera provisão (NBC TG 25).">
              {({ id }) => (
                <Select
                  id={id}
                  value={String(form.prognostico)}
                  onChange={(e) => set('prognostico', Number(e.target.value))}
                  options={PROGNOSTICOS_PERDA}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

export function ProcessosTrabalhistasListPage() {
  const [situacao, setSituacao] = useState('');
  const [prognostico, setPrognostico] = useState('');
  const [termo, setTermo] = useState('');
  const [filtro, setFiltro] = useState<FiltroProcessos>(FILTRO_INICIAL);
  const [cadastrando, setCadastrando] = useState(false);
  const [selecionado, setSelecionado] = useState<ProcessoTrabalhistaResumo | null>(null);

  const query = useBuscarProcessos(filtro);
  const provisao = useProvisaoTrabalhista();
  const totalPaginas = query.data
    ? Math.max(1, Math.ceil(query.data.total / query.data.tamanho))
    : 1;

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setFiltro({
      situacao: paraNumeroOuNull(situacao),
      prognostico: paraNumeroOuNull(prognostico),
      termo: termo.trim() || null,
      pagina: 1,
    });
  }

  function irParaPagina(pagina: number): void {
    setFiltro((atual) => ({ ...atual, pagina }));
  }

  const columns: Column<ProcessoTrabalhistaResumo>[] = [
    {
      key: 'numero',
      header: 'Processo',
      render: (p) => <span className="text-semi-bold">{p.numeroProcesso}</span>,
    },
    { key: 'reclamante', header: 'Reclamante', render: (p) => p.reclamante },
    { key: 'vara', header: 'Vara', render: (p) => p.vara },
    {
      key: 'causa',
      header: 'Valor da causa',
      align: 'end',
      sortAccessor: (p) => p.valorCausa,
      render: (p) => formatarMoeda(p.valorCausa),
    },
    {
      key: 'provisao',
      header: 'Provisionado',
      align: 'end',
      sortAccessor: (p) => p.valorProvisionado,
      render: (p) => formatarMoeda(p.valorProvisionado),
    },
    {
      key: 'prognostico',
      header: 'Prognóstico',
      render: (p) => <Tag variant={prognosticoTagVariant(p.prognostico)}>{p.prognostico}</Tag>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (p) => (
        <Tag variant={situacaoProcessoTagVariant(p.situacao)}>{p.situacao}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Button variant="secondary" size="sm" onClick={() => setSelecionado(p)}>
          Movimentar
        </Button>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Processos Trabalhistas"
        description="Cadastro e acompanhamento das reclamatórias contra o ente, com controle da provisão contábil (NBC TG 25): só o prognóstico provável reconhece passivo."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setCadastrando(true)}>
                <i className="fas fa-gavel" aria-hidden="true" /> Cadastrar processo
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <div className="d-flex align-items-center" style={{ gap: '0.75rem' }}>
          <i className="fas fa-balance-scale-right fa-2x text-primary" aria-hidden="true" />
          <div>
            <span className="d-block text-down-01 text-secondary">
              Provisão trabalhista vigente (perda provável)
            </span>
            <strong className="text-up-02" aria-live="polite">
              {provisao.data ? formatarMoeda(provisao.data.totalProvisionado) : '—'}
            </strong>
          </div>
        </div>
      </Card>

      <Card className="mb-4">
        <form className="br-form" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit">
                <i className="fas fa-search" aria-hidden="true" /> Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-6 col-md-3">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      value={situacao}
                      onChange={(e) => setSituacao(e.target.value)}
                      options={[{ value: '', label: 'Todas' }, ...SITUACOES_PROCESSO]}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6 col-md-3">
                <FormField label="Prognóstico">
                  {({ id }) => (
                    <Select
                      id={id}
                      value={prognostico}
                      onChange={(e) => setPrognostico(e.target.value)}
                      options={[{ value: '', label: 'Todos' }, ...PROGNOSTICOS_PERDA]}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-12 col-md-6">
                <FormField label="Nº ou reclamante">
                  {({ id }) => (
                    <Input
                      id={id}
                      value={termo}
                      onChange={(e) => setTermo(e.target.value)}
                      placeholder="Busca livre"
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Resultado da busca de processos trabalhistas"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-gavel"
            title="Nenhum processo encontrado"
            description="Ajuste os filtros ou cadastre um novo processo trabalhista."
          />
        }
      />

      {query.data && query.data.total > 0 && (
        <nav className="d-flex align-items-center mt-3" aria-label="Paginação" style={{ gap: '0.75rem' }}>
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina - 1)}
            disabled={filtro.pagina <= 1 || query.isFetching}
          >
            Anterior
          </Button>
          <span aria-live="polite">
            Página {filtro.pagina} de {totalPaginas} ({query.data.total} processos)
          </span>
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina + 1)}
            disabled={filtro.pagina >= totalPaginas || query.isFetching}
          >
            Próxima
          </Button>
        </nav>
      )}

      <CadastrarProcessoModal open={cadastrando} onClose={() => setCadastrando(false)} />
      {selecionado && (
        <ProcessoAcoesModal processo={selecionado} onClose={() => setSelecionado(null)} />
      )}
    </>
  );
}
