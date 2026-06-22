// DeclararFracassada — encerra o certame por inexistência de proposta
// válida/habilitada. Exige motivação e é irreversível (I-11).
import { useDeclararFracassada } from '../licitacao.api';
import { MotivoModalBase } from './MotivoModalBase';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function DeclararFracassadaModal({ open, onClose, licitacaoId }: AcaoModalProps) {
  const mutation = useDeclararFracassada(licitacaoId);
  return (
    <MotivoModalBase
      open={open}
      onClose={onClose}
      licitacaoId={licitacaoId}
      title="Declarar fracassada"
      formId="form-fracassada"
      confirmarLabel="Confirmar fracasso"
      sucesso="Licitação declarada fracassada."
      falha="Não foi possível declarar fracassada."
      intro="Encerra o certame por inexistência de proposta válida/habilitada. Esta ação é irreversível (I-11)."
      isPending={mutation.isPending}
      onConfirm={(motivo, cb) => mutation.mutate({ motivo }, cb)}
    />
  );
}
