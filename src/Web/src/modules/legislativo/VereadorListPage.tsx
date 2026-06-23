// Tela de LISTA de Vereadores (cadastro da Camara). DataTable com estados +
// abertura do formulario (cadastro/edicao). Inclui o botao "Semear demonstracao"
// (gated `legislativo.demo.semear`), que popula a demo do PoC.
import { useState } from 'react';
import { Button, DataTable, EmptyState, PageHeader, Tag, Toolbar, useToast } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { Can } from '../../auth/Can';
import { useSemearDemonstracao } from './demonstracao.api';
import { useVereador, useVereadores } from './vereadores.api';
import type { VereadorResumo } from './vereadores.api';
import { situacaoVereadorTagVariant } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { VereadorFormModal } from './VereadorFormModal';

export function VereadorListPage() {
  const toast = useToast();
  const query = useVereadores();
  const semear = useSemearDemonstracao();

  const [formAberto, setFormAberto] = useState(false);
  const [edicaoId, setEdicaoId] = useState<string | null>(null);
  // Carrega o detalhe (inclui nome civil) apenas quando ha um vereador em edicao.
  const detalhe = useVereador(edicaoId ?? '');

  function abrirCadastro(): void {
    setEdicaoId(null);
    setFormAberto(true);
  }

  function abrirEdicao(id: string): void {
    setEdicaoId(id);
    setFormAberto(true);
  }

  function fechar(): void {
    setFormAberto(false);
    setEdicaoId(null);
  }

  function semearDemo(): void {
    semear.mutate(undefined, {
      onSuccess: () => toast.success('Demonstração semeada.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível semear a demonstração.',
        ),
    });
  }

  const columns: Column<VereadorResumo>[] = [
    {
      key: 'nomeParlamentar',
      header: 'Nome parlamentar',
      sortAccessor: (v) => v.nomeParlamentar,
      render: (v) => v.nomeParlamentar,
    },
    { key: 'partido', header: 'Partido', sortAccessor: (v) => v.partido, render: (v) => v.partido },
    {
      key: 'legislatura',
      header: 'Legislatura',
      sortAccessor: (v) => v.legislaturaInicio,
      render: (v) => `${v.legislaturaInicio}–${v.legislaturaFim}`,
    },
    { key: 'cargoMesa', header: 'Cargo na Mesa', render: (v) => v.cargoMesa },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (v) => v.situacao,
      render: (v) => <Tag variant={situacaoVereadorTagVariant(v.situacao)}>{v.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (v) => (
        <Can permission="legislativo.gerenciar">
          <Button variant="ghost" size="sm" onClick={() => abrirEdicao(v.id)}>
            Editar
          </Button>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Vereadores"
        description="Cadastro dos vereadores da Câmara Municipal."
        actions={
          <Toolbar>
            <Can permission="legislativo.demo.semear">
              <Button variant="secondary" onClick={semearDemo} loading={semear.isPending}>
                <i className="fas fa-seedling" aria-hidden="true" /> Semear demonstração
              </Button>
            </Can>
            <Can permission="legislativo.gerenciar">
              <Button variant="primary" onClick={abrirCadastro}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar vereador
              </Button>
            </Can>
          </Toolbar>
        }
      />

      <LegislativoSecoesNav />

      <DataTable
        caption="Vereadores cadastrados"
        columns={columns}
        rows={query.data}
        rowKey={(v) => v.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-users"
            title="Nenhum vereador cadastrado"
            description="Cadastre os vereadores da Câmara ou semeie a demonstração para começar."
          />
        }
      />

      <VereadorFormModal
        open={formAberto}
        onClose={fechar}
        vereador={edicaoId !== null ? detalhe.data : undefined}
      />
    </>
  );
}
