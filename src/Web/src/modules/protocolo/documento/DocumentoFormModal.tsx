// Formulario de JUNTADA de Documento em Modal (Command 5.1 — JuntarDocumento).
// Padrao-ouro: useMutation + validacao por campo (FormField/aria-describedby) + Toast.
// Reflete o validator do dominio: ProcessoId NotEmpty; Hash 64 hex; Criticidade/NivelAcesso
// IsInEnum; FormatoPdfA == true (I-1, PDF/A obrigatorio).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import {
  CriticidadeValor,
  NivelAcessoValor,
  useJuntarDocumento,
} from './documento.api';
import type { CriticidadeAto, JuntarDocumentoInput, NivelDeAcesso } from './documento.api';
import { OPCOES_CRITICIDADE, OPCOES_NIVEL_ACESSO } from './documento.helpers';

export interface DocumentoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Processo ao qual o documento sera juntado (pre-preenchido a partir da consulta). */
  processoIdInicial?: string;
}

interface FormErrors {
  processoId?: string;
  hash?: string;
  criticidade?: string;
  nivelAcesso?: string;
  formatoPdfA?: string;
}

const HASH_SHA256 = /^[0-9a-fA-F]{64}$/;

export function DocumentoFormModal({ open, onClose, processoIdInicial = '' }: DocumentoFormModalProps) {
  const toast = useToast();
  const mutation = useJuntarDocumento();

  const [processoId, setProcessoId] = useState(processoIdInicial);
  const [hash, setHash] = useState('');
  const [criticidade, setCriticidade] = useState<CriticidadeAto | ''>('');
  const [nivelAcesso, setNivelAcesso] = useState<NivelDeAcesso | ''>('');
  const [formatoPdfA, setFormatoPdfA] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (processoId.trim() === '') next.processoId = 'Informe o identificador do processo de destino.';
    if (!HASH_SHA256.test(hash.trim()))
      next.hash = 'Informe o hash SHA-256 (64 caracteres hexadecimais).';
    if (criticidade === '') next.criticidade = 'Selecione a criticidade do ato.';
    if (nivelAcesso === '') next.nivelAcesso = 'Selecione o nivel de acesso.';
    if (!formatoPdfA) next.formatoPdfA = 'O documento deve estar em PDF/A (e-ARQ Brasil).';
    return next;
  }

  function limpar(): void {
    setProcessoId(processoIdInicial);
    setHash('');
    setCriticidade('');
    setNivelAcesso('');
    setFormatoPdfA(false);
    setErrors({});
  }

  function fechar(): void {
    limpar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: JuntarDocumentoInput = {
      processoId: processoId.trim(),
      hash: hash.trim().toLowerCase(),
      criticidade: CriticidadeValor[criticidade as CriticidadeAto],
      nivelAcesso: NivelAcessoValor[nivelAcesso as NivelDeAcesso],
      formatoPdfA,
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success(`Documento juntado ao processo (id ${resposta.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in ({ processoId: 1, hash: 1, criticidade: 1, nivelAcesso: 1, formatoPdfA: 1 } as Record<string, number>)) {
              (mapped as Record<string, string>)[key] = messages[0];
            }
          }
          setErrors((prev) => ({ ...prev, ...mapped }));
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Nao foi possivel juntar o documento.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Juntar documento ao processo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-juntar-documento" loading={mutation.isPending}>
            Juntar
          </Button>
        </>
      }
    >
      <form id="form-juntar-documento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A juntada torna o documento <strong>imutavel</strong> e parte da trilha documental (Lei 11.419/2006).
          Documentos juntados nunca sao excluidos — apenas tornados sem efeito.
        </Alert>

        <FormField label="Identificador do processo de destino" required error={errors.processoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={processoId}
              onChange={(e) => setProcessoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField
          label="Hash SHA-256 do conteudo"
          required
          error={errors.hash}
          help="64 caracteres hexadecimais (integridade do documento)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={hash}
              onChange={(e) => setHash(e.target.value)}
              spellCheck={false}
              autoComplete="off"
              placeholder="e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
            />
          )}
        </FormField>

        <FormField label="Criticidade do ato" required error={errors.criticidade}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_CRITICIDADE}
              placeholder="Selecione a criticidade"
              value={criticidade}
              onChange={(e) => setCriticidade(e.target.value as CriticidadeAto | '')}
            />
          )}
        </FormField>

        <FormField label="Nivel de acesso" required error={errors.nivelAcesso}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_NIVEL_ACESSO}
              placeholder="Selecione o nivel de acesso"
              value={nivelAcesso}
              onChange={(e) => setNivelAcesso(e.target.value as NivelDeAcesso | '')}
            />
          )}
        </FormField>

        <FormField label="Conformidade arquivistica" required error={errors.formatoPdfA}>
          {({ id, describedBy, invalid }) => (
            <div className="br-checkbox">
              <input
                id={id}
                type="checkbox"
                aria-describedby={describedBy}
                aria-invalid={invalid || undefined}
                checked={formatoPdfA}
                onChange={(e) => setFormatoPdfA(e.target.checked)}
              />
              <label htmlFor={id}>Confirmo que o documento esta em PDF/A (e-ARQ Brasil).</label>
            </div>
          )}
        </FormField>
      </form>
    </Modal>
  );
}
