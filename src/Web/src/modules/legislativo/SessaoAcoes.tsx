// Barra de ACOES da Sessao (gated por "legislativo.gerenciar"). Concentra os botoes
// de workflow plenario e o estado de abertura dos modais, mantendo a DetailPage
// enxuta. Cada acao cobre um endpoint POST do agregado Sessao.
import { useState } from 'react';
import { Button, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ConfirmarAcaoModal } from './ConfirmarAcaoModal';
import { PresencaModal, IncluirOrdemDoDiaModal } from './SessaoAcaoModais';
import {
  useVerificarQuorum,
  useAbrirSessao,
  useSuspenderSessao,
  useReabrirSessao,
  useEncerrarSessao,
  useCancelarSessao,
} from './sessao.api';
import { mensagemErro } from './legislativoAcao.shared';

type ModalAtivo =
  | null
  | 'presenca'
  | 'ordemDoDia'
  | 'abrir'
  | 'suspender'
  | 'reabrir'
  | 'encerrar'
  | 'cancelar';

export function SessaoAcoes({ id }: { id: string }) {
  const toast = useToast();
  const [ativo, setAtivo] = useState<ModalAtivo>(null);
  const fechar = () => setAtivo(null);

  const quorum = useVerificarQuorum(id);
  const abrir = useAbrirSessao(id);
  const suspender = useSuspenderSessao(id);
  const reabrir = useReabrirSessao(id);
  const encerrar = useEncerrarSessao(id);
  const cancelar = useCancelarSessao(id);

  function verificarQuorum(): void {
    quorum.mutate(undefined, {
      onSuccess: (atingido) =>
        atingido
          ? toast.success('Quórum atingido.', 'Quórum')
          : toast.warning('Quórum não atingido.', 'Quórum'),
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível verificar o quórum.')),
    });
  }

  return (
    <Can permission="legislativo.gerenciar">
      <div className="d-flex flex-wrap gap-2 mb-4" role="group" aria-label="Ações da sessão">
        <Button variant="secondary" onClick={() => setAtivo('presenca')}>
          Registrar presença
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('ordemDoDia')}>
          Incluir em Ordem do Dia
        </Button>
        <Button variant="secondary" onClick={verificarQuorum} loading={quorum.isPending}>
          Verificar quórum
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('abrir')}>
          Abrir
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('suspender')}>
          Suspender
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('reabrir')}>
          Reabrir
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('encerrar')}>
          Encerrar
        </Button>
        <Button variant="danger" onClick={() => setAtivo('cancelar')}>
          Cancelar
        </Button>
      </div>

      <PresencaModal open={ativo === 'presenca'} onClose={fechar} id={id} />
      <IncluirOrdemDoDiaModal open={ativo === 'ordemDoDia'} onClose={fechar} id={id} />

      <ConfirmarAcaoModal
        open={ativo === 'abrir'}
        onClose={fechar}
        title="Abrir sessão"
        mensagem="Confirma a abertura da sessão plenária?"
        rotuloConfirmar="Abrir"
        mutation={abrir}
        sucesso="Sessão aberta."
        erroFallback="Não foi possível abrir a sessão."
      />
      <ConfirmarAcaoModal
        open={ativo === 'suspender'}
        onClose={fechar}
        title="Suspender sessão"
        mensagem="Confirma a suspensão da sessão?"
        rotuloConfirmar="Suspender"
        mutation={suspender}
        sucesso="Sessão suspensa."
        erroFallback="Não foi possível suspender a sessão."
      />
      <ConfirmarAcaoModal
        open={ativo === 'reabrir'}
        onClose={fechar}
        title="Reabrir sessão"
        mensagem="Confirma a reabertura da sessão suspensa?"
        rotuloConfirmar="Reabrir"
        mutation={reabrir}
        sucesso="Sessão reaberta."
        erroFallback="Não foi possível reabrir a sessão."
      />
      <ConfirmarAcaoModal
        open={ativo === 'encerrar'}
        onClose={fechar}
        title="Encerrar sessão"
        mensagem="Confirma o encerramento da sessão? Os trabalhos serão finalizados."
        rotuloConfirmar="Encerrar"
        mutation={encerrar}
        sucesso="Sessão encerrada."
        erroFallback="Não foi possível encerrar a sessão."
      />
      <ConfirmarAcaoModal
        open={ativo === 'cancelar'}
        onClose={fechar}
        title="Cancelar sessão"
        mensagem="Confirma o cancelamento da sessão agendada?"
        rotuloConfirmar="Cancelar sessão"
        variante="danger"
        mutation={cancelar}
        sucesso="Sessão cancelada."
        erroFallback="Não foi possível cancelar a sessão."
      />
    </Can>
  );
}
