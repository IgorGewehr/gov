// Formulário de ADMISSÃO de servidor em Modal (foco preso). Padrão-ouro:
// mutation + validação por campo (FormField/aria-describedby) + Toast de sucesso/erro,
// com mapeamento de ProblemDetails.errors do backend. Carrega cargos com vagas (Select).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAdmitirServidor, useCargosComVagas } from './api';
import type { AdmitirServidorInput } from './api';
import { REGIMES_PREVIDENCIARIOS } from './recursosHumanos.helpers';

export interface AdmitirServidorFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  cpf?: string;
  matricula?: string;
  nome?: string;
  dataNascimento?: string;
  cargoId?: string;
  regime?: string;
  dataNomeacao?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  cpf: true,
  matricula: true,
  nome: true,
  dataNascimento: true,
  cargoId: true,
  regime: true,
  dataNomeacao: true,
};

export function AdmitirServidorFormModal({ open, onClose }: AdmitirServidorFormModalProps) {
  const toast = useToast();
  const mutation = useAdmitirServidor();
  const cargosQuery = useCargosComVagas();

  const [cpf, setCpf] = useState('');
  const [matricula, setMatricula] = useState('');
  const [nome, setNome] = useState('');
  const [dataNascimento, setDataNascimento] = useState('');
  const [cargoId, setCargoId] = useState('');
  const [regime, setRegime] = useState('');
  const [dataNomeacao, setDataNomeacao] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const cargoOptions: SelectOption[] = (cargosQuery.data ?? []).map((cargo) => ({
    value: cargo.id,
    label: `${cargo.denominacao} (${cargo.tipo}) — ${cargo.vagasDisponiveis} vaga(s)`,
  }));

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (cpf.trim() === '') next.cpf = 'Informe o CPF do servidor.';
    if (matricula.trim() === '') next.matricula = 'Informe a matrícula.';
    if (nome.trim() === '') next.nome = 'Informe o nome do servidor.';
    if (dataNascimento.trim() === '') next.dataNascimento = 'Informe a data de nascimento.';
    if (cargoId.trim() === '') next.cargoId = 'Selecione o cargo provido.';
    if (regime.trim() === '') next.regime = 'Selecione o regime previdenciário.';
    if (dataNomeacao.trim() === '') next.dataNomeacao = 'Informe a data de nomeação.';
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

    const input: AdmitirServidorInput = {
      cpf: cpf.trim(),
      matricula: matricula.trim(),
      dadosPessoais: { nome: nome.trim(), dataNascimento },
      cargoId,
      regime: Number(regime),
      dataNomeacao,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Servidor admitido (matrícula ${input.matricula}).`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível admitir o servidor.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Admitir servidor"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-admitir-servidor"
            loading={mutation.isPending}
          >
            Admitir
          </Button>
        </>
      }
    >
      <form id="form-admitir-servidor" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="CPF" required error={errors.cpf}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cpf}
              onChange={(e) => setCpf(e.target.value)}
              inputMode="numeric"
              placeholder="000.000.000-00"
            />
          )}
        </FormField>

        <FormField label="Matrícula" required error={errors.matricula}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={matricula}
              onChange={(e) => setMatricula(e.target.value)}
              maxLength={20}
            />
          )}
        </FormField>

        <FormField label="Nome do servidor" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Data de nascimento" required error={errors.dataNascimento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataNascimento}
              onChange={(e) => setDataNascimento(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Cargo provido"
          required
          error={errors.cargoId}
          help={cargosQuery.isLoading ? 'Carregando cargos disponíveis…' : undefined}
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cargoId}
              onChange={(e) => setCargoId(e.target.value)}
              placeholder="Selecione um cargo com vaga"
              options={cargoOptions}
              disabled={cargosQuery.isLoading}
            />
          )}
        </FormField>

        <FormField label="Regime previdenciário" required error={errors.regime}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={regime}
              onChange={(e) => setRegime(e.target.value)}
              placeholder="Selecione o regime"
              options={REGIMES_PREVIDENCIARIOS}
            />
          )}
        </FormField>

        <FormField label="Data de nomeação" required error={errors.dataNomeacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataNomeacao}
              onChange={(e) => setDataNomeacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
