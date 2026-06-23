// Formulario de cadastro de unidade socioassistencial (command CadastrarUnidadeSocioassistencial).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCadastrarUnidade } from './censo.api';
import type { TipoUnidadeAtendimento } from './censo.api';
import { TIPO_UNIDADE_OPTIONS } from './censo.helpers';

export interface CadastrarUnidadeModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  nome?: string;
  territorioCobertura?: string;
  endereco?: string;
}

export function CadastrarUnidadeModal({ open, onClose }: CadastrarUnidadeModalProps) {
  const toast = useToast();
  const mutation = useCadastrarUnidade();

  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<TipoUnidadeAtendimento>('Cras');
  const [territorio, setTerritorio] = useState('');
  const [endereco, setEndereco] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function fechar(): void {
    setNome('');
    setTipo('Cras');
    setTerritorio('');
    setEndereco('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (nome.trim() === '') next.nome = 'Informe o nome da unidade.';
    if (territorio.trim() === '') next.territorioCobertura = 'Informe o território de cobertura.';
    if (endereco.trim() === '') next.endereco = 'Informe o endereço.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        nome: nome.trim(),
        tipo,
        territorioCobertura: territorio.trim(),
        endereco: endereco.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Unidade socioassistencial cadastrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
            const mapped: FormErrors = {};
            for (const [field, messages] of Object.entries(error.fieldErrors)) {
              const key = field.charAt(0).toLowerCase() + field.slice(1);
              if (key === 'nome' || key === 'territorioCobertura' || key === 'endereco') {
                (mapped as Record<string, string>)[key] = messages[0];
              }
            }
            setErrors(mapped);
          }
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível cadastrar a unidade.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar unidade socioassistencial"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-unidade" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-unidade" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome da unidade" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              maxLength={200}
            />
          )}
        </FormField>

        <FormField label="Tipo" required help="PAIF só pode ser ofertado em CRAS; PAEFI só em CREAS.">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={TIPO_UNIDADE_OPTIONS}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoUnidadeAtendimento)}
            />
          )}
        </FormField>

        <FormField label="Território de cobertura" required error={errors.territorioCobertura}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={territorio}
              onChange={(e) => setTerritorio(e.target.value)}
              maxLength={120}
            />
          )}
        </FormField>

        <FormField label="Endereço" required error={errors.endereco}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={endereco}
              onChange={(e) => setEndereco(e.target.value)}
              maxLength={300}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
