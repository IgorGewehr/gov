// Formulário de ABERTURA de dispensa eletrônica (AbrirDispensaCommand) em Modal.
// Mutation + validação por campo (FormField/aria-describedby) + Toast.
// A dispensa nasce Aberta (rascunho); o valor total deriva dos itens (incluídos no detalhe).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirDispensa } from './dispensa.api';
import type { AbrirDispensaInput, CriterioJulgamentoDispensa, FundamentoDispensaValor } from './dispensa.api';
import { CRITERIO_OPTIONS, FUNDAMENTO_OPTIONS } from './dispensa.helpers';

export interface DispensaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  objeto?: string;
  fundamento?: string;
  criterioJulgamento?: string;
  etpId?: string;
  termoReferenciaId?: string;
}

const CAMPOS: ReadonlySet<string> = new Set([
  'objeto',
  'fundamento',
  'criterioJulgamento',
  'etpId',
  'termoReferenciaId',
]);

const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function DispensaFormModal({ open, onClose }: DispensaFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useAbrirDispensa();

  const [objeto, setObjeto] = useState('');
  const [fundamento, setFundamento] = useState<FundamentoDispensaValor | ''>('');
  const [criterioJulgamento, setCriterioJulgamento] = useState<CriterioJulgamentoDispensa | ''>('');
  const [etpId, setEtpId] = useState('');
  const [termoReferenciaId, setTermoReferenciaId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (objeto.trim() === '') next.objeto = 'Objeto é obrigatório.';
    else if (objeto.trim().length > 500) next.objeto = 'Objeto deve ter no máximo 500 caracteres.';
    if (fundamento === '') next.fundamento = 'Selecione o fundamento legal.';
    if (criterioJulgamento === '') next.criterioJulgamento = 'Selecione o critério de julgamento.';
    if (etpId.trim() !== '' && !GUID_REGEX.test(etpId.trim()))
      next.etpId = 'Informe um identificador (GUID) válido.';
    if (termoReferenciaId.trim() !== '' && !GUID_REGEX.test(termoReferenciaId.trim()))
      next.termoReferenciaId = 'Informe um identificador (GUID) válido.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setObjeto('');
    setFundamento('');
    setCriterioJulgamento('');
    setEtpId('');
    setTermoReferenciaId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: AbrirDispensaInput = {
      objeto: objeto.trim(),
      fundamento: fundamento as FundamentoDispensaValor,
      criterioJulgamento: criterioJulgamento as CriterioJulgamentoDispensa,
      etpId: etpId.trim() || null,
      termoReferenciaId: termoReferenciaId.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success('Dispensa aberta com sucesso.', 'Sucesso');
        fechar();
        if (resultado?.id) navigate(`/administracao/dispensas/${resultado.id}`);
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
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a dispensa.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir dispensa eletrônica"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-dispensa" loading={mutation.isPending}>
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-abrir-dispensa" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Objeto"
          required
          error={errors.objeto}
          help="Descrição do objeto da contratação direta (máx. 500 caracteres)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={500}
              value={objeto}
              onChange={(e) => setObjeto(e.target.value)}
              placeholder="Ex.: Aquisição de material de expediente"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-6">
            <FormField
              label="Fundamento legal"
              required
              error={errors.fundamento}
              help="Hipótese de dispensa em razão do valor (Lei 14.133/2021, art. 75)."
            >
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  placeholder="Selecione…"
                  options={FUNDAMENTO_OPTIONS}
                  value={fundamento}
                  onChange={(e) => setFundamento(e.target.value as FundamentoDispensaValor)}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Critério de julgamento" required error={errors.criterioJulgamento}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  placeholder="Selecione…"
                  options={CRITERIO_OPTIONS}
                  value={criterioJulgamento}
                  onChange={(e) => setCriterioJulgamento(e.target.value as CriterioJulgamentoDispensa)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <FormField label="ETP (Estudo Técnico Preliminar)" error={errors.etpId} help="Opcional — identificador do ETP.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={etpId}
                  onChange={(e) => setEtpId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField
              label="TR (Termo de Referência)"
              error={errors.termoReferenciaId}
              help="Opcional — identificador do TR."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={termoReferenciaId}
                  onChange={(e) => setTermoReferenciaId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
