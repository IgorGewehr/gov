// Formulário de ABERTURA de edital de credenciamento (AbrirCredenciamentoCommand) em Modal.
// Mutation + validação por campo (FormField/aria-describedby) + Toast.
// O edital nasce EmElaboracao (rascunho); itens credenciáveis são incluídos no detalhe.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirCredenciamento } from './credenciamento.api';
import type { AbrirCredenciamentoInput, HipoteseCredenciamento } from './credenciamento.api';
import { HIPOTESE_OPTIONS } from './credenciamento.helpers';

export interface CredenciamentoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  objeto?: string;
  hipotese?: string;
  vigenciaInicio?: string;
  vigenciaFim?: string;
  fundamentacaoLegal?: string;
}

const CAMPOS: ReadonlySet<string> = new Set([
  'objeto',
  'hipotese',
  'vigenciaInicio',
  'vigenciaFim',
  'fundamentacaoLegal',
]);

export function CredenciamentoFormModal({ open, onClose }: CredenciamentoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useAbrirCredenciamento();

  const [objeto, setObjeto] = useState('');
  const [hipotese, setHipotese] = useState<HipoteseCredenciamento | ''>('');
  const [vigenciaInicio, setVigenciaInicio] = useState('');
  const [vigenciaFim, setVigenciaFim] = useState('');
  const [fundamentacaoLegal, setFundamentacaoLegal] = useState('Art. 74, IV c/c art. 79, Lei 14.133/2021');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (objeto.trim() === '') next.objeto = 'Objeto é obrigatório.';
    else if (objeto.trim().length > 2000) next.objeto = 'Objeto deve ter no máximo 2000 caracteres.';
    if (hipotese === '') next.hipotese = 'Selecione a hipótese autorizadora.';
    if (vigenciaInicio === '') next.vigenciaInicio = 'Informe o início da vigência.';
    if (vigenciaFim === '') next.vigenciaFim = 'Informe o fim da vigência.';
    else if (vigenciaInicio !== '' && vigenciaFim < vigenciaInicio)
      next.vigenciaFim = 'A vigência final não pode ser anterior à inicial.';
    if (fundamentacaoLegal.trim() === '') next.fundamentacaoLegal = 'Informe a fundamentação legal.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setObjeto('');
    setHipotese('');
    setVigenciaInicio('');
    setVigenciaFim('');
    setFundamentacaoLegal('Art. 74, IV c/c art. 79, Lei 14.133/2021');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: AbrirCredenciamentoInput = {
      objeto: objeto.trim(),
      hipotese: hipotese as HipoteseCredenciamento,
      vigenciaInicio,
      vigenciaFim,
      fundamentacaoLegal: fundamentacaoLegal.trim(),
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success('Credenciamento aberto com sucesso.', 'Sucesso');
        fechar();
        if (resultado?.id) navigate(`/administracao/credenciamentos/${resultado.id}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (CAMPOS.has(key)) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o credenciamento.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir edital de credenciamento"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-credenciamento" loading={mutation.isPending}>
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-abrir-credenciamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Objeto"
          required
          error={errors.objeto}
          help="Descrição do objeto do credenciamento (máx. 2000 caracteres)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={2000}
              value={objeto}
              onChange={(e) => setObjeto(e.target.value)}
              placeholder="Ex.: Credenciamento de laboratórios para exames de análises clínicas"
            />
          )}
        </FormField>

        <FormField
          label="Hipótese autorizadora"
          required
          error={errors.hipotese}
          help="Hipótese do credenciamento (Lei 14.133/2021, art. 79, I a III)."
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={HIPOTESE_OPTIONS}
              value={hipotese}
              onChange={(e) => setHipotese(e.target.value as HipoteseCredenciamento)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Início da vigência" required error={errors.vigenciaInicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vigenciaInicio}
                  onChange={(e) => setVigenciaInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Fim da vigência" required error={errors.vigenciaFim}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vigenciaFim}
                  onChange={(e) => setVigenciaFim(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField
          label="Fundamentação legal"
          required
          error={errors.fundamentacaoLegal}
          help="Amparo legal do credenciamento (inexigibilidade — art. 74, IV c/c art. 79)."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={500}
              rows={2}
              value={fundamentacaoLegal}
              onChange={(e) => setFundamentacaoLegal(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
