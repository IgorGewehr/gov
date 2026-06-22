// [Command ReclassificarAbc §5.5] Reclassifica o item na Curva ABC (A/B/C). Validação: classe
// válida (Select garante). Pré-seleciona a classe atual do item.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useReclassificarAbc } from './itemestoque.api';
import type { ClassificacaoAbcNome, ReclassificarAbcInput } from './itemestoque.api';
import { CLASSIFICACAO_ABC } from './itemestoque.api';
import { OPCOES_CLASSIFICACAO_ABC } from './itemEstoque.helpers';

export interface ItemEstoqueAbcModalProps {
  open: boolean;
  onClose: () => void;
  itemId: string;
  /** Classe atual, usada como valor inicial do Select. */
  classeAtual: ClassificacaoAbcNome;
}

export function ItemEstoqueAbcModal({ open, onClose, itemId, classeAtual }: ItemEstoqueAbcModalProps) {
  const toast = useToast();
  const mutation = useReclassificarAbc(itemId);

  const [classificacaoAbc, setClassificacaoAbc] = useState(String(CLASSIFICACAO_ABC[classeAtual]));

  function fechar(): void {
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const input: ReclassificarAbcInput = { classificacaoAbc: Number(classificacaoAbc) };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Classificação ABC atualizada.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível reclassificar o item.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Reclassificar na Curva ABC"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abc-item" loading={mutation.isPending}>
            Reclassificar
          </Button>
        </>
      }
    >
      <form id="form-abc-item" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Classe ABC" required help="A: alta relevância · B: média · C: baixa.">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={OPCOES_CLASSIFICACAO_ABC}
              value={classificacaoAbc}
              onChange={(e) => setClassificacaoAbc(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
