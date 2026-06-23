// Tela de DETALHE / FLUXO de um Inventário (Lei 4.320 art. 96). Conduz a máquina de
// estados: EmAbertura -> (snapshot) -> EmContagem -> (conciliação) -> EmConciliacao ->
// (encerramento) -> Encerrado; ou Cancelado. Reúne:
//   - [Query ObterInventario] ficha + comissão + itens do snapshot;
//   - ações WIRED: CarregarSnapshot, RegistrarContagem (por item), RegistrarSobra,
//     ConciliarInventario, EncerrarInventario, CancelarInventario;
//   - divergências (Falta/Sobra/...) com recomendação de efetivação.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
  errorMessage,
  useToast,
} from '../../../components/ui';
import { formatarData } from '../../../i18n/format';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { useCarregarSnapshot, useConciliarInventario, useInventario } from './inventario.api';
import type { InventarioDetalhe, ItemInventarioDto } from './inventario.api';
import {
  situacaoInventarioLabel,
  situacaoInventarioTagVariant,
  tipoInventarioLabel,
} from './inventario.helpers';
import { COLUNAS_DIVERGENCIAS, colunasItens } from './inventario.colunas';
import { InventarioContagemModal } from './InventarioContagemModal';
import { InventarioSobraModal } from './InventarioSobraModal';
import { InventarioEncerrarModal } from './InventarioEncerrarModal';
import { InventarioCancelarModal } from './InventarioCancelarModal';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type ModalAtivo = 'sobra' | 'encerrar' | 'cancelar' | null;

export function InventarioDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useInventario(id);
  const snapshot = useCarregarSnapshot(id);
  const conciliacao = useConciliarInventario(id);

  const [modal, setModal] = useState<ModalAtivo>(null);
  const [itemContagem, setItemContagem] = useState<ItemInventarioDto | null>(null);

  function carregarSnapshot(): void {
    snapshot.mutate(undefined, {
      onSuccess: () => toast.success('Snapshot do acervo congelado.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível carregar o snapshot.',
        ),
    });
  }

  function conciliar(): void {
    conciliacao.mutate(undefined, {
      onSuccess: (divergencias) =>
        toast.success(
          `Conciliação concluída: ${divergencias.length} divergência${divergencias.length === 1 ? '' : 's'}.`,
          'Sucesso',
        ),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível conciliar.',
        ),
    });
  }

  return (
    <>
      <PageHeader
        title="Inventário patrimonial"
        actions={
          <Link className="br-button secondary" to="/patrimonio/inventarios">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<InventarioDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Inventário não encontrado"
            description="Verifique o identificador informado."
          />
        }
      >
        {(inventario) => {
          const emAbertura = inventario.situacao === 'EmAbertura';
          const emContagem = inventario.situacao === 'EmContagem';
          const emConciliacao = inventario.situacao === 'EmConciliacao';
          const terminal = inventario.situacao === 'Encerrado' || inventario.situacao === 'Cancelado';
          const presidente = inventario.comissao.find((m) => m.presidente);
          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center flex-wrap">
                    <strong>
                      Exercício {inventario.exercicio} — {tipoInventarioLabel(inventario.tipo)}
                    </strong>
                    <Tag variant={situacaoInventarioTagVariant(inventario.situacao)}>
                      {situacaoInventarioLabel(inventario.situacao)}
                    </Tag>
                  </div>
                }
                footer={
                  !terminal ? (
                    <Can permission="patrimonio.gerenciar">
                      <Toolbar>
                        {emAbertura && (
                          <Button variant="primary" onClick={carregarSnapshot} loading={snapshot.isPending}>
                            <i className="fas fa-camera" aria-hidden="true" /> Carregar snapshot
                          </Button>
                        )}
                        {emContagem && (
                          <>
                            <Button variant="secondary" onClick={() => setModal('sobra')}>
                              <i className="fas fa-plus" aria-hidden="true" /> Registrar sobra
                            </Button>
                            <Button variant="primary" onClick={conciliar} loading={conciliacao.isPending}>
                              <i className="fas fa-scale-balanced" aria-hidden="true" /> Conciliar
                            </Button>
                          </>
                        )}
                        {emConciliacao && (
                          <Button variant="primary" onClick={() => setModal('encerrar')}>
                            <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar
                          </Button>
                        )}
                        <Button variant="danger" onClick={() => setModal('cancelar')}>
                          <i className="fas fa-ban" aria-hidden="true" /> Cancelar
                        </Button>
                      </Toolbar>
                    </Can>
                  ) : null
                }
              >
                <dl className="row">
                  <Campo rotulo="Setor/UO">{inventario.setor ?? 'Geral'}</Campo>
                  <Campo rotulo="Portaria">{inventario.portaria}</Campo>
                  <Campo rotulo="Data de abertura">{formatarData(inventario.dataAbertura)}</Campo>
                  <Campo rotulo="Data de encerramento">
                    {inventario.dataEncerramento ? formatarData(inventario.dataEncerramento) : '—'}
                  </Campo>
                  <Campo rotulo="Presidente da comissão">{presidente?.nome ?? '—'}</Campo>
                  <Campo rotulo="Membros da comissão">{inventario.comissao.length}</Campo>
                </dl>
              </Card>

              <Card className="mb-4" header={<strong>Snapshot do acervo e contagem física</strong>}>
                <DataTable
                  caption="Itens congelados no snapshot do inventário"
                  columns={colunasItens(emContagem, setItemContagem)}
                  rows={inventario.itens}
                  rowKey={(i) => i.id}
                  empty={
                    <EmptyState
                      icon="fas fa-camera"
                      title="Sem snapshot carregado"
                      description="Carregue o snapshot do acervo para iniciar a contagem física."
                    />
                  }
                />
              </Card>

              <Card header={<strong>Divergências apuradas</strong>}>
                <DataTable
                  caption="Divergências físico × contábil apuradas na conciliação"
                  columns={COLUNAS_DIVERGENCIAS}
                  rows={inventario.divergencias}
                  rowKey={(d) => d.id}
                  error={conciliacao.isError ? errorMessage(conciliacao.error) : null}
                  empty={
                    <EmptyState
                      icon="fas fa-circle-check"
                      title="Sem divergências"
                      description="Concilie o inventário para apurar faltas, sobras e demais divergências."
                    />
                  }
                />
              </Card>

              <InventarioContagemModal
                open={itemContagem !== null}
                onClose={() => setItemContagem(null)}
                inventarioId={inventario.id}
                item={itemContagem}
              />
              <InventarioSobraModal
                open={modal === 'sobra'}
                onClose={() => setModal(null)}
                inventarioId={inventario.id}
              />
              <InventarioEncerrarModal
                open={modal === 'encerrar'}
                onClose={() => setModal(null)}
                inventarioId={inventario.id}
                totalDivergencias={inventario.divergencias.length}
              />
              <InventarioCancelarModal
                open={modal === 'cancelar'}
                onClose={() => setModal(null)}
                inventarioId={inventario.id}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
