// Formulário de AUTUAÇÃO de processo (command AutuarProcesso) em Modal (foco preso).
// Espelha AutuarProcessoValidator: Classificacao NotEmpty + MaximumLength(60);
// NivelAcesso IsInEnum; OrigemId obrigatório quando há OrigemModulo (I-12).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import type { SelectOption } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { NIVEL_ACESSO_VALOR, useAutuarProcesso } from './processo.api';
import type { AutuarProcessoInput, NivelDeAcesso } from './processo.api';
import { NIVEL_ACESSO_LABEL } from './processo.helpers';

const CLASSIFICACAO_MAX = 60;

const NIVEL_ACESSO_OPCOES: SelectOption[] = (
  ['Publico', 'Restrito', 'Sigiloso'] as NivelDeAcesso[]
).map((n) => ({ value: n, label: NIVEL_ACESSO_LABEL[n] }));

export interface ProcessoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Setor cuja lista deve ser invalidada após a autuação (consulta ativa). */
  setorIdParaInvalidar?: string;
}

interface FormErrors {
  classificacao?: string;
  nivelAcesso?: string;
  requerimentoId?: string;
  origemModulo?: string;
  origemId?: string;
}

export function ProcessoFormModal({ open, onClose, setorIdParaInvalidar }: ProcessoFormModalProps) {
  const toast = useToast();
  const mutation = useAutuarProcesso(setorIdParaInvalidar);

  const [classificacao, setClassificacao] = useState('');
  const [nivelAcesso, setNivelAcesso] = useState<NivelDeAcesso | ''>('');
  const [requerimentoId, setRequerimentoId] = useState('');
  const [origemModulo, setOrigemModulo] = useState('');
  const [origemId, setOrigemId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const classe = classificacao.trim();
    if (classe === '') next.classificacao = 'Informe a classificação documental.';
    else if (classe.length > CLASSIFICACAO_MAX)
      next.classificacao = `A classificação deve ter no máximo ${CLASSIFICACAO_MAX} caracteres.`;
    if (nivelAcesso === '') next.nivelAcesso = 'Selecione o nível de acesso.';
    // I-12: origem obrigatória quando há módulo originador.
    if (origemModulo.trim() !== '' && origemId.trim() === '')
      next.origemId = 'Informe o identificador da origem quando houver módulo originador.';
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
    if (nivelAcesso === '') return;

    const input: AutuarProcessoInput = {
      requerimentoId: requerimentoId.trim() || null,
      classificacao: classificacao.trim(),
      nivelAcesso: NIVEL_ACESSO_VALOR[nivelAcesso],
      origemModulo: origemModulo.trim() || null,
      origemId: origemId.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success(`Processo autuado (id ${resultado.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          const conhecidos: Record<string, number> = {
            classificacao: 1,
            nivelAcesso: 1,
            requerimentoId: 1,
            origemModulo: 1,
            origemId: 1,
          };
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in conhecidos) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível autuar o processo.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Autuar processo administrativo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-autuar-processo" loading={mutation.isPending}>
            Autuar
          </Button>
        </>
      }
    >
      <form id="form-autuar-processo" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Classificação documental"
          required
          error={errors.classificacao}
          help={`Vincula à Tabela de Temporalidade (CONARQ). Máx. ${CLASSIFICACAO_MAX} caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={CLASSIFICACAO_MAX}
              value={classificacao}
              onChange={(e) => setClassificacao(e.target.value)}
              placeholder="Ex.: 023.1 — Pessoal/Férias"
            />
          )}
        </FormField>

        <FormField label="Nível de acesso" required error={errors.nivelAcesso}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={NIVEL_ACESSO_OPCOES}
              value={nivelAcesso}
              onChange={(e) => setNivelAcesso(e.target.value as NivelDeAcesso | '')}
            />
          )}
        </FormField>

        <FormField label="Requerimento de origem" help="Opcional — identificador do requerimento que originou o processo.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={requerimentoId}
              onChange={(e) => setRequerimentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Módulo originador" help="Opcional — preencha quando autuado por outro módulo (ex.: Licitacao, RH).">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={origemModulo}
              onChange={(e) => setOrigemModulo(e.target.value)}
              placeholder="Licitacao, RH, Licencas…"
            />
          )}
        </FormField>

        <FormField
          label="Identificador da origem"
          required={origemModulo.trim() !== ''}
          error={errors.origemId}
          help="Obrigatório quando há módulo originador (I-12)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={origemId}
              onChange={(e) => setOrigemId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
