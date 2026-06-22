// Formulario de apresentacao de Proposicao em Modal (foco preso). Padrao de
// mutation + validacao por campo (FormField/aria-describedby) + Toast de feedback.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { REGIMES_TRAMITACAO, TIPOS_PROPOSICAO, useApresentarProposicao } from './api';
import type { ApresentarProposicaoInput } from './api';

export interface ProposicaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  tipo?: string;
  ementa?: string;
  autoria?: string;
  regime?: string;
}

const CAMPOS: ReadonlyArray<keyof FormErrors> = ['tipo', 'ementa', 'autoria', 'regime'];

export function ProposicaoFormModal({ open, onClose }: ProposicaoFormModalProps) {
  const toast = useToast();
  const mutation = useApresentarProposicao();

  const [tipo, setTipo] = useState<string>(String(TIPOS_PROPOSICAO[0].value));
  const [regime, setRegime] = useState<string>(String(REGIMES_TRAMITACAO[0].value));
  const [ementa, setEmenta] = useState('');
  const [autoria, setAutoria] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (ementa.trim() === '') next.ementa = 'Informe a ementa (resumo do objeto).';
    if (autoria.trim() === '') next.autoria = 'Informe a autoria (iniciativa).';
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

    const input: ApresentarProposicaoInput = {
      tipo: Number(tipo),
      regime: Number(regime),
      ementa: ementa.trim(),
      autoria: autoria.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Proposição apresentada (protocolada).', 'Sucesso');
        setEmenta('');
        setAutoria('');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apresentar a proposição.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Apresentar proposição"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-apresentar-proposicao" loading={mutation.isPending}>
            Apresentar
          </Button>
        </>
      }
    >
      <form id="form-apresentar-proposicao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de proposição" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_PROPOSICAO.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>

        <FormField label="Regime de tramitação" required error={errors.regime}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={regime}
              onChange={(e) => setRegime(e.target.value)}
              options={REGIMES_TRAMITACAO.map((r) => ({ value: String(r.value), label: r.label }))}
            />
          )}
        </FormField>

        <FormField label="Ementa" required error={errors.ementa} help="Resumo do objeto da proposição (máx. 1000 caracteres).">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              maxLength={1000}
              aria-describedby={describedBy}
              invalid={invalid}
              value={ementa}
              onChange={(e) => setEmenta(e.target.value)}
              placeholder="Dispõe sobre…"
            />
          )}
        </FormField>

        <FormField label="Autoria" required error={errors.autoria} help="Autor(es) / iniciativa.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              maxLength={400}
              aria-describedby={describedBy}
              invalid={invalid}
              value={autoria}
              onChange={(e) => setAutoria(e.target.value)}
              placeholder="Vereador(a) Fulano de Tal"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
