// Plano de Contas (PCASP) — árvore/lista hierárquica de contas contábeis do tenant
// (GET /financas/contabilidade/plano-de-contas). Ação "Semear plano de contas" (gated por
// financas.gerenciar) popula o plano padrão. Cada conta analítica leva ao seu Razão.
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  errorMessage,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { FinancasSubNav } from '../FinancasSubNav';
import { mensagemErro } from '../financas.helpers';
import { usePlanoDeContas, useSemearPlanoDeContas } from './contabilidade.api';
import type { ContaContabil } from './contabilidade.api';
import { contaEhAnalitica, recuoPorNivel, tipoContaTagVariant } from './contabilidade.helpers';

export function PlanoDeContasPage() {
  const toast = useToast();
  const query = usePlanoDeContas();
  const semear = useSemearPlanoDeContas();

  function semearPlano(): void {
    semear.mutate(undefined, {
      onSuccess: (data) =>
        toast.success(`Plano de contas semeado. ${data.criadas} conta(s) criada(s).`, 'Sucesso'),
      onError: (error) =>
        toast.error(mensagemErro(error, 'Não foi possível semear o plano de contas.')),
    });
  }

  const columns: Column<ContaContabil>[] = [
    {
      key: 'codigo',
      header: 'Código',
      sortAccessor: (c) => c.codigo,
      render: (c) => (
        <span style={{ paddingInlineStart: recuoPorNivel(c.nivel) }} className="text-mono">
          {contaEhAnalitica(c) ? (
            <Link to={`/financas/contabilidade/contas/${c.id}/razao`} aria-label={`Razão da conta ${c.codigo}`}>
              {c.codigo}
            </Link>
          ) : (
            c.codigo
          )}
        </span>
      ),
    },
    {
      key: 'titulo',
      header: 'Título',
      sortAccessor: (c) => c.titulo,
      render: (c) => (contaEhAnalitica(c) ? c.titulo : <strong>{c.titulo}</strong>),
    },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (c) => c.tipo,
      render: (c) => <Tag variant={tipoContaTagVariant(c.tipo)}>{c.tipo}</Tag>,
    },
    {
      key: 'naturezaSaldo',
      header: 'Natureza do saldo',
      sortAccessor: (c) => c.naturezaSaldo,
      render: (c) => c.naturezaSaldo,
    },
    {
      key: 'ativa',
      header: 'Situação',
      sortAccessor: (c) => (c.ativa ? 'Ativa' : 'Inativa'),
      render: (c) => <Tag variant={c.ativa ? 'success' : 'default'}>{c.ativa ? 'Ativa' : 'Inativa'}</Tag>,
    },
  ];

  return (
    <>
      <PageHeader
        title="Plano de Contas"
        description="Plano de Contas Aplicado ao Setor Público (PCASP). Contas analíticas levam ao razão."
        actions={
          <Can permission="financas.gerenciar">
            <Button variant="primary" onClick={semearPlano} loading={semear.isPending}>
              <i className="fas fa-seedling" aria-hidden="true" /> Semear plano de contas
            </Button>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card>
        <DataTable
          caption="Plano de contas do exercício"
          columns={columns}
          rows={query.data}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-list-ol"
              title="Plano de contas vazio"
              description="Nenhuma conta cadastrada. Use “Semear plano de contas” para criar o plano padrão."
            />
          }
        />
      </Card>
    </>
  );
}
