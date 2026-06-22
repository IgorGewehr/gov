// AnularLicitacao — encerra o certame por ilegalidade.
// Exige motivação (I-15) e é irreversível.
import { useAnularLicitacao } from '../licitacao.api';
import { MotivoModalBase } from './MotivoModalBase';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function AnularLicitacaoModal({ open, onClose, licitacaoId }: AcaoModalProps) {
  const mutation = useAnularLicitacao(licitacaoId);
  return (
    <MotivoModalBase
      open={open}
      onClose={onClose}
      licitacaoId={licitacaoId}
      title="Anular licitação"
      formId="form-anular"
      confirmarLabel="Confirmar anulação"
      sucesso="Licitação anulada."
      falha="Não foi possível anular a licitação."
      intro="Encerra o certame por ilegalidade. Exige motivação (I-15) e é irreversível."
      isPending={mutation.isPending}
      onConfirm={(motivo, cb) => mutation.mutate({ motivo }, cb)}
    />
  );
}
