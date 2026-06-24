// Formulário de EMISSÃO de portaria / ato de pessoal em Modal. A numeração é apurada no backend
// (sequencial por exercício/tenant) — o usuário não a informa. Vínculo opcional ao servidor (matrícula).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useEmitirPortaria } from './api';
import type { EmitirPortariaInput } from './api';
import { TIPOS_PORTARIA } from './recursosHumanos.helpers';

export interface EmitirPortariaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando informado, pré-vincula a portaria a este servidor (ficha funcional). */
  servidorId?: string;
}

interface FormErrors {
  tipo?: string;
  ementa?: string;
  texto?: string;
}

export function EmitirPortariaFormModal({ open, onClose, servidorId }: EmitirPortariaFormModalProps) {
  const toast = useToast();
  const mutation = useEmitirPortaria();

  const [tipo, setTipo] = useState('');
  const [ementa, setEmenta] = useState('');
  const [texto, setTexto] = useState('');
  const [dataAto, setDataAto] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (tipo.trim() === '') next.tipo = 'Selecione a natureza do ato.';
    if (ementa.trim() === '') next.ementa = 'Informe a ementa do ato.';
    if (texto.trim() === '') next.texto = 'Informe o texto do ato.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: EmitirPortariaInput = {
      tipo: Number(tipo),
      ementa: ementa.trim(),
      texto: texto.trim(),
      servidorId: servidorId ?? null,
      dataAto: dataAto.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Portaria emitida.', 'Sucesso');
        setTipo('');
        setEmenta('');
        setTexto('');
        setDataAto('');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível emitir a portaria.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir portaria"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-emitir-portaria"
            loading={mutation.isPending}
          >
            Emitir
          </Button>
        </>
      }
    >
      <form id="form-emitir-portaria" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Natureza do ato"
          required
          error={errors.tipo}
          help="A numeração sequencial é atribuída automaticamente no exercício."
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              placeholder="Selecione a natureza"
              options={TIPOS_PORTARIA}
            />
          )}
        </FormField>

        <FormField label="Ementa" required error={errors.ementa}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={ementa}
              onChange={(e) => setEmenta(e.target.value)}
              maxLength={500}
              placeholder="Resumo do ato"
            />
          )}
        </FormField>

        <FormField label="Texto do ato" required error={errors.texto}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              maxLength={20000}
              rows={8}
            />
          )}
        </FormField>

        <FormField label="Data do ato" help="Opcional; em branco usa a data de hoje do município.">
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              value={dataAto}
              onChange={(e) => setDataAto(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
