// Formulário de INCORPORAÇÃO de bem patrimonial em Modal (foco preso).
// Command coberto: IncorporarBem (POST /patrimonio/bens).
// Padrão-ouro: useMutation + validação por campo (FormField/aria-describedby)
// + mapeamento de ProblemDetails.errors + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useIncorporarBem } from './bempatrimonial.api';
import type { IncorporarBemInput } from './bempatrimonial.api';
import { TIPO_BEM_OPCOES, tipoBemNumero } from './bemPatrimonial.helpers';

export interface BemPatrimonialFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Navega para o detalhe do bem recém-criado quando true (padrão). */
  navegarAposCriar?: boolean;
}

interface FormErrors {
  descricao?: string;
  tipo?: string;
  valorInicial?: string;
  valorResidual?: string;
  vidaUtilMeses?: string;
  dataIncorporacao?: string;
  origem?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  descricao: true,
  tipo: true,
  valorInicial: true,
  valorResidual: true,
  vidaUtilMeses: true,
  dataIncorporacao: true,
  origem: true,
};

const hoje = (): string => new Date().toISOString().slice(0, 10);

export function BemPatrimonialFormModal({ open, onClose, navegarAposCriar = true }: BemPatrimonialFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useIncorporarBem();

  const [descricao, setDescricao] = useState('');
  const [tipo, setTipo] = useState('1');
  const [valorInicial, setValorInicial] = useState('');
  const [valorResidual, setValorResidual] = useState('');
  const [vidaUtilMeses, setVidaUtilMeses] = useState('');
  const [dataIncorporacao, setDataIncorporacao] = useState(hoje());
  const [origem, setOrigem] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (descricao.trim() === '') next.descricao = 'Informe a descrição do bem.';
    else if (descricao.trim().length > 200) next.descricao = 'Máximo de 200 caracteres.';

    const inicial = Number(valorInicial);
    if (valorInicial.trim() === '' || Number.isNaN(inicial) || inicial <= 0)
      next.valorInicial = 'Informe um valor inicial maior que zero.';

    const residual = Number(valorResidual);
    if (valorResidual.trim() === '' || Number.isNaN(residual) || residual < 0)
      next.valorResidual = 'Informe um valor residual válido (≥ 0).';
    else if (!Number.isNaN(inicial) && residual > inicial)
      next.valorResidual = 'O valor residual não pode exceder o valor inicial.';

    const vida = Number(vidaUtilMeses);
    if (vidaUtilMeses.trim() === '' || !Number.isInteger(vida) || vida <= 0)
      next.vidaUtilMeses = 'Informe a vida útil em meses (inteiro positivo).';

    if (dataIncorporacao.trim() === '') next.dataIncorporacao = 'Informe a data de incorporação.';
    if (origem.trim() === '') next.origem = 'Informe a origem (aquisição, doação, produção própria).';
    return next;
  }

  function resetar(): void {
    setDescricao('');
    setTipo('1');
    setValorInicial('');
    setValorResidual('');
    setVidaUtilMeses('');
    setDataIncorporacao(hoje());
    setOrigem('');
    setErrors({});
  }

  function fechar(): void {
    resetar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: IncorporarBemInput = {
      descricao: descricao.trim(),
      tipo: tipoBemNumero(tipo),
      valorInicial: Number(valorInicial),
      valorResidual: Number(valorResidual),
      vidaUtilMeses: Number(vidaUtilMeses),
      dataIncorporacao,
      origem: origem.trim(),
    };

    mutation.mutate(input, {
      onSuccess: (criado) => {
        toast.success('Bem incorporado ao acervo (em incorporação).', 'Sucesso');
        const novoId = criado.id;
        fechar();
        if (navegarAposCriar && novoId) navigate(`/patrimonio/bens/${novoId}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível incorporar o bem.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Incorporar bem patrimonial"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-incorporar-bem" loading={mutation.isPending}>
            Incorporar
          </Button>
        </>
      }
    >
      <form id="form-incorporar-bem" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Descrição" required error={errors.descricao} help="Máximo de 200 caracteres.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              placeholder="Ex.: Notebook Dell Latitude 5440"
            />
          )}
        </FormField>

        <FormField label="Tipo do bem" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={TIPO_BEM_OPCOES}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Valor inicial (R$)" required error={errors.valorInicial}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valorInicial}
                  onChange={(e) => setValorInicial(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField
              label="Valor residual (R$)"
              required
              error={errors.valorResidual}
              help="Não pode exceder o valor inicial."
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
                  value={valorResidual}
                  onChange={(e) => setValorResidual(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Vida útil (meses)" required error={errors.vidaUtilMeses}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vidaUtilMeses}
                  onChange={(e) => setVidaUtilMeses(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de incorporação" required error={errors.dataIncorporacao}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataIncorporacao}
                  onChange={(e) => setDataIncorporacao(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField
          label="Origem"
          required
          error={errors.origem}
          help="Forma de ingresso ao acervo."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              rows={2}
              value={origem}
              onChange={(e) => setOrigem(e.target.value)}
              placeholder="Aquisição (Contrato nº…), doação, produção própria…"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
