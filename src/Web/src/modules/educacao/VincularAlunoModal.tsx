// Modal de vínculo de aluno a uma rota de transporte (PNATE). Espelha
// VincularAlunoRotaPayload (AlunoId, MatriculaId?, PontoEmbarque). O aluno é
// escolhido via AlunoPicker real (GET /educacao/alunos) — fim do GUID digitado.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useVincularAluno } from './transporte.api';
import type { VincularAlunoInput } from './transporte.api';
import { AlunoPicker } from './MatriculaPickers';
import type { AlunoItemLista } from './aluno.api';

export interface VincularAlunoModalProps {
  open: boolean;
  rotaId: string;
  onClose: () => void;
}

export function VincularAlunoModal({ open, rotaId, onClose }: VincularAlunoModalProps) {
  const toast = useToast();
  const mutation = useVincularAluno(rotaId);

  const [aluno, setAluno] = useState<AlunoItemLista | null>(null);
  const [pontoEmbarque, setPontoEmbarque] = useState('');
  const [erroAluno, setErroAluno] = useState<string | undefined>();
  const [erroPonto, setErroPonto] = useState<string | undefined>();

  function reiniciar(): void {
    setAluno(null);
    setPontoEmbarque('');
    setErroAluno(undefined);
    setErroPonto(undefined);
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const erros: boolean[] = [];
    if (!aluno) {
      setErroAluno('Selecione o aluno.');
      erros.push(true);
    } else {
      setErroAluno(undefined);
    }
    if (pontoEmbarque.trim() === '') {
      setErroPonto('Informe o ponto de embarque.');
      erros.push(true);
    } else {
      setErroPonto(undefined);
    }
    if (erros.length > 0 || !aluno) return;

    const input: VincularAlunoInput = {
      alunoId: aluno.id,
      matriculaId: null,
      pontoEmbarque: pontoEmbarque.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Aluno vinculado à rota.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível vincular o aluno.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Vincular aluno à rota"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vincular-aluno" loading={mutation.isPending}>
            Vincular aluno
          </Button>
        </>
      }
    >
      <form id="form-vincular-aluno" className="br-form" onSubmit={submeter} noValidate>
        <AlunoPicker selecionado={aluno} onSelecionar={setAluno} error={erroAluno} />

        <FormField label="Ponto de embarque" required error={erroPonto}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={pontoEmbarque}
              maxLength={200}
              onChange={(e) => setPontoEmbarque(e.target.value)}
              placeholder="Ex.: Esquina da Rua A com Av. B"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
