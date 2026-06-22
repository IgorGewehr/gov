// Formulário de criação de Dotação Orçamentária (POST /dotacoes) em Modal.
// useMutation + validação por campo (FormField/aria-describedby) + mapeamento de
// ProblemDetails.fieldErrors + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { useCriarDotacao } from './financas.api';
import type { CriarDotacaoInput } from './financas.api';
import {
  CATEGORIAS_ECONOMICAS,
  exercicioCorrente,
  mensagemErro,
  tratarErroCampos,
} from './financas.helpers';

export interface DotacaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface Campos {
  exercicio?: string;
  orgao?: string;
  unidade?: string;
  funcionalProgramatica?: string;
  categoriaEconomica?: string;
  fonteRecurso?: string;
  valorDotado?: string;
}

const CONHECIDOS: Record<string, true> = {
  exercicio: true,
  orgao: true,
  unidade: true,
  funcionalProgramatica: true,
  categoriaEconomica: true,
  fonteRecurso: true,
  valorDotado: true,
};

export function DotacaoFormModal({ open, onClose }: DotacaoFormModalProps) {
  const toast = useToast();
  const mutation = useCriarDotacao();

  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [orgao, setOrgao] = useState('');
  const [unidade, setUnidade] = useState('');
  const [funcional, setFuncional] = useState('');
  const [categoria, setCategoria] = useState('');
  const [fonte, setFonte] = useState('');
  const [valor, setValor] = useState('');
  const [errors, setErrors] = useState<Campos>({});

  function validar(): Campos {
    const next: Campos = {};
    const ex = Number(exercicio);
    if (!Number.isInteger(ex) || ex < 2000) next.exercicio = 'Informe um exercício válido.';
    if (orgao.trim() === '') next.orgao = 'Informe o órgão.';
    if (unidade.trim() === '') next.unidade = 'Informe a unidade.';
    if (funcional.trim() === '') next.funcionalProgramatica = 'Informe a funcional programática.';
    if (categoria === '') next.categoriaEconomica = 'Selecione a categoria econômica.';
    if (fonte.trim() === '') next.fonteRecurso = 'Informe a fonte de recurso.';
    const v = Number(valor);
    if (valor.trim() === '' || Number.isNaN(v) || v <= 0) next.valorDotado = 'Informe um valor maior que zero.';
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

    const input: CriarDotacaoInput = {
      exercicio: Number(exercicio),
      orgao: orgao.trim(),
      unidade: unidade.trim(),
      funcionalProgramatica: funcional.trim(),
      categoriaEconomica: Number(categoria),
      fonteRecurso: fonte.trim(),
      valorDotado: Number(valor),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Dotação criada.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível criar a dotação.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Criar dotação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-criar-dotacao" loading={mutation.isPending}>
            Criar
          </Button>
        </>
      }
    >
      <form id="form-criar-dotacao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Exercício" required error={errors.exercicio}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
              aria-describedby={describedBy} invalid={invalid}
              value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
          )}
        </FormField>
        <FormField label="Órgão" required error={errors.orgao}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={120}
              value={orgao} onChange={(e) => setOrgao(e.target.value)} placeholder="Ex.: Secretaria de Saúde" />
          )}
        </FormField>
        <FormField label="Unidade orçamentária" required error={errors.unidade}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={120}
              value={unidade} onChange={(e) => setUnidade(e.target.value)} placeholder="Ex.: Fundo Municipal de Saúde" />
          )}
        </FormField>
        <FormField label="Funcional programática" required error={errors.funcionalProgramatica}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={60}
              value={funcional} onChange={(e) => setFuncional(e.target.value)} placeholder="Ex.: 10.301.0002.2010" />
          )}
        </FormField>
        <FormField label="Categoria econômica" required error={errors.categoriaEconomica}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={categoria} onChange={(e) => setCategoria(e.target.value)}
              placeholder="Selecione…" options={CATEGORIAS_ECONOMICAS} />
          )}
        </FormField>
        <FormField label="Fonte de recurso" required error={errors.fonteRecurso}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={60}
              value={fonte} onChange={(e) => setFonte(e.target.value)} placeholder="Ex.: 1500 — Recursos próprios" />
          )}
        </FormField>
        <FormField label="Valor dotado (R$)" required error={errors.valorDotado}>
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
