// Edição das PERMISSÕES de um papel existente (command DefinirPermissoes ->
// PUT /api/identidade/papeis/{id}/permissoes) em Modal (foco preso). Inicia com
// as permissões atuais do papel marcadas e substitui o conjunto ao salvar.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useDefinirPermissoes } from './papel.api';
import type { Papel } from './papel.api';
import { PermissoesCheckboxGroup } from './PermissoesCheckboxGroup';

export interface PapelPermissoesModalProps {
  open: boolean;
  onClose: () => void;
  /** Papel cujas permissões serão editadas (null quando o modal está fechado). */
  papel: Papel | null;
}

export function PapelPermissoesModal({ open, onClose, papel }: PapelPermissoesModalProps) {
  const toast = useToast();
  const mutation = useDefinirPermissoes();
  const [selecionadas, setSelecionadas] = useState<Set<string>>(new Set());

  // Sincroniza o estado local com as permissões atuais do papel ao abrir.
  useEffect(() => {
    if (open && papel) setSelecionadas(new Set(papel.permissoes));
  }, [open, papel]);

  function alternar(chave: string): void {
    setSelecionadas((atual) => {
      const proximo = new Set(atual);
      if (proximo.has(chave)) proximo.delete(chave);
      else proximo.add(chave);
      return proximo;
    });
  }

  function alternarGrupo(chaves: string[], marcar: boolean): void {
    setSelecionadas((atual) => {
      const proximo = new Set(atual);
      for (const chave of chaves) {
        if (marcar) proximo.add(chave);
        else proximo.delete(chave);
      }
      return proximo;
    });
  }

  function fechar(): void {
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!papel) return;

    mutation.mutate(
      { papelId: papel.id, input: { permissoes: Array.from(selecionadas) } },
      {
        onSuccess: () => {
          toast.success(`Permissões do papel "${papel.nome}" atualizadas.`, 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível atualizar as permissões do papel.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={papel ? `Permissões — ${papel.nome}` : 'Permissões do papel'}
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-permissoes-papel"
            loading={mutation.isPending}
          >
            Salvar permissões
          </Button>
        </>
      }
    >
      <form id="form-permissoes-papel" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-base mb-3">
          Marque as permissões que este papel concede. O conjunto selecionado substitui
          integralmente as permissões atuais ao salvar.
        </p>
        <PermissoesCheckboxGroup
          selecionadas={selecionadas}
          onToggle={alternar}
          onToggleGrupo={alternarGrupo}
          enabled={open}
        />
      </form>
    </Modal>
  );
}
