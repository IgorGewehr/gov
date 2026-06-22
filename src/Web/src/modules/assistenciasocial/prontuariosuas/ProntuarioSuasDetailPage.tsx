// Tela de DETALHE/AÇÕES de um Prontuário SUAS (param de rota :id = prontuarioId).
// Concentra a máquina de estados como botões de AÇÃO (cada um abre um Modal+form WIRED):
//   - RegistrarAtendimento   (Aberto -> Aberto)
//   - EncerrarAcompanhamento (Aberto -> Encerrado, destrutiva c/ confirmação)
//   - RegistrarAcessoProntuario (append-only na trilha)
// Exibe ainda a CONSULTA da trilha de acesso imutável (ObterTrilhaAcessoProntuarioQuery)
// via DataTable com estados loading/vazio/erro + ordenação.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  PageHeader,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { useTrilhaAcesso } from './prontuariosuas.api';
import type { AcessoResumo } from './prontuariosuas.api';
import {
  EncerrarAcompanhamentoModal,
  RegistrarAcessoModal,
  RegistrarAtendimentoModal,
} from './ProntuarioSuasAcaoModais';
import { Can } from '../../../auth/Can';

/** Formata um instante UTC (ISO 8601 com hora) em data/hora local pt-BR. */
function formatarDataHora(iso: string): string {
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) return '—';
  return parsed.toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function ProntuarioSuasDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const trilha = useTrilhaAcesso(id);

  const [atendimentoAberto, setAtendimentoAberto] = useState(false);
  const [encerramentoAberto, setEncerramentoAberto] = useState(false);
  const [acessoAberto, setAcessoAberto] = useState(false);

  const columns: Column<AcessoResumo>[] = [
    {
      key: 'dataHora',
      header: 'Data/hora do acesso',
      sortAccessor: (a) => a.dataHoraAcessoUtc,
      render: (a) => formatarDataHora(a.dataHoraAcessoUtc),
    },
    {
      key: 'usuario',
      header: 'Usuário',
      sortAccessor: (a) => a.usuarioId,
      render: (a) => a.usuarioId,
    },
    {
      key: 'motivo',
      header: 'Motivo do acesso',
      render: (a) => a.motivoAcesso,
    },
  ];

  return (
    <>
      <PageHeader
        title="Prontuário SUAS"
        description={`Identificador: ${id}`}
        actions={
          <Link className="br-button secondary" to="/assistenciasocial">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <Card className="mb-4" header={<strong>Ações do acompanhamento</strong>}>
        <p className="text-gray-60">
          Ações permitidas pela máquina de estados. Atendimento e encerramento exigem situação
          <strong> Aberto</strong>; o registro de acesso compõe a trilha imutável (auditoria).
        </p>
        <Can
          permission="assistenciasocial.gerenciar"
          fallback={
            <p className="text-gray-60 mb-0">
              <i className="fas fa-lock" aria-hidden="true" /> Você não possui permissão para
              executar ações neste prontuário.
            </p>
          }
        >
          <div className="d-flex flex-wrap" style={{ gap: 'var(--spacing-scale-1x)' }}>
            <Button variant="primary" onClick={() => setAtendimentoAberto(true)}>
              <i className="fas fa-notes-medical" aria-hidden="true" /> Registrar atendimento
            </Button>
            <Button variant="secondary" onClick={() => setAcessoAberto(true)}>
              <i className="fas fa-user-shield" aria-hidden="true" /> Registrar acesso
            </Button>
            <Button variant="danger" onClick={() => setEncerramentoAberto(true)}>
              <i className="fas fa-folder-minus" aria-hidden="true" /> Encerrar acompanhamento
            </Button>
          </div>
        </Can>
      </Card>

      <Card header={<strong>Trilha de acesso (imutável)</strong>}>
        <p className="text-gray-60">
          Registro append-only de cada leitura do prontuário (quem/quando/por quê), exigível pelo
          controle social/Tribunal de Contas.
        </p>
        <DataTable
          caption={`Trilha de acesso do prontuário ${id}`}
          columns={columns}
          rows={trilha.data}
          rowKey={(a) => a.acessoId}
          loading={trilha.isLoading}
          error={trilha.isError ? errorMessage(trilha.error) : null}
          empty={
            <EmptyState
              icon="fas fa-clipboard-list"
              title="Nenhum acesso registrado"
              description="Ainda não há acessos auditados para este prontuário."
            />
          }
        />
      </Card>

      <RegistrarAtendimentoModal
        open={atendimentoAberto}
        onClose={() => setAtendimentoAberto(false)}
        prontuarioId={id}
      />
      <EncerrarAcompanhamentoModal
        open={encerramentoAberto}
        onClose={() => setEncerramentoAberto(false)}
        prontuarioId={id}
      />
      <RegistrarAcessoModal
        open={acessoAberto}
        onClose={() => setAcessoAberto(false)}
        prontuarioId={id}
      />
    </>
  );
}
