// Formulário de CADASTRO de contribuinte pessoa física (command
// CadastrarContribuintePessoaFisica) em Modal (foco preso). Espelha o validator:
// Cpf NotEmpty + válido; Nome NotEmpty + MaximumLength(200). Mapeia
// ProblemDetails.fieldErrors por campo e exibe Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarContribuintePf } from './api';
import type { CadastrarContribuintePfInput } from './api';

const NOME_MAX = 200;

export interface ContribuinteFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Callback opcional com o id do contribuinte recém-criado. */
  onCriado?: (id: string) => void;
}

interface FormErrors {
  cpf?: string;
  nome?: string;
  inscricaoMunicipal?: string;
}

export function ContribuinteFormModal({ open, onClose, onCriado }: ContribuinteFormModalProps) {
  const toast = useToast();
  const mutation = useCadastrarContribuintePf();

  const [cpf, setCpf] = useState('');
  const [nome, setNome] = useState('');
  const [inscricaoMunicipal, setInscricaoMunicipal] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (cpf.trim() === '') next.cpf = 'Informe o CPF do contribuinte.';
    const nomeTrim = nome.trim();
    if (nomeTrim === '') next.nome = 'Informe o nome completo.';
    else if (nomeTrim.length > NOME_MAX) next.nome = `O nome deve ter no máximo ${NOME_MAX} caracteres.`;
    return next;
  }

  function fechar(): void {
    setErrors({});
    setCpf('');
    setNome('');
    setInscricaoMunicipal('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CadastrarContribuintePfInput = {
      cpf: cpf.trim(),
      nome: nome.trim(),
      inscricaoMunicipal: inscricaoMunicipal.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success(`Contribuinte cadastrado (id ${resultado.id}).`, 'Sucesso');
        onCriado?.(resultado.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          const conhecidos: Record<string, number> = { cpf: 1, nome: 1, inscricaoMunicipal: 1 };
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in conhecidos) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o contribuinte.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar contribuinte (pessoa física)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-contribuinte" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-contribuinte" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="CPF" required error={errors.cpf} help="Com ou sem máscara.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              value={cpf}
              onChange={(e) => setCpf(e.target.value)}
              placeholder="000.000.000-00"
            />
          )}
        </FormField>

        <FormField label="Nome completo" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={NOME_MAX}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Inscrição municipal" error={errors.inscricaoMunicipal} help="Opcional.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={inscricaoMunicipal}
              onChange={(e) => setInscricaoMunicipal(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
