// Formulário de cadastro de Profissional de saúde em Modal. CPF + nome + CNS opcional +
// registro de conselho opcional (CBO/CRM são tratados no vínculo, separadamente).
// Padrão-ouro: mutation + validação por campo + Toast + mapeamento de ProblemDetails.
// Ação gated por "saude.gerenciar" na página que o abre.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarProfissional } from './api';
import type { CadastrarProfissionalInput } from './api';
import { opcoesConselho } from './saude.helpers';

export interface ProfissionalFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  cpf?: string;
  nome?: string;
  cns?: string;
  uf?: string;
  numero?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  cpf: true,
  nome: true,
  cns: true,
  uf: true,
  numero: true,
};

export function ProfissionalFormModal({ open, onClose }: ProfissionalFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useCadastrarProfissional();

  const [cpf, setCpf] = useState('');
  const [nome, setNome] = useState('');
  const [cns, setCns] = useState('');
  const [comConselho, setComConselho] = useState(false);
  const [tipoConselho, setTipoConselho] = useState('1');
  const [ufConselho, setUfConselho] = useState('');
  const [numeroConselho, setNumeroConselho] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (cpf.replace(/\D/g, '').length !== 11) next.cpf = 'Informe um CPF válido (11 dígitos).';
    if (nome.trim() === '') next.nome = 'Informe o nome do profissional.';
    if (cns.trim() !== '' && cns.trim().length !== 15) next.cns = 'O CNS deve ter 15 dígitos.';
    if (comConselho) {
      if (ufConselho.trim().length !== 2) next.uf = 'A UF do registro deve ter 2 caracteres.';
      if (numeroConselho.trim() === '') next.numero = 'Informe o número do registro.';
    }
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

    const input: CadastrarProfissionalInput = {
      cpf: cpf.replace(/\D/g, ''),
      nome: nome.trim(),
      cns: cns.trim() || null,
      registro: comConselho
        ? {
            tipo: Number(tipoConselho),
            uf: ufConselho.trim().toUpperCase(),
            numero: numeroConselho.trim(),
          }
        : null,
    };

    mutation.mutate(input, {
      onSuccess: (id) => {
        toast.success('Profissional cadastrado com sucesso.', 'Sucesso');
        fechar();
        navigate(`/saude/profissionais/${id}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const leaf = field.split('.').pop() ?? field;
            const key = (leaf.charAt(0).toLowerCase() + leaf.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o profissional.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar profissional"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-cadastrar-profissional"
            loading={mutation.isPending}
          >
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-profissional" className="br-form" onSubmit={submeter} noValidate>
        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Identificação</legend>

          <FormField label="CPF" required error={errors.cpf}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={14}
                value={cpf}
                onChange={(e) => setCpf(e.target.value)}
                placeholder="000.000.000-00"
              />
            )}
          </FormField>

          <FormField label="Nome" required error={errors.nome}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={120}
                value={nome}
                onChange={(e) => setNome(e.target.value)}
              />
            )}
          </FormField>

          <FormField label="CNS" help="Opcional — 15 dígitos." error={errors.cns}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={15}
                value={cns}
                onChange={(e) => setCns(e.target.value.replace(/\D/g, ''))}
                placeholder="000000000000000"
              />
            )}
          </FormField>
        </fieldset>

        <fieldset>
          <legend className="text-up-01 text-semi-bold">Registro de conselho</legend>

          <div className="br-checkbox mb-3">
            <input
              id="profissional-com-conselho"
              type="checkbox"
              checked={comConselho}
              onChange={(e) => setComConselho(e.target.checked)}
            />
            <label htmlFor="profissional-com-conselho">
              Possui registro em conselho de classe (CRM, COREN, ...)
            </label>
          </div>

          {comConselho && (
            <div className="row">
              <div className="col-sm-5">
                <FormField label="Conselho" required>
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesConselho}
                      value={tipoConselho}
                      onChange={(e) => setTipoConselho(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="UF" required error={errors.uf}>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      maxLength={2}
                      value={ufConselho}
                      onChange={(e) => setUfConselho(e.target.value.toUpperCase())}
                      placeholder="RS"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Número" required error={errors.numero}>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={numeroConselho}
                      onChange={(e) => setNumeroConselho(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          )}
        </fieldset>
      </form>
    </Modal>
  );
}
