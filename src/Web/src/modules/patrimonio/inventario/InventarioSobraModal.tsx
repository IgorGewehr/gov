// [Command RegistrarBemNaoCadastrado] Registra um bem físico encontrado sem tombo
// ("sobra"/achado) durante a contagem (em Modal). Validações: descrição e localização
// obrigatórias, valor estimado >= 0. Mapeia ApiError.fieldErrors e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useRegistrarSobra } from './inventario.api';
import type { RegistrarSobraInput } from './inventario.api';
import { mapearFieldErrors } from './inventario.helpers';

export interface InventarioSobraModalProps {
  open: boolean;
  onClose: () => void;
  inventarioId: string;
}

const CAMPOS = ['descricao', 'localizacao', 'valorEstimado'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function InventarioSobraModal({ open, onClose, inventarioId }: InventarioSobraModalProps) {
  const toast = useToast();
  const mutation = useRegistrarSobra(inventarioId);

  const [descricao, setDescricao] = useState('');
  const [localizacao, setLocalizacao] = useState('');
  const [valorEstimado, setValorEstimado] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (descricao.trim() === '') next.descricao = 'Informe a descrição do achado.';
    else if (descricao.trim().length > 200) next.descricao = 'Descrição deve ter no máximo 200 caracteres.';
    if (localizacao.trim() === '') next.localizacao = 'Informe onde o bem foi encontrado.';
    else if (localizacao.trim().length > 200)
      next.localizacao = 'Localização deve ter no máximo 200 caracteres.';
    const valor = Number(valorEstimado);
    if (valorEstimado.trim() === '' || Number.isNaN(valor) || valor < 0)
      next.valorEstimado = 'Valor estimado não pode ser negativo.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setDescricao('');
    setLocalizacao('');
    setValorEstimado('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: RegistrarSobraInput = {
      descricao: descricao.trim(),
      localizacao: localizacao.trim(),
      valorEstimado: Number(valorEstimado),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Sobra (achado) registrada.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a sobra.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar sobra (bem sem tombo)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-sobra-inventario" loading={mutation.isPending}>
            Registrar sobra
          </Button>
        </>
      }
    >
      <form id="form-sobra-inventario" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Descrição" required error={errors.descricao} help="Identificação do bem encontrado.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              placeholder="Ex.: Cadeira de escritório sem plaqueta"
            />
          )}
        </FormField>

        <FormField label="Localização" required error={errors.localizacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={localizacao}
              onChange={(e) => setLocalizacao(e.target.value)}
              placeholder="Ex.: Sala 12 — Almoxarifado"
            />
          )}
        </FormField>

        <FormField
          label="Valor estimado (R$)"
          required
          error={errors.valorEstimado}
          help="Estimativa para futura incorporação."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorEstimado}
              onChange={(e) => setValorEstimado(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
