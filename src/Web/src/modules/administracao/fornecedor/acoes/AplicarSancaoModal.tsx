// AplicarSancao -> AplicarSancaoCommand (POST /fornecedores/{id}/sancoes).
// Mutation + validação por campo + Toast + mapeamento de ApiError.fieldErrors.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../../components/ui';
import type { SelectOption } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import {
  TIPOS_SANCAO,
  TIPO_SANCAO_LABEL,
  TIPOS_IMPEDITIVOS,
  useAplicarSancao,
} from '../fornecedor.api';
import type { AplicarSancaoInput, TipoSancao } from '../fornecedor.api';
import { hojeIso } from '../fornecedor.helpers';
import type { AcaoModalProps } from './acoesModais.shared';

interface SancaoErrors {
  tipo?: string;
  dataInicio?: string;
  dataFim?: string;
  processoAdministrativo?: string;
  fundamentacao?: string;
  valorMulta?: string;
}

const SANCAO_CAMPOS: Record<keyof SancaoErrors, true> = {
  tipo: true,
  dataInicio: true,
  dataFim: true,
  processoAdministrativo: true,
  fundamentacao: true,
  valorMulta: true,
};

export function AplicarSancaoModal({ open, onClose, fornecedorId }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useAplicarSancao(fornecedorId);

  const [tipo, setTipo] = useState<TipoSancao | ''>('');
  const [dataInicio, setDataInicio] = useState(hojeIso());
  const [dataFim, setDataFim] = useState('');
  const [processoAdministrativo, setProcessoAdministrativo] = useState('');
  const [fundamentacao, setFundamentacao] = useState('');
  const [valorMulta, setValorMulta] = useState('');
  const [errors, setErrors] = useState<SancaoErrors>({});

  const ehMulta = tipo === 'Multa';
  const ehImpeditiva = tipo !== '' && TIPOS_IMPEDITIVOS.has(tipo);

  const tipoOptions: SelectOption[] = TIPOS_SANCAO.map((t) => ({ value: t, label: TIPO_SANCAO_LABEL[t] }));

  function validar(): SancaoErrors {
    const next: SancaoErrors = {};
    if (tipo === '') next.tipo = 'Selecione o tipo de sancao.';
    if (dataInicio.trim() === '') next.dataInicio = 'Informe a data de inicio.';
    if (dataFim.trim() !== '' && dataInicio.trim() !== '' && dataFim < dataInicio)
      next.dataFim = 'Data fim nao pode ser anterior ao inicio.';
    if (processoAdministrativo.trim() === '') next.processoAdministrativo = 'Informe o processo administrativo.';
    else if (processoAdministrativo.trim().length > 60)
      next.processoAdministrativo = 'Processo administrativo: maximo 60 caracteres.';
    if (fundamentacao.trim() === '') next.fundamentacao = 'Informe a fundamentacao.';
    if (ehMulta) {
      const valor = Number(valorMulta);
      if (valorMulta.trim() === '' || Number.isNaN(valor) || valor <= 0)
        next.valorMulta = 'Valor da multa deve ser positivo.';
    }
    return next;
  }

  function fechar(): void {
    setTipo('');
    setDataInicio(hojeIso());
    setDataFim('');
    setProcessoAdministrativo('');
    setFundamentacao('');
    setValorMulta('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || tipo === '') return;

    const input: AplicarSancaoInput = {
      tipo,
      dataInicio: dataInicio.trim(),
      dataFim: dataFim.trim() === '' ? null : dataFim.trim(),
      processoAdministrativo: processoAdministrativo.trim(),
      fundamentacao: fundamentacao.trim(),
      valorMulta: ehMulta ? Number(valorMulta) : null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Sancao registrada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: SancaoErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof SancaoErrors;
            if (key in SANCAO_CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Nao foi possivel aplicar a sancao.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Aplicar sancao administrativa"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-aplicar-sancao" loading={mutation.isPending}>
            Aplicar sancao
          </Button>
        </>
      }
    >
      <form id="form-aplicar-sancao" className="br-form" onSubmit={submeter} noValidate>
        {ehImpeditiva && (
          <Alert variant="warning" title="Sancao impeditiva">
            Sancao de {tipo === 'Inidoneidade' ? 'inidoneidade' : 'impedimento'} vigente torna o fornecedor
            impedido de licitar e contratar e altera a situacao para Sancionado (art. 156).
          </Alert>
        )}

        <FormField label="Tipo de sancao" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoSancao)}
              placeholder="Selecione…"
              options={tipoOptions}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Data de inicio" required error={errors.dataInicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataInicio}
                  onChange={(e) => setDataInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de fim" help="Em branco = sem termo final (vigente ate reabilitacao)." error={errors.dataFim}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataFim}
                  min={dataInicio || undefined}
                  onChange={(e) => setDataFim(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Processo administrativo" required error={errors.processoAdministrativo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={processoAdministrativo}
              onChange={(e) => setProcessoAdministrativo(e.target.value)}
              maxLength={60}
              placeholder="PA-2026-0001"
            />
          )}
        </FormField>

        {ehMulta && (
          <FormField label="Valor da multa (R$)" required error={errors.valorMulta}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                aria-describedby={describedBy}
                invalid={invalid}
                value={valorMulta}
                onChange={(e) => setValorMulta(e.target.value)}
              />
            )}
          </FormField>
        )}

        <FormField label="Fundamentacao" required error={errors.fundamentacao} help="Motivacao do ato (devido processo legal).">
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={fundamentacao}
              onChange={(e) => setFundamentacao(e.target.value)}
              rows={4}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
