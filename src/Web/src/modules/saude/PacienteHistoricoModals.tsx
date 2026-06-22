// Modais de registro no HISTÓRICO CLÍNICO do paciente (dado sensível — LGPD art. 11):
// condição de saúde (CID-10/CIAP-2) e alergia. WIRED a mutations TanStack Query, com
// validação por campo e Toast. Ações gated por "saude.gerenciar" na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useRegistrarAlergia, useRegistrarCondicao } from './api';
import { mapearErros } from './saude.formErrors';

interface AcaoPacienteProps {
  open: boolean;
  onClose: () => void;
  pacienteId: string;
}

// --- REGISTRAR CONDIÇÃO DE SAÚDE ----------------------------------------------

export function RegistrarCondicaoModal({ open, onClose, pacienteId }: AcaoPacienteProps) {
  const toast = useToast();
  const mutation = useRegistrarCondicao(pacienteId);
  const [codigo, setCodigo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [errors, setErrors] = useState<{ codigo?: string; descricao?: string }>({});

  function fechar(): void {
    setErrors({});
    setCodigo('');
    setDescricao('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { codigo?: string; descricao?: string } = {};
    if (codigo.trim() === '') next.codigo = 'Informe o código (CID-10/CIAP-2).';
    if (descricao.trim() === '') next.descricao = 'Informe a descrição da condição.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { codigo: codigo.trim(), descricao: descricao.trim() },
      {
        onSuccess: () => {
          toast.success('Condição registrada no histórico.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(mapearErros(error, { codigo: 1, descricao: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a condição.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar condição de saúde"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-condicao" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-condicao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Código (CID-10/CIAP-2)" required error={errors.codigo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value)}
              placeholder="Ex.: E11 — Diabetes mellitus tipo 2"
            />
          )}
        </FormField>
        <FormField label="Descrição" required error={errors.descricao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// --- REGISTRAR ALERGIA --------------------------------------------------------

export function RegistrarAlergiaModal({ open, onClose, pacienteId }: AcaoPacienteProps) {
  const toast = useToast();
  const mutation = useRegistrarAlergia(pacienteId);
  const [substancia, setSubstancia] = useState('');
  const [gravidade, setGravidade] = useState('');
  const [errors, setErrors] = useState<{ substancia?: string; gravidade?: string }>({});

  function fechar(): void {
    setErrors({});
    setSubstancia('');
    setGravidade('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { substancia?: string; gravidade?: string } = {};
    if (substancia.trim() === '') next.substancia = 'Informe a substância/agente.';
    if (gravidade.trim() === '') next.gravidade = 'Informe a gravidade.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { substancia: substancia.trim(), gravidade: gravidade.trim() },
      {
        onSuccess: () => {
          toast.success('Alergia registrada no histórico.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(mapearErros(error, { substancia: 1, gravidade: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a alergia.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar alergia"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-alergia" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-alergia" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Substância / agente" required error={errors.substancia}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={substancia}
              onChange={(e) => setSubstancia(e.target.value)}
              placeholder="Ex.: Penicilina"
            />
          )}
        </FormField>
        <FormField label="Gravidade" required error={errors.gravidade}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={gravidade}
              onChange={(e) => setGravidade(e.target.value)}
              placeholder="Ex.: Grave (anafilaxia)"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
