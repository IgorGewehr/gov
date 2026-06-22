// RevogarLicitacao — encerra o certame por conveniência/oportunidade.
// Exige motivação (I-15) e é irreversível.
import { useRevogarLicitacao } from '../licitacao.api';
import { MotivoModalBase } from './MotivoModalBase';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function RevogarLicitacaoModal({ open, onClose, licitacaoId }: AcaoModalProps) {
  const mutation = useRevogarLicitacao(licitacaoId);
  return (
    <MotivoModalBase
      open={open}
      onClose={onClose}
      licitacaoId={licitacaoId}
      title="Revogar licitação"
      formId="form-revogar"
      confirmarLabel="Confirmar revogação"
      sucesso="Licitação revogada."
      falha="Não foi possível revogar a licitação."
      intro="Encerra o certame por conveniência/oportunidade. Exige motivação (I-15) e é irreversível."
      isPending={mutation.isPending}
      onConfirm={(motivo, cb) => mutation.mutate({ motivo }, cb)}
    />
  );
}
