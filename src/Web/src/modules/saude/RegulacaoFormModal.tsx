// Formulário de abertura de Solicitação de Regulação (SIGTAP) em Modal. Padrão-ouro:
// mutation + validação por campo + Toast e mapeamento de erros do backend.
// Pode ser aberto a partir da fila (sem paciente) ou do prontuário (paciente fixo).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useSolicitarRegulacao } from './api';
import type { SolicitarRegulacaoInput } from './api';
import { opcoesPrioridade, paraPrioridade } from './saude.helpers';

export interface RegulacaoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando aberto a partir de um prontuário, fixa o paciente. */
  pacienteId?: string;
}

interface FormErrors {
  pacienteId?: string;
  estabelecimentoSolicitanteId?: string;
  profissionalSolicitanteId?: string;
  codigoSigtap?: string;
  descricaoProcedimento?: string;
  justificativa?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  pacienteId: true,
  estabelecimentoSolicitanteId: true,
  profissionalSolicitanteId: true,
  codigoSigtap: true,
  descricaoProcedimento: true,
  justificativa: true,
};

export function RegulacaoFormModal({ open, onClose, pacienteId }: RegulacaoFormModalProps) {
  const toast = useToast();
  const mutation = useSolicitarRegulacao();
  const pacienteFixo = typeof pacienteId === 'string' && pacienteId.trim() !== '';

  const [pacienteInformado, setPacienteInformado] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [codigoSigtap, setCodigoSigtap] = useState('');
  const [descricao, setDescricao] = useState('');
  const [prioridade, setPrioridade] = useState('1');
  const [justificativa, setJustificativa] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const pacienteAtual = pacienteFixo ? (pacienteId as string) : pacienteInformado;

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (pacienteAtual.trim() === '') next.pacienteId = 'Informe o paciente.';
    if (estabelecimentoId.trim() === '')
      next.estabelecimentoSolicitanteId = 'Informe o estabelecimento solicitante.';
    if (profissionalId.trim() === '')
      next.profissionalSolicitanteId = 'Informe o profissional solicitante.';
    if (codigoSigtap.trim() === '') next.codigoSigtap = 'Informe o código SIGTAP.';
    if (descricao.trim() === '') next.descricaoProcedimento = 'Informe a descrição do procedimento.';
    if (justificativa.trim() === '') next.justificativa = 'Informe a justificativa clínica.';
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

    const input: SolicitarRegulacaoInput = {
      pacienteId: pacienteAtual.trim(),
      estabelecimentoSolicitanteId: estabelecimentoId.trim(),
      profissionalSolicitanteId: profissionalId.trim(),
      codigoSigtap: codigoSigtap.trim(),
      descricaoProcedimento: descricao.trim(),
      prioridade: paraPrioridade(prioridade),
      justificativa: justificativa.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Solicitação de regulação aberta com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a solicitação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Solicitar regulação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-solicitar-regulacao" loading={mutation.isPending}>
            Solicitar
          </Button>
        </>
      }
    >
      <form id="form-solicitar-regulacao" className="br-form" onSubmit={submeter} noValidate>
        {!pacienteFixo && (
          <FormField label="Identificador do paciente" required error={errors.pacienteId}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={pacienteInformado}
                onChange={(e) => setPacienteInformado(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
            )}
          </FormField>
        )}

        <FormField label="Estabelecimento solicitante (CNES)" required error={errors.estabelecimentoSolicitanteId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={estabelecimentoId}
              onChange={(e) => setEstabelecimentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Profissional solicitante" required error={errors.profissionalSolicitanteId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={profissionalId}
              onChange={(e) => setProfissionalId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-5">
            <FormField label="Código SIGTAP" required error={errors.codigoSigtap}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  maxLength={10}
                  value={codigoSigtap}
                  onChange={(e) => setCodigoSigtap(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-7">
            <FormField label="Prioridade" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesPrioridade}
                  value={prioridade}
                  onChange={(e) => setPrioridade(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Descrição do procedimento" required error={errors.descricaoProcedimento}>
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

        <FormField label="Justificativa clínica" required error={errors.justificativa}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={2000}
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
