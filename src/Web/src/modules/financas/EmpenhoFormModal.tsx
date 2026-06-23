// Formulário de emissão de Empenho (POST /empenhos) em Modal. useMutation + validação
// por campo + mapeamento de ProblemDetails.fieldErrors + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { DotacaoPicker } from './DespesaPickers';
import { useEmpenhar } from './financas.api';
import type { EmpenharInput } from './financas.api';
import {
  TIPOS_EMPENHO,
  TIPOS_PESSOA,
  exercicioCorrente,
  hojeIso,
  mensagemErro,
  tratarErroCampos,
} from './financas.helpers';

export interface EmpenhoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o identificador da dotação a partir da consulta corrente. */
  dotacaoIdInicial?: string;
}

interface Campos {
  numero?: string;
  dotacaoId?: string;
  tipoEmpenho?: string;
  exercicio?: string;
  dataEmpenho?: string;
  credorNome?: string;
  credorTipo?: string;
  credorDocumento?: string;
  valor?: string;
}

const CONHECIDOS: Record<string, true> = {
  numero: true, dotacaoId: true, tipoEmpenho: true, exercicio: true, dataEmpenho: true,
  credorNome: true, credorTipo: true, credorDocumento: true, valor: true,
};

export function EmpenhoFormModal({ open, onClose, dotacaoIdInicial = '' }: EmpenhoFormModalProps) {
  const toast = useToast();
  const mutation = useEmpenhar();

  const [numero, setNumero] = useState('');
  const [dotacaoId, setDotacaoId] = useState(dotacaoIdInicial);
  const [tipo, setTipo] = useState('');
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [dataEmpenho, setDataEmpenho] = useState(hojeIso());
  const [credorNome, setCredorNome] = useState('');
  const [credorTipo, setCredorTipo] = useState('');
  const [credorDocumento, setCredorDocumento] = useState('');
  const [valor, setValor] = useState('');
  const [errors, setErrors] = useState<Campos>({});

  function validar(): Campos {
    const next: Campos = {};
    if (numero.trim() === '') next.numero = 'Informe o número do empenho.';
    if (dotacaoId.trim() === '') next.dotacaoId = 'Informe a dotação.';
    if (tipo === '') next.tipoEmpenho = 'Selecione o tipo de empenho.';
    const ex = Number(exercicio);
    if (!Number.isInteger(ex) || ex < 2000) next.exercicio = 'Informe um exercício válido.';
    if (dataEmpenho.trim() === '') next.dataEmpenho = 'Informe a data do empenho.';
    if (credorNome.trim() === '') next.credorNome = 'Informe o nome do credor.';
    if (credorTipo === '') next.credorTipo = 'Selecione a natureza do credor.';
    if (credorDocumento.trim() === '') next.credorDocumento = 'Informe o documento do credor.';
    const v = Number(valor);
    if (valor.trim() === '' || Number.isNaN(v) || v <= 0) next.valor = 'Informe um valor maior que zero.';
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

    const input: EmpenharInput = {
      numero: numero.trim(),
      dotacaoId: dotacaoId.trim(),
      tipoEmpenho: Number(tipo),
      exercicio: Number(exercicio),
      dataEmpenho,
      credorNome: credorNome.trim(),
      credorTipo: Number(credorTipo),
      credorDocumento: credorDocumento.trim(),
      valor: Number(valor),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Empenho ${input.numero} emitido.`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível emitir o empenho.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir empenho"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-emitir-empenho" loading={mutation.isPending}>
            Emitir
          </Button>
        </>
      }
    >
      <form id="form-emitir-empenho" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Número do empenho" required error={errors.numero}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={30}
              value={numero} onChange={(e) => setNumero(e.target.value)} placeholder="2026NE000123" />
          )}
        </FormField>
        <DotacaoPicker
          required
          error={errors.dotacaoId}
          value={dotacaoId}
          onChange={(novoId) => {
            setDotacaoId(novoId);
            if (errors.dotacaoId) setErrors((prev) => ({ ...prev, dotacaoId: undefined }));
          }}
        />
        <FormField label="Tipo de empenho" required error={errors.tipoEmpenho}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={tipo} onChange={(e) => setTipo(e.target.value)} placeholder="Selecione…" options={TIPOS_EMPENHO} />
          )}
        </FormField>
        <FormField label="Exercício" required error={errors.exercicio}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
              aria-describedby={describedBy} invalid={invalid}
              value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
          )}
        </FormField>
        <FormField label="Data do empenho" required error={errors.dataEmpenho}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
              value={dataEmpenho} onChange={(e) => setDataEmpenho(e.target.value)} />
          )}
        </FormField>
        <FormField label="Nome do credor" required error={errors.credorNome}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={200}
              value={credorNome} onChange={(e) => setCredorNome(e.target.value)} />
          )}
        </FormField>
        <FormField label="Natureza do credor" required error={errors.credorTipo}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={credorTipo} onChange={(e) => setCredorTipo(e.target.value)} placeholder="Selecione…" options={TIPOS_PESSOA} />
          )}
        </FormField>
        <FormField label="Documento do credor (CPF/CNPJ)" required error={errors.credorDocumento}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={20}
              value={credorDocumento} onChange={(e) => setCredorDocumento(e.target.value)} />
          )}
        </FormField>
        <FormField label="Valor (R$)" required error={errors.valor}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="0" step="0.01" inputMode="decimal"
              aria-describedby={describedBy} invalid={invalid}
              value={valor} onChange={(e) => setValor(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
