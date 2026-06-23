// Detalhe da rota de transporte (PNATE): cabeçalho + alunos transportados + ações
// de ciclo de vida. Espelha GET /educacao/transporte/rotas/{rotaId}/alunos
// (RotaTransporteDto). Vincular/desligar aluno + ativar/encerrar gated em
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
import { useRota, useDesligarAluno, useAtivarRota, useEncerrarRota } from './transporte.api';
import type { AlunoTransportadoDto, RotaTransporteDto } from './transporte.api';
import { situacaoRotaTagVariant } from './educacao.helpers';
import { VincularAlunoModal } from './VincularAlunoModal';
import { EducacaoSubNav } from './EducacaoSubNav';

export function RotaDetailPage() {
  const { rotaId = '' } = useParams<{ rotaId: string }>();
  const toast = useToast();
  const podeGerenciar = useHasPermission('educacao.gerenciar');

  const query = useRota(rotaId);
  const desligar = useDesligarAluno(rotaId);
  const ativar = useAtivarRota(rotaId);
  const encerrar = useEncerrarRota(rotaId);

  const [vincularAberto, setVincularAberto] = useState(false);

  const ocupada = desligar.isPending || ativar.isPending || encerrar.isPending;

  function onDesligar(alunoTransportadoId: string): void {
    desligar.mutate(alunoTransportadoId, {
      onSuccess: () => toast.success('Aluno desligado da rota.', 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível desligar o aluno.'),
    });
  }

  function onAtivar(): void {
    ativar.mutate(undefined, {
      onSuccess: () => toast.success('Rota ativada.', 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível ativar a rota.'),
    });
  }

  function onEncerrar(): void {
    encerrar.mutate(undefined, {
      onSuccess: () => toast.success('Rota encerrada.', 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar a rota.'),
    });
  }

  return (
    <>
      <EducacaoSubNav />
      <QueryState<RotaTransporteDto>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-bus"
            title="Rota não encontrada"
            description="A rota pode ter sido removida ou o identificador é inválido."
          />
        }
      >
        {(rota) => {
          const planejada = rota.situacao === 'Planejada';
          const encerrada = rota.situacao === 'Encerrada';

          const columns: Column<AlunoTransportadoDto>[] = [
            {
              key: 'aluno',
              header: 'Aluno (Id)',
              render: (a) => <code className="text-down-01">{a.alunoId}</code>,
            },
            { key: 'ponto', header: 'Ponto de embarque', render: (a) => a.pontoEmbarque },
            {
              key: 'ativo',
              header: 'Vínculo',
              sortAccessor: (a) => (a.ativo ? 1 : 0),
              render: (a) => (
                <Tag variant={a.ativo ? 'success' : 'default'}>{a.ativo ? 'Ativo' : 'Desligado'}</Tag>
              ),
            },
            ...(podeGerenciar && !encerrada
              ? [
                  {
                    key: 'acoes',
                    header: 'Ações',
                    sticky: true,
                    render: (a: AlunoTransportadoDto) =>
                      a.ativo ? (
                        <Button
                          variant="secondary"
                          className="small"
                          disabled={ocupada}
                          onClick={() => onDesligar(a.id)}
                        >
                          <i className="fas fa-user-minus" aria-hidden="true" /> Desligar
                        </Button>
                      ) : (
                        <span className="text-secondary text-down-01">—</span>
                      ),
                  } as Column<AlunoTransportadoDto>,
                ]
              : []),
          ];

          return (
            <>
              <PageHeader
                eyebrow="Transporte escolar (PNATE)"
                title={rota.nome}
                description={`Turno ${rota.turno} · ${rota.modalidade} · ${rota.quilometragem.toLocaleString('pt-BR')} km.`}
                actions={
                  <Can permission="educacao.gerenciar">
                    <Toolbar>
                      {!encerrada && (
                        <Button variant="secondary" onClick={() => setVincularAberto(true)}>
                          <i className="fas fa-user-plus" aria-hidden="true" /> Vincular aluno
                        </Button>
                      )}
                      {planejada && (
                        <Button
                          variant="primary"
                          loading={ativar.isPending}
                          disabled={rota.alunos.every((a) => !a.ativo)}
                          onClick={onAtivar}
                        >
                          <i className="fas fa-play" aria-hidden="true" /> Ativar
                        </Button>
                      )}
                      {!encerrada && (
                        <Button variant="secondary" loading={encerrar.isPending} onClick={onEncerrar}>
                          <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar
                        </Button>
                      )}
                    </Toolbar>
                  </Can>
                }
              />

              <Card className="mb-4">
                <div className="d-flex align-items-center">
                  <span className="text-secondary mr-2">Situação:</span>
                  <Tag variant={situacaoRotaTagVariant(rota.situacao)}>{rota.situacao}</Tag>
                  {rota.veiculoId && (
                    <span className="ml-4 text-down-01 text-secondary">
                      Veículo (Frota): <code>{rota.veiculoId}</code>
                    </span>
                  )}
                </div>
              </Card>

              <DataTable
                caption="Alunos transportados"
                columns={columns}
                rows={rota.alunos}
                rowKey={(a) => a.id}
                empty={
                  <EmptyState
                    icon="fas fa-users"
                    title="Nenhum aluno vinculado"
                    description={
                      podeGerenciar && !encerrada
                        ? 'Vincule alunos com o ponto de embarque para poder ativar a rota.'
                        : 'Esta rota ainda não possui alunos transportados.'
                    }
                  />
                }
              />

              <VincularAlunoModal
                open={vincularAberto}
                rotaId={rota.id}
                onClose={() => setVincularAberto(false)}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
