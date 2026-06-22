// AtualizarNivelSicaf -> AtualizarNivelSicafCommand (PUT .../nivel-sicaf).
// Mutation + Toast + mapeamento de ApiError.fieldErrors (campo Nivel).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, useToast } from '../../../../components/ui';
import type { SelectOption } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { NIVEIS_SICAF, NIVEL_SICAF_LABEL, useAtualizarNivelSicaf } from '../fornecedor.api';
import type { AtualizarNivelSicafInput, NivelCadastralSICAF } from '../fornecedor.api';
import type { AcaoModalProps } from './acoesModais.shared';

interface AtualizarNivelModalProps extends AcaoModalProps {
  /** Nivel atual (pre-seleciona o Select). */
  nivelAtual?: NivelCadastralSICAF;
}

export function AtualizarNivelSicafModal({ open, onClose, fornecedorId, nivelAtual }: AtualizarNivelModalProps) {
  const toast = useToast();
  const mutation = useAtualizarNivelSicaf(fornecedorId);

  const [nivel, setNivel] = useState<NivelCadastralSICAF>(nivelAtual ?? 'NaoCadastrado');
  const [erro, setErro] = useState<string | undefined>(undefined);

  const nivelOptions: SelectOption[] = NIVEIS_SICAF.map((n) => ({ value: n, label: NIVEL_SICAF_LABEL[n] }));

  function fechar(): void {
    setNivel(nivelAtual ?? 'NaoCadastrado');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const input: AtualizarNivelSicafInput = { nivel };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Nivel cadastral SICAF atualizado.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        const msg = error instanceof ApiError ? error.userMessage : 'Nao foi possivel atualizar o nivel SICAF.';
        setErro(error instanceof ApiError ? error.fieldErrors.Nivel?.[0] : undefined);
        toast.error(msg);
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atualizar nivel cadastral SICAF"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-nivel-sicaf" loading={mutation.isPending}>
            Atualizar
          </Button>
        </>
      }
    >
      <form id="form-nivel-sicaf" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nivel cadastral SICAF" required error={erro} help="Consultado no SICAF / Compras.gov.br (art. 87).">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nivel}
              onChange={(e) => setNivel(e.target.value as NivelCadastralSICAF)}
              options={nivelOptions}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
