// Formulário de ABERTURA de licitação (AbrirLicitacaoCommand) em Modal.
// Mutation + validação por campo (FormField/aria-describedby) + Toast.
// Espelha as regras: I-1 (objeto), I-2 (valor > 0), I-5 (Pregão × critério).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirLicitacao } from './licitacao.api';
import type { AbrirLicitacaoInput, CriterioJulgamento, ModalidadeLicitacao } from './licitacao.api';
import {
  CRITERIO_OPTIONS,
  MODALIDADE_OPTIONS,
  combinacaoModalidadeCriterioValida,
} from './licitacao.helpers';

export interface LicitacaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  objeto?: string;
  modalidade?: string;
  criterioJulgamento?: string;
  valorEstimado?: string;
  etpId?: string;
  termoReferenciaId?: string;
}

const CAMPOS: ReadonlySet<string> = new Set([
  'objeto',
  'modalidade',
  'criterioJulgamento',
  'valorEstimado',
  'etpId',
  'termoReferenciaId',
]);

const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function LicitacaoFormModal({ open, onClose }: LicitacaoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useAbrirLicitacao();

  const [objeto, setObjeto] = useState('');
  const [modalidade, setModalidade] = useState<ModalidadeLicitacao | ''>('');
  const [criterioJulgamento, setCriterioJulgamento] = useState<CriterioJulgamento | ''>('');
  const [valorEstimado, setValorEstimado] = useState('');
  const [etpId, setEtpId] = useState('');
  const [termoReferenciaId, setTermoReferenciaId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (objeto.trim() === '') next.objeto = 'Objeto é obrigatório.';
    else if (objeto.trim().length > 500) next.objeto = 'Objeto deve ter no máximo 500 caracteres.';
    if (modalidade === '') next.modalidade = 'Selecione a modalidade.';
    if (criterioJulgamento === '') next.criterioJulgamento = 'Selecione o critério de julgamento.';

    const valor = Number(valorEstimado);
    if (valorEstimado.trim() === '' || Number.isNaN(valor) || valor <= 0)
      next.valorEstimado = 'Valor estimado deve ser positivo.';

    if (
      modalidade !== '' &&
      criterioJulgamento !== '' &&
      !combinacaoModalidadeCriterioValida(modalidade, criterioJulgamento)
    ) {
      next.criterioJulgamento = 'Pregão admite apenas menor preço ou maior desconto.';
    }

    if (etpId.trim() !== '' && !GUID_REGEX.test(etpId.trim()))
      next.etpId = 'Informe um identificador (GUID) válido.';
    if (termoReferenciaId.trim() !== '' && !GUID_REGEX.test(termoReferenciaId.trim()))
      next.termoReferenciaId = 'Informe um identificador (GUID) válido.';

    return next;
  }

  function fechar(): void {
    setErrors({});
    setObjeto('');
    setModalidade('');
    setCriterioJulgamento('');
    setValorEstimado('');
    setEtpId('');
    setTermoReferenciaId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: AbrirLicitacaoInput = {
      objeto: objeto.trim(),
      modalidade: modalidade as ModalidadeLicitacao,
      criterioJulgamento: criterioJulgamento as CriterioJulgamento,
      valorEstimado: Number(valorEstimado),
      etpId: etpId.trim() || null,
      termoReferenciaId: termoReferenciaId.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success('Licitação aberta com sucesso.', 'Sucesso');
        fechar();
        if (resultado?.id) navigate(`/administracao/licitacoes/${resultado.id}`);
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
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a licitação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir licitação"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-licitacao" loading={mutation.isPending}>
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-abrir-licitacao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Objeto" required error={errors.objeto} help="Descrição do objeto licitado (máx. 500 caracteres).">
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
            <FormField label="Modalidade" required error={errors.modalidade}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  placeholder="Selecione…"
                  options={MODALIDADE_OPTIONS}
                  value={modalidade}
                  onChange={(e) => setModalidade(e.target.value as ModalidadeLicitacao)}
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
                  onChange={(e) => setCriterioJulgamento(e.target.value as CriterioJulgamento)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Valor estimado (R$)" required error={errors.valorEstimado}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorEstimado}
              onChange={(e) => setValorEstimado(e.target.value)}
            />
          )}
        </FormField>

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
