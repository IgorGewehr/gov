// Aba ESTABELECIMENTOS da VISA: busca/lista de estabelecimentos fiscalizáveis (ramo/risco),
// cadastro, reclassificação e interdição/levantamento. Endpoints reais sob /saude/vigilancia,
// gated em "saude.vigilancia.ver"; ações em "saude.vigilancia.gerenciar".
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Select,
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can, useHasPermission } from '../../auth/Can';
import {
  useBuscarEstabelecimentosVisa,
  useInterditarEstabelecimento,
  useLevantarInterdicao,
} from './vigilancia.api';
import type { EstabelecimentoVisaDto, EstabelecimentoVisaFiltro } from './vigilancia.api';
import {
  opcoesRamo,
  opcoesRisco,
  opcoesSituacaoEstab,
  ramoLabel,
  riscoLabel,
  riscoVariant,
  situacaoEstabLabel,
  situacaoEstabVariant,
} from './vigilancia.helpers';
import { EstabelecimentoVisaFormModal } from './EstabelecimentoVisaFormModal';
import { VisaReclassificarModal } from './VisaReclassificarModal';
import { VisaMotivoModal } from './VisaMotivoModal';

const TAMANHO_PAGINA = 20;

export function VisaEstabelecimentosTab() {
  const podeGerenciar = useHasPermission('saude.vigilancia.gerenciar');
  const [termoCampo, setTermoCampo] = useState('');
  const [ramoCampo, setRamoCampo] = useState('');
  const [riscoCampo, setRiscoCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [ramo, setRamo] = useState('');
  const [risco, setRisco] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const [formAberto, setFormAberto] = useState(false);
  const [reclassificar, setReclassificar] = useState<EstabelecimentoVisaDto | null>(null);
  const [interditar, setInterditar] = useState<EstabelecimentoVisaDto | null>(null);

  const filtro = useMemo<EstabelecimentoVisaFiltro>(
    () => ({
      termo: termo || undefined,
      ramo: ramo || undefined,
      risco: risco || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [termo, ramo, risco, situacao, pagina],
  );
  const query = useBuscarEstabelecimentosVisa(filtro);

  const interdicaoMut = useInterditarEstabelecimento(interditar?.id ?? '');
  const levantar = useLevantarInterdicao();

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo.trim());
    setRamo(ramoCampo);
    setRisco(riscoCampo);
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<EstabelecimentoVisaDto>[] = [
    {
      key: 'razaoSocial',
      header: 'Razão social',
      sortAccessor: (e) => e.razaoSocial.toLowerCase(),
      render: (e) => (
        <>
          <strong>{e.razaoSocial}</strong>
          <span className="d-block text-down-01 text-secondary">
            {e.ehPessoaJuridica ? 'CNPJ' : 'CPF'} {e.documento} · {e.municipio || '—'}
          </span>
        </>
      ),
    },
    { key: 'ramo', header: 'Ramo', render: (e) => ramoLabel[e.ramo] },
    {
      key: 'risco',
      header: 'Risco',
      render: (e) => <Tag variant={riscoVariant(e.risco)}>{riscoLabel[e.risco]}</Tag>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (e) => <Tag variant={situacaoEstabVariant(e.situacao)}>{situacaoEstabLabel[e.situacao]}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (e) =>
        podeGerenciar ? (
          <div className="d-flex" style={{ gap: '0.5rem' }}>
            <Button variant="secondary" className="small" onClick={() => setReclassificar(e)}>
              Reclassificar
            </Button>
            {e.situacao === 'Interditado' ? (
              <Button
                variant="secondary"
                className="small"
                loading={levantar.isPending}
                onClick={() => levantar.mutate(e.id, { onError: () => undefined })}
              >
                Levantar interdição
              </Button>
            ) : (
              <Button variant="secondary" className="small" onClick={() => setInterditar(e)}>
                Interditar
              </Button>
            )}
          </div>
        ) : (
          <span className="text-secondary">—</span>
        ),
    },
  ];

  return (
    <>
      <Toolbar className="mb-3">
        <Can permission="saude.vigilancia.gerenciar">
          <Button variant="primary" onClick={() => setFormAberto(true)}>
            <i className="fas fa-plus" aria-hidden="true" /> Cadastrar estabelecimento
          </Button>
        </Can>
      </Toolbar>

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarBusca}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-4">
                <FormField label="Buscar" help="Razão social ou documento.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoCampo}
                      onChange={(e) => setTermoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-3">
                <FormField label="Ramo">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesRamo}
                      placeholder="Todos"
                      value={ramoCampo}
                      onChange={(e) => setRamoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-6 col-md-2">
                <FormField label="Risco">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesRisco}
                      placeholder="Todos"
                      value={riscoCampo}
                      onChange={(e) => setRiscoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-6 col-md-3">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoEstab}
                      placeholder="Todas"
                      value={situacaoCampo}
                      onChange={(e) => setSituacaoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Estabelecimentos sujeitos à Vigilância Sanitária"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(e) => e.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-store"
            title="Nenhum estabelecimento encontrado"
            description="Ajuste os filtros ou cadastre um novo estabelecimento fiscalizável."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de estabelecimentos"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} estabelecimento(s)
          </span>
          <div className="d-flex">
            <Button
              variant="secondary"
              className="small"
              disabled={pagina <= 1 || query.isFetching}
              onClick={() => setPagina((p) => Math.max(1, p - 1))}
            >
              Anterior
            </Button>
            <Button
              variant="secondary"
              className="small ml-2"
              disabled={pagina >= totalPaginas || query.isFetching}
              onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}
            >
              Próxima
            </Button>
          </div>
        </nav>
      )}

      <EstabelecimentoVisaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
      <VisaReclassificarModal
        open={reclassificar !== null}
        onClose={() => setReclassificar(null)}
        estabelecimento={reclassificar}
      />
      <VisaMotivoModal
        open={interditar !== null}
        onClose={() => setInterditar(null)}
        title={`Interditar ${interditar?.razaoSocial ?? ''}`}
        label="Motivo da interdição"
        acaoLabel="Interditar"
        sucessoMensagem="Estabelecimento interditado."
        pendente={interdicaoMut.isPending}
        executar={(motivo) => interdicaoMut.mutateAsync(motivo)}
      />
    </>
  );
}
