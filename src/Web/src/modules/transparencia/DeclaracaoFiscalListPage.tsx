// Tela de LISTA (consulta) de Declarações Fiscais (SICONFI) por exercício.
// Filtros (exercício/tipo/situação) -> DataTable com estados loading/vazio/erro
// + ordenação, link para detalhe e abertura do formulário de consolidação (mutation).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  Tag,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useDeclaracoesFiscais } from './api';
import type {
  DeclaracaoFiscalResumo,
  ListarDeclaracoesParams,
  SituacaoDeclaracaoFiscal,
  TipoDeclaracaoFiscal,
} from './api';
import {
  exercicioCorrente,
  situacaoDeclaracaoOptions,
  situacaoDeclaracaoTagVariant,
  tipoDeclaracaoOptions,
} from './transparencia.helpers';
import { DeclaracaoFiscalFormModal } from './DeclaracaoFiscalFormModal';

export function DeclaracaoFiscalListPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [tipo, setTipo] = useState<TipoDeclaracaoFiscal | ''>('');
  const [situacao, setSituacao] = useState<SituacaoDeclaracaoFiscal | ''>('');
  const [filtros, setFiltros] = useState<ListarDeclaracoesParams | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const query = useDeclaracoesFiscais(filtros ?? { exercicio: 0 }, filtros !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano <= 0) return;
    setFiltros({ exercicio: ano, tipo: tipo || null, situacao: situacao || null });
  }

  const columns: Column<DeclaracaoFiscalResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (d) => d.tipoDeclaracao,
      render: (d) => d.tipoDeclaracao,
    },
    {
      key: 'periodo',
      header: 'Período',
      sortAccessor: (d) => d.periodo,
      render: (d) => d.periodo,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (d) => d.situacao,
      render: (d) => <Tag variant={situacaoDeclaracaoTagVariant(d.situacao)}>{d.situacao}</Tag>,
    },
    {
      key: 'dataLimite',
      header: 'Prazo',
      sortAccessor: (d) => d.dataLimite,
      render: (d) => formatarData(d.dataLimite),
    },
    {
      key: 'dataTransmissao',
      header: 'Transmissão',
      sortAccessor: (d) => d.dataTransmissao ?? '',
      render: (d) => formatarData(d.dataTransmissao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (d) => (
        <Link className="br-button tertiary small" to={`/transparencia/declaracoes-fiscais/${d.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Declarações Fiscais (SICONFI)"
        description="Consulte e consolide MSC, RREO, RGF e DCA transmitidas ao SICONFI/STN."
        actions={
          <Can permission="transparencia.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Consolidar declaração
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col-12 col-sm-3">
              <FormField label="Exercício" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="number"
                    min="1900"
                    inputMode="numeric"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={exercicio}
                    onChange={(e) => setExercicio(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-sm-4">
              <FormField label="Tipo de declaração">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    placeholder="Todos"
                    options={tipoDeclaracaoOptions}
                    value={tipo}
                    onChange={(e) => setTipo(e.target.value as TipoDeclaracaoFiscal | '')}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-sm-3">
              <FormField label="Situação">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    placeholder="Todas"
                    options={situacaoDeclaracaoOptions}
                    value={situacao}
                    onChange={(e) => setSituacao(e.target.value as SituacaoDeclaracaoFiscal | '')}
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {filtros === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício e clique em Consultar para listar as declarações."
        />
      ) : (
        <DataTable
          caption={`Declarações fiscais do exercício ${filtros.exercicio}`}
          columns={columns}
          rows={query.data}
          rowKey={(d) => d.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma declaração encontrada"
              description="Não há declarações para o exercício e filtros informados."
            />
          }
        />
      )}

      <DeclaracaoFiscalFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        exercicioInicial={Number(exercicio) || exercicioCorrente()}
      />
    </>
  );
}
