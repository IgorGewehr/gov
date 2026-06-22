// Modais de AÇÃO da Matrícula (parte 2): encerramento e Situação do Aluno (2ª etapa
// do Censo). Complementam MatriculaAcaoModais (rematricular/transferir). Mantém os
// arquivos < 300 linhas. Cada modal é WIRED a uma mutation, com Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  MOTIVO_ENCERRAMENTO_VALOR,
  MOVIMENTO_VALOR,
  RENDIMENTO_VALOR,
  useEncerrarMatricula,
  useRegistrarSituacaoDoAluno,
} from './api';
import type { MotivoEncerramento, Movimento, Rendimento } from './api';
import type { MatriculaAcaoBaseProps } from './matricula.acoes.types';

// ---------------------------------------------------------------------------
// ENCERRAR (POST /matriculas/{id}/encerramento)
// ---------------------------------------------------------------------------

const MOTIVOS: { value: MotivoEncerramento; label: string }[] = [
  { value: 'Conclusao', label: 'Conclusão da etapa/ano' },
  { value: 'Abandono', label: 'Abandono escolar' },
];

export function EncerrarMatriculaModal({ open, onClose, matriculaId, alunoId }: MatriculaAcaoBaseProps) {
  const toast = useToast();
  const mutation = useEncerrarMatricula(alunoId);
  const [motivo, setMotivo] = useState<MotivoEncerramento>('Conclusao');

  function fechar(): void {
    setMotivo('Conclusao');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      { matriculaId, input: { motivo: MOTIVO_ENCERRAMENTO_VALOR[motivo] } },
      {
        onSuccess: () => {
          toast.success('Matrícula encerrada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar a matrícula.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar matrícula"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-encerrar" loading={mutation.isPending}>
            Encerrar
          </Button>
        </>
      }
    >
      <form id="form-encerrar" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Motivo do encerramento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={MOTIVOS}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value as MotivoEncerramento)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// REGISTRAR SITUAÇÃO DO ALUNO (POST /matriculas/{id}/situacao-aluno)
// ---------------------------------------------------------------------------

const RENDIMENTOS: { value: Rendimento; label: string }[] = [
  { value: 'Aprovado', label: 'Aprovado' },
  { value: 'Reprovado', label: 'Reprovado' },
];

const MOVIMENTOS: { value: Movimento; label: string }[] = [
  { value: 'SemMovimento', label: 'Sem movimento' },
  { value: 'Transferido', label: 'Transferido' },
  { value: 'Abandono', label: 'Abandono' },
  { value: 'Falecido', label: 'Falecido' },
];

export function RegistrarSituacaoModal({ open, onClose, matriculaId, alunoId }: MatriculaAcaoBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarSituacaoDoAluno(alunoId);
  const [rendimento, setRendimento] = useState<Rendimento>('Aprovado');
  const [movimento, setMovimento] = useState<Movimento>('SemMovimento');

  function fechar(): void {
    setRendimento('Aprovado');
    setMovimento('SemMovimento');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      {
        matriculaId,
        input: { rendimento: RENDIMENTO_VALOR[rendimento], movimento: MOVIMENTO_VALOR[movimento] },
      },
      {
        onSuccess: () => {
          toast.success('Situação do aluno registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a situação.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar Situação do Aluno"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-situacao-aluno" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-situacao-aluno" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Rendimento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={RENDIMENTOS}
              value={rendimento}
              onChange={(e) => setRendimento(e.target.value as Rendimento)}
            />
          )}
        </FormField>
        <FormField label="Movimento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={MOVIMENTOS}
              value={movimento}
              onChange={(e) => setMovimento(e.target.value as Movimento)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
