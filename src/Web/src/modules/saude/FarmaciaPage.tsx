// Tela da FARMÁCIA (HÓRUS): catálogo de medicamentos (REMUME), posição de estoque por UBS
// (com entrada de lote/validade) e alertas de validade/ruptura. Endpoints REAIS sob
// /saude/farmacia/*, gated em "saude.farmacia.ver"; ações em "saude.farmacia.gerenciar"
// e "saude.farmacia.dispensar". Estoque/catálogo são dados operacionais (sem trilha LGPD).
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
  PageHeader,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import {
  useAlertasValidade,
  useBuscarMedicamentos,
  usePosicaoEstoque,
} from './api';
import type { AlertaValidadeDto, MedicamentoItemLista, PosicaoEstoqueDto } from './api';
import { SaudeSubNav } from './SaudeSubNav';
import { EstabelecimentoPicker } from './EstabelecimentoPicker';
import { CadastrarMedicamentoModal, EntradaEstoqueModal } from './FarmaciaModals';
import { DispensacaoModal } from './DispensacaoModal';
import { DispensacaoHistorico } from './DispensacaoHistorico';

const TAMANHO_PAGINA = 20;
const HORIZONTE_VALIDADE = 90;

export function FarmaciaPage() {
  const [termoCampo, setTermoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [pagina, setPagina] = useState(1);
  const [estabId, setEstabId] = useState('');

  const [cadastrarAberto, setCadastrarAberto] = useState(false);
  const [dispensarAberto, setDispensarAberto] = useState(false);
  const [entradaMed, setEntradaMed] = useState<MedicamentoItemLista | null>(null);

  const filtro = useMemo(
    () => ({ termo: termo || undefined, apenasAtivos: true, pagina, tamanho: TAMANHO_PAGINA }),
    [termo, pagina],
  );
  const catalogo = useBuscarMedicamentos(filtro);
  const estoque = usePosicaoEstoque(estabId, estabId !== '');
  const alertas = useAlertasValidade(HORIZONTE_VALIDADE);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo);
  }

  const total = catalogo.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const colunasCatalogo: Column<MedicamentoItemLista>[] = [
    { key: 'principioAtivo', header: 'Princípio ativo', sortAccessor: (m) => m.principioAtivo, render: (m) => m.principioAtivo },
    { key: 'apresentacao', header: 'Apresentação', render: (m) => `${m.apresentacao} · ${m.concentracao}` },
    { key: 'forma', header: 'Forma', render: (m) => m.forma },
    {
      key: 'controle',
      header: 'Controle',
      render: (m) =>
        m.exigeReceitaControlada ? <Tag variant="warning">{m.controle}</Tag> : <Tag variant="default">Comum</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (m) => (
        <Can permission="saude.farmacia.gerenciar">
          <Button variant="secondary" className="small" onClick={() => setEntradaMed(m)}>
            <i className="fas fa-boxes-stacked" aria-hidden="true" /> Entrada de lote
          </Button>
        </Can>
      ),
    },
  ];

  const colunasEstoque: Column<PosicaoEstoqueDto>[] = [
    { key: 'principioAtivo', header: 'Medicamento', render: (p) => p.principioAtivo },
    { key: 'saldo', header: 'Saldo total', render: (p) => p.saldo },
    {
      key: 'saldoValido',
      header: 'Saldo válido',
      render: (p) =>
        p.emRuptura ? (
          <Tag variant="danger">{p.saldoValido} (ruptura)</Tag>
        ) : (
          <span>{p.saldoValido}</span>
        ),
    },
    { key: 'ponto', header: 'Ponto ressup.', render: (p) => p.pontoDeRessuprimento },
    {
      key: 'lotes',
      header: 'Lotes',
      render: (p) =>
        p.lotes.length === 0 ? (
          <span className="text-secondary">—</span>
        ) : (
          <ul className="mb-0 pl-3">
            {p.lotes.map((l) => (
              <li key={l.numeroLote}>
                {l.numeroLote} · venc. {formatarData(l.validade)} · {l.saldo}{' '}
                {l.vencido && <Tag variant="danger">vencido</Tag>}
              </li>
            ))}
          </ul>
        ),
    },
  ];

  const colunasAlertas: Column<AlertaValidadeDto>[] = [
    { key: 'principioAtivo', header: 'Medicamento', render: (a) => a.principioAtivo },
    { key: 'numeroLote', header: 'Lote', render: (a) => a.numeroLote },
    {
      key: 'validade',
      header: 'Validade',
      sortAccessor: (a) => a.validade,
      render: (a) =>
        a.vencido ? (
          <Tag variant="danger">{formatarData(a.validade)} (vencido)</Tag>
        ) : (
          <Tag variant="warning">{formatarData(a.validade)}</Tag>
        ),
    },
    { key: 'saldo', header: 'Saldo', render: (a) => a.saldo },
  ];

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Farmácia"
        description="Catálogo (REMUME), estoque por unidade e alertas de validade/ruptura."
        actions={
          <Toolbar>
            <Can permission="saude.farmacia.dispensar">
              <Button variant="secondary" onClick={() => setDispensarAberto(true)}>
                <i className="fas fa-hand-holding-medical" aria-hidden="true" /> Dispensar
              </Button>
            </Can>
            <Can permission="saude.farmacia.gerenciar">
              <Button variant="primary" onClick={() => setCadastrarAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Novo medicamento
              </Button>
            </Can>
          </Toolbar>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarBusca}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={catalogo.isFetching}>
                Buscar
              </Button>
            }
          >
            <FormField label="Buscar no catálogo (princípio ativo / apresentação)">
              {({ id }) => (
                <Input id={id} value={termoCampo} onChange={(e) => setTermoCampo(e.target.value)} placeholder="Ex.: Dipirona" />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Catálogo de medicamentos"
        columns={colunasCatalogo}
        rows={catalogo.data?.itens}
        rowKey={(m) => m.id}
        loading={catalogo.isLoading}
        error={catalogo.isError ? errorMessage(catalogo.error) : null}
        empty={
          <EmptyState icon="fas fa-pills" title="Nenhum medicamento" description="Cadastre o primeiro item do REMUME." />
        }
      />

      {totalPaginas > 1 && (
        <nav className="d-flex align-items-center justify-content-between mt-3" aria-label="Paginação do catálogo">
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} item(ns)
          </span>
          <div className="d-flex">
            <Button variant="secondary" className="small" disabled={pagina <= 1 || catalogo.isFetching} onClick={() => setPagina((p) => Math.max(1, p - 1))}>
              <i className="fas fa-chevron-left" aria-hidden="true" /> Anterior
            </Button>
            <Button variant="secondary" className="small ml-2" disabled={pagina >= totalPaginas || catalogo.isFetching} onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}>
              Próxima <i className="fas fa-chevron-right" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      )}

      <Card className="mt-5 mb-4">
        <h2 className="text-up-01 mb-3">Estoque por unidade</h2>
        <EstabelecimentoPicker label="Unidade (CNES)" value={estabId} onChange={setEstabId} />
        {estabId !== '' && (
          <div className="mt-3">
            <DataTable
              caption="Posição de estoque"
              columns={colunasEstoque}
              rows={estoque.data}
              rowKey={(p) => p.estoqueId}
              loading={estoque.isLoading}
              error={estoque.isError ? errorMessage(estoque.error) : null}
              empty={<EmptyState icon="fas fa-warehouse" title="Sem estoque" description="Registre uma entrada de lote." />}
            />
          </div>
        )}
      </Card>

      <Card className="mb-4" accent="warning">
        <h2 className="text-up-01 mb-3">
          Alertas de validade <span className="text-secondary text-down-01">(próximos {HORIZONTE_VALIDADE} dias)</span>
        </h2>
        <DataTable
          caption="Lotes a vencer ou vencidos"
          columns={colunasAlertas}
          rows={alertas.data}
          rowKey={(a) => `${a.estabelecimentoId}-${a.medicamentoId}-${a.numeroLote}`}
          loading={alertas.isLoading}
          error={alertas.isError ? errorMessage(alertas.error) : null}
          empty={<EmptyState icon="fas fa-circle-check" title="Sem alertas" description="Nenhum lote a vencer no horizonte." />}
        />
      </Card>

      <Can permission="saude.prontuario.ler">
        <DispensacaoHistorico />
      </Can>

      <CadastrarMedicamentoModal open={cadastrarAberto} onClose={() => setCadastrarAberto(false)} />
      <DispensacaoModal open={dispensarAberto} onClose={() => setDispensarAberto(false)} />
      <EntradaEstoqueModal open={entradaMed !== null} medicamento={entradaMed} onClose={() => setEntradaMed(null)} />
    </>
  );
}
