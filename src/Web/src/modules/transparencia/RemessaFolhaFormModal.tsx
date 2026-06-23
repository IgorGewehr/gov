// Formulário de geração da REMESSA DE FOLHA ao TCE-RS (Resolução 1099/2018) em Modal.
// Contrato: exercício + mês (competência mensal) + versão do leiaute (default "1099").
// A folha REUSA o agregado RemessaTce: a remessa gerada cai na MESMA lista e abre na
// MESMA tela de detalhe, percorrendo validar → empacotar → baixar → registrar protocolo.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useGerarRemessaFolha } from './api';
import type { GerarRemessaFolhaInput } from './api';
import { leiauteFolhaVersaoPadrao, mesOptions } from './transparencia.helpers';

export interface RemessaFolhaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o exercício a partir do filtro corrente. */
  exercicioInicial: number;
}

interface FormErrors {
  exercicio?: string;
  mes?: string;
  leiauteVersao?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = { exercicio: true, mes: true, leiauteVersao: true };

export function RemessaFolhaFormModal({ open, onClose, exercicioInicial }: RemessaFolhaFormModalProps) {
  const toast = useToast();
  const mutation = useGerarRemessaFolha();

  const [exercicio, setExercicio] = useState(String(exercicioInicial));
  const [mes, setMes] = useState('1');
  const [leiauteVersao, setLeiauteVersao] = useState(leiauteFolhaVersaoPadrao);
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const ano = Number(exercicio);
    if (exercicio.trim() === '' || !Number.isInteger(ano) || ano < 1900)
      next.exercicio = 'Informe um exercício válido (>= 1900).';
    const numeroMes = Number(mes);
    if (!Number.isInteger(numeroMes) || numeroMes < 1 || numeroMes > 12)
      next.mes = 'Selecione um mês válido (1 a 12).';
    if (leiauteVersao.trim() === '') next.leiauteVersao = 'Informe a versão do leiaute.';
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

    const input: GerarRemessaFolhaInput = {
      exercicio: Number(exercicio),
      mes: Number(mes),
      leiauteVersao: leiauteVersao.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Remessa de folha gerada e pronta para validação.', 'Sucesso');
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
          error instanceof ApiError ? error.userMessage : 'Não foi possível gerar a remessa de folha.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar remessa de folha (TCE-RS)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-gerar-remessa-folha" loading={mutation.isPending}>
            Gerar
          </Button>
        </>
      }
    >
      <Alert variant="info" title="Remessa de folha — Resolução 1099/2018">
        Monta os arquivos posicionais da folha (TCE_4810/4820/4960) a partir da folha do RH já
        fechada na competência. A folha tem periodicidade mensal e segue o mesmo ciclo das demais
        remessas: validar, empacotar, baixar e registrar o protocolo.
      </Alert>

      <form id="form-gerar-remessa-folha" className="br-form mt-3" onSubmit={submeter} noValidate>
        <FormField label="Exercício" required error={errors.exercicio}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1900"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={exercicio}
              onChange={(e) => setExercicio(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Mês (competência)" required help="A folha é sempre mensal." error={errors.mes}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={mesOptions}
              value={mes}
              onChange={(e) => setMes(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Versão do leiaute"
          required
          help="Versão do leiaute de folha do TCE-RS (Res. 1099)."
          error={errors.leiauteVersao}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={leiauteVersao}
              onChange={(e) => setLeiauteVersao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
