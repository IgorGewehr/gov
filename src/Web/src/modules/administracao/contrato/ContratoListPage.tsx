// Tela de LISTA/CONSULTA de Contratos. Cobre as queries:
//   - ListarContratosVigentes (por data de referência);
//   - ListarContratosPorFornecedor (por fornecedor).
// DataTable com filtros + colunas + ordenação + estados loading/vazio/erro,
// link para o detalhe e abertura do FormModal de celebração (command CelebrarContrato).
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
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import {
  SITUACAO_ROTULO,
  useContratosPorFornecedor,
  useContratosVigentes,
  useVarrerPrazosPncp,
} from './contrato.api';
import type { ContratoResumo } from './contrato.api';
import { situacaoTagVariant } from './contrato.helpers';
import { ContratoFormModal } from './ContratoFormModal';
import { AdministracaoSubNav } from '../AdministracaoSubNav';

type Criterio = 'vigentes' | 'fornecedor';

function hoje(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ContratoListPage() {
  const toast = useToast();
  const varrerMutation = useVarrerPrazosPncp();
  const [criterio, setCriterio] = useState<Criterio>('vigentes');
  const [referencia, setReferencia] = useState(hoje());
  const [fornecedorId, setFornecedorId] = useState('');
  const [consulta, setConsulta] = useState<{ criterio: Criterio; valor: string } | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const vigentesQuery = useContratosVigentes(
    consulta?.valor ?? '',
    consulta?.criterio === 'vigentes',
  );
  const fornecedorQuery = useContratosPorFornecedor(
    consulta?.valor ?? '',
    consulta?.criterio === 'fornecedor',
  );

  const query = consulta?.criterio === 'fornecedor' ? fornecedorQuery : vigentesQuery;

  function varrerPrazos(): void {
    varrerMutation.mutate(undefined, {
      onSuccess: (r) =>
        toast.success(
          r.alertas > 0
            ? `Varredura concluída: ${r.alertas} alerta(s) de prazo PNCP (art. 94) enviados ao Portal do Gestor.`
            : 'Varredura concluída: nenhum prazo PNCP a vencer ou vencido no momento.',
          'PNCP',
        ),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível varrer os prazos PNCP.'),
    });
  }

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const valor = criterio === 'vigentes' ? referencia.trim() : fornecedorId.trim();
    if (valor === '') return;
    setConsulta({ criterio, valor });
  }

  const columns: Column<ContratoResumo>[] = [
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => c.situacao,
      render: (c) => <Tag variant={situacaoTagVariant(c.situacao)}>{SITUACAO_ROTULO[c.situacao]}</Tag>,
    },
    {
      key: 'objeto',
      header: 'Objeto',
      sortAccessor: (c) => c.objeto,
      render: (c) => c.objeto,
    },
    {
      key: 'fornecedor',
      header: 'Fornecedor',
      render: (c) => <span className="text-mono text-down-01">{c.fornecedorId}</span>,
    },
    {
      key: 'valorAtual',
      header: 'Valor atual',
      align: 'end',
      sortAccessor: (c) => c.valorAtual,
      render: (c) => formatarMoeda(c.valorAtual),
    },
    {
      key: 'vigenciaFim',
      header: 'Fim da vigência',
      sortAccessor: (c) => c.vigenciaFim,
      render: (c) => formatarData(c.vigenciaFim),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Link className="br-button secondary small" to={`/administracao/contratos/${c.id}`}>
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
        title="Contratos"
        description="Consulte os contratos administrativos vigentes ou por fornecedor (Lei 14.133/2021)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button
                variant="secondary"
                onClick={varrerPrazos}
                loading={varrerMutation.isPending}
                title="Varrer prazos de divulgação no PNCP (art. 94) e alertar a gestão"
              >
                <i className="fas fa-stopwatch" aria-hidden="true" /> Varrer prazos PNCP
              </Button>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Celebrar contrato
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button
                variant="primary"
                type="submit"
                disabled={(criterio === 'vigentes' ? referencia.trim() : fornecedorId.trim()) === ''}
                loading={query.isFetching}
              >
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-6">
                <FormField label="Critério de consulta">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={criterio}
                      onChange={(e) => setCriterio(e.target.value as Criterio)}
                      options={[
                        { value: 'vigentes', label: 'Contratos vigentes (por data)' },
                        { value: 'fornecedor', label: 'Por fornecedor' },
                      ]}
                    />
                  )}
                </FormField>
              </div>

              {criterio === 'vigentes' ? (
                <div className="col-sm-6">
                  <FormField label="Data de referência" required>
                    {({ id, describedBy, invalid }) => (
                      <Input
                        id={id}
                        type="date"
                        aria-describedby={describedBy}
                        invalid={invalid}
                        value={referencia}
                        onChange={(e) => setReferencia(e.target.value)}
                      />
                    )}
                  </FormField>
                </div>
              ) : (
                <div className="col-sm-6">
                  <FormField label="Identificador do fornecedor" required>
                    {({ id, describedBy, invalid }) => (
                      <Input
                        id={id}
                        aria-describedby={describedBy}
                        invalid={invalid}
                        value={fornecedorId}
                        onChange={(e) => setFornecedorId(e.target.value)}
                        placeholder="00000000-0000-0000-0000-000000000000"
                      />
                    )}
                  </FormField>
                </div>
              )}
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Escolha o critério, informe os dados e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={
            consulta.criterio === 'vigentes'
              ? `Contratos vigentes em ${formatarData(consulta.valor)}`
              : `Contratos do fornecedor ${consulta.valor}`
          }
          columns={columns}
          rows={query.data}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum contrato encontrado"
              description="Não há contratos para os critérios informados."
            />
          }
        />
      )}

      <ContratoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
