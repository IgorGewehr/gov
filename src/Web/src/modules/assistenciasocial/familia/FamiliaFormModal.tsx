// Formulario de REFERENCIAMENTO de familia ao CRAS em Modal (command ReferenciarFamilia).
// Padrao-ouro: mutation + validacao por campo (FormField/aria-describedby) + mapeamento de
// ProblemDetails.fieldErrors + Toast de sucesso/erro. gov.br DS + WCAG AA.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useReferenciarFamilia } from './familia.api';
import type { ReferenciarFamiliaInput } from './familia.api';
import {
  MembrosFieldset,
  linhasParaMembros,
  membroLinhaVazia,
} from './MembrosFieldset';
import type { MembroLinha } from './MembrosFieldset';

export interface FamiliaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pre-preenche o territorio quando aberto a partir de uma consulta. */
  territorioInicial?: string;
  /** Notifica a familia recem-referenciada (id), para navegacao opcional. */
  onReferenciada?: (familiaId: string) => void;
}

interface FormErrors {
  nis?: string;
  cpfResponsavel?: string;
  unidadeAtendimentoId?: string;
  logradouro?: string;
  municipio?: string;
  cep?: string;
  territorio?: string;
  membros?: string;
}

const ESTADO_INICIAL = () => ({
  nis: '',
  cpfResponsavel: '',
  unidadeAtendimentoId: '',
  logradouro: '',
  municipio: '',
  cep: '',
  territorio: '',
});

export function FamiliaFormModal({
  open,
  onClose,
  territorioInicial = '',
  onReferenciada,
}: FamiliaFormModalProps) {
  const toast = useToast();
  const mutation = useReferenciarFamilia();

  const [campos, setCampos] = useState(() => ({ ...ESTADO_INICIAL(), territorio: territorioInicial }));
  const [linhas, setLinhas] = useState<MembroLinha[]>(() => [
    { ...membroLinhaVazia(), parentesco: '1' },
  ]);
  const [errors, setErrors] = useState<FormErrors>({});
  const [errosMembro, setErrosMembro] = useState<Record<number, { cpf?: string }>>({});

  function set<K extends keyof ReturnType<typeof ESTADO_INICIAL>>(chave: K, valor: string): void {
    setCampos((c) => ({ ...c, [chave]: valor }));
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    const apenasDigitos = (v: string) => v.replace(/\D/g, '');
    if (apenasDigitos(campos.nis).length !== 11) next.nis = 'NIS é obrigatório e deve ter 11 dígitos.';
    if (apenasDigitos(campos.cpfResponsavel).length !== 11)
      next.cpfResponsavel = 'CPF do responsável é obrigatório e deve ter 11 dígitos.';
    if (campos.unidadeAtendimentoId.trim() === '')
      next.unidadeAtendimentoId = 'Unidade de atendimento (CRAS) é obrigatória.';
    if (campos.logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
    if (campos.municipio.trim() === '') next.municipio = 'Informe o município.';
    if (campos.cep.trim() === '') next.cep = 'Informe o CEP.';
    if (campos.territorio.trim() === '') next.territorio = 'Território do endereço é obrigatório.';
    if (linhas.length === 0) next.membros = 'A família deve ter ao menos um membro.';
    return next;
  }

  function fechar(): void {
    setCampos({ ...ESTADO_INICIAL(), territorio: territorioInicial });
    setLinhas([{ ...membroLinhaVazia(), parentesco: '1' }]);
    setErrors({});
    setErrosMembro({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    setErrosMembro({});
    if (Object.keys(validacao).length > 0) return;

    const input: ReferenciarFamiliaInput = {
      nis: campos.nis.trim(),
      cpfResponsavel: campos.cpfResponsavel.trim(),
      unidadeAtendimentoId: campos.unidadeAtendimentoId.trim(),
      endereco: {
        logradouro: campos.logradouro.trim(),
        municipio: campos.municipio.trim(),
        cep: campos.cep.trim(),
        territorio: campos.territorio.trim(),
      },
      membros: linhasParaMembros(linhas),
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success('Família referenciada ao CRAS com sucesso.', 'Sucesso');
        onReferenciada?.(resposta.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in ({ nis: 1, cpfResponsavel: 1, unidadeAtendimentoId: 1, territorio: 1, membros: 1 } as Record<string, number>)) {
              (mapped as Record<string, string>)[key] = messages[0];
            }
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível referenciar a família.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Referenciar família ao CRAS"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-referenciar-familia" loading={mutation.isPending}>
            Referenciar
          </Button>
        </>
      }
    >
      <form id="form-referenciar-familia" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="NIS do responsável" required error={errors.nis}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  inputMode="numeric"
                  value={campos.nis}
                  onChange={(e) => set('nis', e.target.value)}
                  placeholder="00000000000"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="CPF do responsável" required error={errors.cpfResponsavel}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  inputMode="numeric"
                  value={campos.cpfResponsavel}
                  onChange={(e) => set('cpfResponsavel', e.target.value)}
                  placeholder="000.000.000-00"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField
          label="Unidade de atendimento (CRAS)"
          required
          error={errors.unidadeAtendimentoId}
          help="Identificador do CRAS de referência."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={campos.unidadeAtendimentoId}
              onChange={(e) => set('unidadeAtendimentoId', e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold mb-2">Endereço territorializado</legend>
          <div className="row">
            <div className="col-12">
              <FormField label="Logradouro" required error={errors.logradouro}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={campos.logradouro}
                    onChange={(e) => set('logradouro', e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="Município" required error={errors.municipio}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={campos.municipio}
                    onChange={(e) => set('municipio', e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="CEP" required error={errors.cep}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    inputMode="numeric"
                    value={campos.cep}
                    onChange={(e) => set('cep', e.target.value)}
                    placeholder="00000-000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12">
              <FormField
                label="Território de cobertura do CRAS"
                required
                error={errors.territorio}
                help="O endereço deve estar dentro do território do CRAS informado."
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={campos.territorio}
                    onChange={(e) => set('territorio', e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </fieldset>

        <MembrosFieldset
          linhas={linhas}
          onChange={setLinhas}
          errosPorLinha={errosMembro}
          erroGeral={errors.membros}
          disabled={mutation.isPending}
        />
      </form>
    </Modal>
  );
}
