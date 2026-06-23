// [Command RegistrarContagem] Registra a contagem física de um item do snapshot do
// inventário (em Modal). O bem é pré-selecionado a partir da linha do snapshot.
// Validação por campo + mapeamento de ApiError.fieldErrors e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useRegistrarContagem } from './inventario.api';
import type { ItemInventarioDto, RegistrarContagemInput } from './inventario.api';
import { SITUACAO_ENCONTRADA } from './inventario.api';
import { OPCOES_SITUACAO_ENCONTRADA, mapearFieldErrors } from './inventario.helpers';

export interface InventarioContagemModalProps {
  open: boolean;
  onClose: () => void;
  inventarioId: string;
  /** Linha do snapshot sendo contada (nulo quando fechado). */
  item: ItemInventarioDto | null;
}

const CAMPOS = ['situacaoEncontrada', 'localizacaoEncontrada', 'observacao'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function InventarioContagemModal({
  open,
  onClose,
  inventarioId,
  item,
}: InventarioContagemModalProps) {
  const toast = useToast();
  const mutation = useRegistrarContagem(inventarioId);

  const [situacao, setSituacao] = useState(String(SITUACAO_ENCONTRADA.Localizado));
  const [localizacao, setLocalizacao] = useState('');
  const [observacao, setObservacao] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (localizacao.trim().length > 200)
      next.localizacaoEncontrada = 'Localização deve ter no máximo 200 caracteres.';
    if (observacao.trim().length > 500)
      next.observacao = 'Observação deve ter no máximo 500 caracteres.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setSituacao(String(SITUACAO_ENCONTRADA.Localizado));
    setLocalizacao('');
    setObservacao('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!item) return;
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: RegistrarContagemInput = {
      bemPatrimonialId: item.bemPatrimonialId,
      situacaoEncontrada: Number(situacao),
      localizacaoEncontrada: localizacao.trim() || null,
      observacao: observacao.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Contagem registrada.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a contagem.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar contagem"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-contagem-inventario"
            loading={mutation.isPending}
          >
            Registrar contagem
          </Button>
        </>
      }
    >
      <form id="form-contagem-inventario" className="br-form" onSubmit={submeter} noValidate>
        {item && (
          <p className="text-down-01 text-gray-60 mb-3">
            <strong>{item.descricaoSnapshot}</strong>
            {item.numeroTombamento ? ` — Tombo ${item.numeroTombamento}` : ''}
            {item.localizacaoEsperada ? ` — Esperado em ${item.localizacaoEsperada}` : ''}
          </p>
        )}

        <FormField label="Situação encontrada" required error={errors.situacaoEncontrada}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_SITUACAO_ENCONTRADA}
              value={situacao}
              onChange={(e) => setSituacao(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Localização encontrada"
          error={errors.localizacaoEncontrada}
          help="Onde o bem foi efetivamente localizado (opcional)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={localizacao}
              onChange={(e) => setLocalizacao(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Observação" error={errors.observacao} help="Anotação livre (opcional).">
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={500}
              rows={3}
              value={observacao}
              onChange={(e) => setObservacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
