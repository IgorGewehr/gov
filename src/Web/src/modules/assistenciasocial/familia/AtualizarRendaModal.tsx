// Modal de acao do command AtualizarRendaFamiliar: edita a composicao/renda da familia,
// recalcula a renda per capita e renova o marco cadastral (pode regularizar familia vencida).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAtualizarRendaFamiliar } from './familia.api';
import type { MembroFamiliarInput } from './familia.api';
import {
  MembrosFieldset,
  linhasParaMembros,
  membroLinhaVazia,
} from './MembrosFieldset';
import type { MembroLinha } from './MembrosFieldset';

export interface AtualizarRendaModalProps {
  open: boolean;
  onClose: () => void;
  familiaId: string;
  /** Composicao atual (quando conhecida) para pre-preencher o editor. */
  membrosIniciais?: MembroFamiliarInput[];
}

function paraLinhas(membros?: MembroFamiliarInput[]): MembroLinha[] {
  if (!membros || membros.length === 0) {
    return [{ ...membroLinhaVazia(), parentesco: '1' }];
  }
  return membros.map((m) => ({
    cpf: m.cpf,
    parentesco: String(m.parentesco),
    dataNascimento: m.dataNascimento,
    rendaIndividual: String(m.rendaIndividual),
    ehPcd: m.ehPcd,
  }));
}

export function AtualizarRendaModal({ open, onClose, familiaId, membrosIniciais }: AtualizarRendaModalProps) {
  const toast = useToast();
  const mutation = useAtualizarRendaFamiliar();
  const [linhas, setLinhas] = useState<MembroLinha[]>(() => paraLinhas(membrosIniciais));
  const [erroMembros, setErroMembros] = useState<string | undefined>();

  function fechar(): void {
    setLinhas(paraLinhas(membrosIniciais));
    setErroMembros(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (linhas.length === 0) {
      setErroMembros('A família deve ter ao menos um membro.');
      return;
    }
    setErroMembros(undefined);

    mutation.mutate(
      { familiaId, membros: linhasParaMembros(linhas) },
      {
        onSuccess: () => {
          toast.success('Renda familiar atualizada e cadastro renovado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors['Membros']) {
            setErroMembros(error.fieldErrors['Membros'][0]);
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível atualizar a renda.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atualizar renda e composição familiar"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-atualizar-renda" loading={mutation.isPending}>
            Atualizar
          </Button>
        </>
      }
    >
      <form id="form-atualizar-renda" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          Atualizar a composição/renda recalcula a renda per capita e renova a vigência cadastral. Se a
          família estiver com atualização vencida, ela será regularizada.
        </Alert>

        <MembrosFieldset
          linhas={linhas}
          onChange={setLinhas}
          erroGeral={erroMembros}
          disabled={mutation.isPending}
        />
      </form>
    </Modal>
  );
}
