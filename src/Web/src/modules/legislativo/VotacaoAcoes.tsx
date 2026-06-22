// Barra de ACOES da Votacao (gated por "legislativo.gerenciar"). Registrar voto,
// encerrar (apura o resultado) e cancelar. Mantem a DetailPage enxuta.
import { useState } from 'react';
import { Button, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ConfirmarAcaoModal } from './ConfirmarAcaoModal';
import { RegistrarVotoModal } from './VotacaoAcaoModais';
import { useEncerrarVotacao, useCancelarVotacao } from './votacao.api';
import { mensagemErro } from './legislativoAcao.shared';

type ModalAtivo = null | 'voto' | 'cancelar';

export function VotacaoAcoes({ id }: { id: string }) {
  const toast = useToast();
  const [ativo, setAtivo] = useState<ModalAtivo>(null);
  const fechar = () => setAtivo(null);

  const encerrar = useEncerrarVotacao(id);
  const cancelar = useCancelarVotacao(id);

  function encerrarVotacao(): void {
    encerrar.mutate(undefined, {
      onSuccess: (resultado) => toast.success(`Votação encerrada: ${resultado}.`, 'Resultado'),
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível encerrar a votação.')),
    });
  }

  return (
    <Can permission="legislativo.gerenciar">
      <div className="d-flex flex-wrap gap-2 mb-4" role="group" aria-label="Ações da votação">
        <Button variant="secondary" onClick={() => setAtivo('voto')}>
          Registrar voto
        </Button>
        <Button variant="secondary" onClick={encerrarVotacao} loading={encerrar.isPending}>
          Encerrar (apurar)
        </Button>
        <Button variant="danger" onClick={() => setAtivo('cancelar')}>
          Cancelar
        </Button>
      </div>

      <RegistrarVotoModal open={ativo === 'voto'} onClose={fechar} id={id} />

      <ConfirmarAcaoModal
        open={ativo === 'cancelar'}
        onClose={fechar}
        title="Cancelar votação"
        mensagem="Confirma o cancelamento da votação? Os votos registrados serão descartados."
        rotuloConfirmar="Cancelar votação"
        variante="danger"
        mutation={cancelar}
        sucesso="Votação cancelada."
        erroFallback="Não foi possível cancelar a votação."
      />
    </Can>
  );
}
