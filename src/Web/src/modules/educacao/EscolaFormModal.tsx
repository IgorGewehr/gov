// Formulário de credenciamento de Escola em Modal (foco preso). Demonstra o padrão
// de mutation + validação por campo (FormField/aria-describedby) + Toast de sucesso/erro.
// Os campos de Endereço/Infraestrutura são compartilhados via EscolaCamposCenso.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCredenciarEscola } from './api';
import type { CredenciarEscolaInput, DependenciaAdministrativa } from './api';
import {
  CAMPOS_CENSO_CHAVES,
  EscolaCamposCenso,
  censoVazio,
  montarCenso,
  validarCenso,
} from './EscolaCamposCenso';
import type { CamposCensoErrors, CamposCensoState } from './EscolaCamposCenso';

export interface EscolaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors extends CamposCensoErrors {
  codigoInep?: string;
  nome?: string;
  dependencia?: string;
}

const DEPENDENCIAS: SelectOption[] = [
  { value: 'Municipal', label: 'Municipal' },
  { value: 'Estadual', label: 'Estadual' },
  { value: 'Federal', label: 'Federal' },
  { value: 'Privada', label: 'Privada' },
];

const CAMPOS_VALIDOS: Record<string, true> = {
  ...CAMPOS_CENSO_CHAVES,
  codigoInep: true,
  nome: true,
  dependencia: true,
};

export function EscolaFormModal({ open, onClose }: EscolaFormModalProps) {
  const toast = useToast();
  const mutation = useCredenciarEscola();

  const [codigoInep, setCodigoInep] = useState('');
  const [nome, setNome] = useState('');
  const [dependencia, setDependencia] = useState<DependenciaAdministrativa>('Municipal');
  const [censo, setCenso] = useState<CamposCensoState>(censoVazio());
  const [errors, setErrors] = useState<FormErrors>({});

  function alterarCenso(parcial: Partial<CamposCensoState>): void {
    setCenso((atual) => ({ ...atual, ...parcial }));
  }

  function validar(): FormErrors {
    const next: FormErrors = { ...validarCenso(censo) };
    if (codigoInep.trim() === '') next.codigoInep = 'Informe o código INEP (8 dígitos).';
    else if (codigoInep.trim().length > 8) next.codigoInep = 'O código INEP tem no máximo 8 dígitos.';
    if (nome.trim() === '') next.nome = 'Informe o nome da escola.';
    else if (nome.trim().length > 150) next.nome = 'O nome tem no máximo 150 caracteres.';
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

    const { endereco, infraestrutura } = montarCenso(censo);
    const input: CredenciarEscolaInput = {
      codigoInep: codigoInep.trim(),
      nome: nome.trim(),
      dependencia,
      endereco,
      infraestrutura,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Escola credenciada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in CAMPOS_VALIDOS) {
              (mapped as Record<string, string>)[key] = messages[0];
            }
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível credenciar a escola.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Credenciar escola"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-credenciar-escola" loading={mutation.isPending}>
            Credenciar
          </Button>
        </>
      }
    >
      <form id="form-credenciar-escola" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-4">
            <FormField label="Código INEP" required error={errors.codigoInep}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={codigoInep}
                  onChange={(e) => setCodigoInep(e.target.value)}
                  inputMode="numeric"
                  maxLength={8}
                  placeholder="12345678"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-8">
            <FormField label="Nome da escola" required error={errors.nome}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={nome}
                  onChange={(e) => setNome(e.target.value)}
                  placeholder="EMEF Maximiliano de Almeida"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Dependência administrativa" required error={errors.dependencia}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={DEPENDENCIAS}
              value={dependencia}
              onChange={(e) => setDependencia(e.target.value as DependenciaAdministrativa)}
            />
          )}
        </FormField>

        <EscolaCamposCenso estado={censo} errors={errors} onChange={alterarCenso} idSufixo="credenciar" />
      </form>
    </Modal>
  );
}
