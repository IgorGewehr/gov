// Modais de AÇÃO do agregado Matrícula (parte 1), abertos da MatriculaListPage:
//   - Rematricular (renova p/ turma destino)        -> POST matriculas/rematricula
//   - Transferir (movimento p/ outra escola/turma)  -> POST .../transferencia
// Encerramento e Situação do Aluno ficam em MatriculaSituacaoModais (re-exportados
// abaixo) para manter os arquivos < 300 linhas. Cada modal é WIRED a uma mutation.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useRematricularAluno, useTransferirAluno } from './api';
import type { MatriculaAcaoBaseProps as AcaoBaseProps } from './matricula.acoes.types';

export { EncerrarMatriculaModal, RegistrarSituacaoModal } from './MatriculaSituacaoModais';

// ---------------------------------------------------------------------------
// REMATRICULAR (POST /matriculas/rematricula)
// ---------------------------------------------------------------------------

export function RematricularModal({ open, onClose, matriculaId }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useRematricularAluno();
  const [turmaDestinoId, setTurmaDestinoId] = useState('');
  const [dataReferencia, setDataReferencia] = useState('');
  const [errors, setErrors] = useState<{ turmaDestinoId?: string; dataReferencia?: string }>({});

  function fechar(): void {
    setErrors({});
    setTurmaDestinoId('');
    setDataReferencia('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { turmaDestinoId?: string; dataReferencia?: string } = {};
    if (turmaDestinoId.trim() === '') next.turmaDestinoId = 'Informe a turma destino.';
    if (dataReferencia.trim() === '') next.dataReferencia = 'Informe a data de referência.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        matriculaAnteriorId: matriculaId,
        turmaDestinoId: turmaDestinoId.trim(),
        dataReferencia,
      },
      {
        onSuccess: () => {
          toast.success('Aluno rematriculado.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível rematricular o aluno.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Rematricular aluno"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-rematricular" loading={mutation.isPending}>
            Rematricular
          </Button>
        </>
      }
    >
      <form id="form-rematricular" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">Matrícula anterior: {matriculaId}</p>
        <FormField label="Turma destino (identificador)" required error={errors.turmaDestinoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={turmaDestinoId}
              onChange={(e) => setTurmaDestinoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Data de referência (Censo)" required error={errors.dataReferencia}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataReferencia}
              onChange={(e) => setDataReferencia(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// TRANSFERIR (POST /matriculas/{id}/transferencia) — sem corpo
// ---------------------------------------------------------------------------

export function TransferirModal({ open, onClose, matriculaId, alunoId }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useTransferirAluno(alunoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(matriculaId, {
      onSuccess: () => {
        toast.success('Aluno transferido.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível transferir o aluno.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Transferir aluno"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-transferir" loading={mutation.isPending}>
            Confirmar transferência
          </Button>
        </>
      }
    >
      <form id="form-transferir" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="warning" title="Atenção:">
          A transferência encerra o vínculo desta matrícula (estado terminal). Confirme para
          prosseguir.
        </Alert>
      </form>
    </Modal>
  );
}
