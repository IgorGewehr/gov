// Modal de MOVER uma Unidade Organizacional para um novo pai
// (POST /identidade/unidades/{id}/mover { novoPaiId }). Segue o PADRÃO-OURO dos
// modais de ação do Protocolo/Usuários: controlado, validação local, toast e
// invalidação via hook. O select de destino exclui a própria UO e toda a sua
// subárvore (evita ciclo) e mostra a hierarquia por indentação textual.
import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Select, useToast } from '../../../components/ui';
import type { SelectOption } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useMoverUnidade } from './unidades.api';
import type { NoUnidade } from './unidades.api';
import { achatarArvore, idsDaSubarvore } from './unidades.helpers';

export interface UnidadeMoverModalProps {
  open: boolean;
  onClose: () => void;
  /** UO a ser movida. */
  unidade: NoUnidade | null;
  /** Árvore completa de UOs (para montar os destinos possíveis). */
  arvore: readonly NoUnidade[];
}

const PREFIXO_NIVEL = '    '; // 4 espaços não quebráveis por nível.

export function UnidadeMoverModal({ open, onClose, unidade, arvore }: UnidadeMoverModalProps) {
  const toast = useToast();
  const mutation = useMoverUnidade();
  const [novoPaiId, setNovoPaiId] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);

  // Destinos válidos: todas as UOs, exceto a própria e seus descendentes.
  const opcoes = useMemo<SelectOption[]>(() => {
    if (!unidade) return [];
    const bloqueados = idsDaSubarvore(unidade);
    return achatarArvore(arvore)
      .filter((linha) => !bloqueados.has(linha.unidade.id))
      .map((linha) => ({
        value: linha.unidade.id,
        label: `${PREFIXO_NIVEL.repeat(linha.nivel)}${linha.unidade.nome}`,
      }));
  }, [unidade, arvore]);

  useEffect(() => {
    if (!open) return;
    setNovoPaiId('');
    setErro(undefined);
  }, [open]);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!unidade) return;
    if (novoPaiId === '') {
      setErro('Selecione a unidade de destino.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { id: unidade.id, input: { novoPaiId } },
      {
        onSuccess: () => {
          toast.success('Unidade movida.', 'Sucesso');
          onClose();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível mover a unidade.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={unidade ? `Mover unidade — ${unidade.nome}` : 'Mover unidade'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-mover" loading={mutation.isPending}>
            Mover
          </Button>
        </>
      }
    >
      <form id="form-mover" className="br-form" onSubmit={submeter} noValidate>
        {opcoes.length === 0 ? (
          <Alert variant="info">
            Não há unidade de destino disponível para esta operação.
          </Alert>
        ) : (
          <FormField
            label="Nova unidade superior"
            required
            error={erro}
            help="A unidade passará a ser subordinada à unidade selecionada."
          >
            {({ id, describedBy, invalid }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={novoPaiId}
                onChange={(e) => setNovoPaiId(e.target.value)}
                placeholder="Selecione a unidade de destino"
                options={opcoes}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
