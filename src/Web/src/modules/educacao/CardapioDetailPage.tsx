// Detalhe do cardápio semanal (PNAE): cabeçalho + itens planejados (por dia/refeição)
// + ações de ciclo de vida. Espelha GET /educacao/merenda/cardapios/{cardapioId}
// (CardapioDto). Adicionar item / publicar / registrar distribuição gated em
// "educacao.gerenciar"; leitura em "educacao.ver".
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { Can, useHasPermission } from '../../auth/Can';
import { useCardapio, usePublicarCardapio } from './merenda.api';
import type { CardapioDto, ItemCardapioDto } from './merenda.api';
import { situacaoCardapioTagVariant } from './educacao.helpers';
import { AdicionarItemModal, DistribuicaoModal } from './MerendaAcaoModais';
import { EducacaoSubNav } from './EducacaoSubNav';

export function CardapioDetailPage() {
  const { cardapioId = '' } = useParams<{ cardapioId: string }>();
  const toast = useToast();
  const podeGerenciar = useHasPermission('educacao.gerenciar');

  const query = useCardapio(cardapioId);
  const publicar = usePublicarCardapio(cardapioId);

  const [itemAberto, setItemAberto] = useState(false);
  const [distribuicaoAberta, setDistribuicaoAberta] = useState(false);

  function onPublicar(): void {
    publicar.mutate(undefined, {
      onSuccess: () => toast.success('Cardápio publicado.', 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar.'),
    });
  }

  const columns: Column<ItemCardapioDto>[] = [
    { key: 'dia', header: 'Dia', sortAccessor: (i) => i.dia, render: (i) => i.dia },
    { key: 'refeicao', header: 'Refeição', render: (i) => i.refeicao },
    { key: 'genero', header: 'Gênero (Id)', render: (i) => <code className="text-down-01">{i.generoEstoqueId}</code> },
    {
      key: 'percapita',
      header: 'Per capita',
      sortAccessor: (i) => i.quantidadePerCapita,
      render: (i) => `${i.quantidadePerCapita.toLocaleString('pt-BR')} ${i.unidadeMedida}`,
    },
  ];

  return (
    <>
      <EducacaoSubNav />
      <QueryState<CardapioDto>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-utensils"
            title="Cardápio não encontrado"
            description="O cardápio pode ter sido removido ou o identificador é inválido."
          />
        }
      >
        {(cardapio) => {
          const planejado = cardapio.situacao === 'Planejado';
          const publicado = cardapio.situacao === 'Publicado';
          return (
            <>
              <PageHeader
                eyebrow="Merenda escolar (PNAE)"
                title={`Cardápio — semana de ${new Date(`${cardapio.semana}T00:00:00`).toLocaleDateString('pt-BR')}`}
                description={`Faixa etária: ${cardapio.faixaEtaria}.`}
                actions={
                  <Can permission="educacao.gerenciar">
                    <Toolbar>
                      {planejado && (
                        <Button variant="secondary" onClick={() => setItemAberto(true)}>
                          <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
                        </Button>
                      )}
                      {planejado && (
                        <Button
                          variant="primary"
                          loading={publicar.isPending}
                          disabled={cardapio.itens.length === 0}
                          onClick={onPublicar}
                        >
                          <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar
                        </Button>
                      )}
                      {publicado && (
                        <Button variant="primary" onClick={() => setDistribuicaoAberta(true)}>
                          <i className="fas fa-clipboard-check" aria-hidden="true" /> Registrar distribuição
                        </Button>
                      )}
                    </Toolbar>
                  </Can>
                }
              />

              <Card className="mb-4">
                <div className="d-flex align-items-center">
                  <span className="text-secondary mr-2">Situação:</span>
                  <Tag variant={situacaoCardapioTagVariant(cardapio.situacao)}>{cardapio.situacao}</Tag>
                </div>
              </Card>

              <DataTable
                caption="Itens planejados do cardápio"
                columns={columns}
                rows={cardapio.itens}
                rowKey={(i) => i.id}
                empty={
                  <EmptyState
                    icon="fas fa-carrot"
                    title="Nenhum item planejado"
                    description={
                      podeGerenciar && planejado
                        ? 'Adicione gêneros por dia/refeição antes de publicar.'
                        : 'Este cardápio ainda não possui itens.'
                    }
                  />
                }
              />

              <AdicionarItemModal
                open={itemAberto}
                cardapioId={cardapio.id}
                onClose={() => setItemAberto(false)}
              />
              <DistribuicaoModal
                open={distribuicaoAberta}
                cardapioId={cardapio.id}
                onClose={() => setDistribuicaoAberta(false)}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
