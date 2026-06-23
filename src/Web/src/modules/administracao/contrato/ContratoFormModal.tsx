// Formulário de CELEBRAÇÃO de contrato em Modal (command CelebrarContrato).
// Validação por campo (I-1/I-2/I-3/I-4) + mapeamento de ApiError.fieldErrors + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { ORIGEM_NUMERICA, ORIGEM_ROTULO, useCelebrarContrato } from './contrato.api';
import type { CelebrarContratoInput, OrigemContratacao } from './contrato.api';
import { FornecedorPicker } from '../fornecedor/FornecedorPicker';
import { LicitacaoPicker } from '../licitacao/LicitacaoPicker';

export interface ContratoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  fornecedorId?: string;
  licitacaoId?: string;
  objeto?: string;
  valor?: string;
  vigenciaInicio?: string;
  vigenciaFim?: string;
  justificativaContratacaoDireta?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  fornecedorId: true,
  licitacaoId: true,
  objeto: true,
  valor: true,
  vigenciaInicio: true,
  vigenciaFim: true,
  justificativaContratacaoDireta: true,
};

export function ContratoFormModal({ open, onClose }: ContratoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useCelebrarContrato();

  const [fornecedorId, setFornecedorId] = useState('');
  const [origem, setOrigem] = useState<OrigemContratacao>('Licitacao');
  const [licitacaoId, setLicitacaoId] = useState('');
  const [objeto, setObjeto] = useState('');
  const [valor, setValor] = useState('');
  const [vigenciaInicio, setVigenciaInicio] = useState('');
  const [vigenciaFim, setVigenciaFim] = useState('');
  const [empenhoId, setEmpenhoId] = useState('');
  const [numeroEmpenho, setNumeroEmpenho] = useState('');
  const [justificativa, setJustificativa] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const ehContratacaoDireta = origem !== 'Licitacao';

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (fornecedorId.trim() === '') next.fornecedorId = 'Informe o identificador do fornecedor.';
    if (objeto.trim() === '') next.objeto = 'Informe o objeto do contrato.';
    else if (objeto.trim().length > 500) next.objeto = 'Objeto deve ter no máximo 500 caracteres.';

    const valorNum = Number(valor);
    if (valor.trim() === '' || Number.isNaN(valorNum) || valorNum <= 0)
      next.valor = 'Informe um valor contratado maior que zero.';

    if (vigenciaInicio.trim() === '') next.vigenciaInicio = 'Informe o início da vigência.';
    if (vigenciaFim.trim() === '') next.vigenciaFim = 'Informe o fim da vigência.';
    else if (vigenciaInicio.trim() !== '' && vigenciaFim < vigenciaInicio)
      next.vigenciaFim = 'Fim da vigência não pode ser anterior ao início.';

    // I-4: coerência origem × licitação.
    if (origem === 'Licitacao' && licitacaoId.trim() === '')
      next.licitacaoId = 'Licitação é obrigatória quando a origem é Licitação.';
    if (ehContratacaoDireta && licitacaoId.trim() !== '')
      next.licitacaoId = 'Licitação é vedada nas contratações diretas.';
    if (ehContratacaoDireta && justificativa.trim() === '')
      next.justificativaContratacaoDireta = 'Justificativa é obrigatória na contratação direta.';

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

    const input: CelebrarContratoInput = {
      fornecedorId: fornecedorId.trim(),
      origem: ORIGEM_NUMERICA[origem],
      licitacaoId: origem === 'Licitacao' ? licitacaoId.trim() : null,
      objeto: objeto.trim(),
      valor: Number(valor),
      vigenciaInicio,
      vigenciaFim,
      empenhoId: empenhoId.trim() || null,
      numeroEmpenho: numeroEmpenho.trim() || null,
      justificativaContratacaoDireta: ehContratacaoDireta ? justificativa.trim() : null,
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success('Contrato celebrado (situação: Assinado).', 'Sucesso');
        fechar();
        navigate(`/administracao/contratos/${resposta.id}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível celebrar o contrato.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Celebrar contrato"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-celebrar-contrato" loading={mutation.isPending}>
            Celebrar
          </Button>
        </>
      }
    >
      <form id="form-celebrar-contrato" className="br-form" onSubmit={submeter} noValidate>
        <FornecedorPicker
          label="Fornecedor"
          required
          error={errors.fornecedorId}
          value={fornecedorId}
          onChange={(id) => {
            setFornecedorId(id);
            if (id !== '') setErrors((prev) => ({ ...prev, fornecedorId: undefined }));
          }}
        />

        <FormField label="Origem da contratação" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={origem}
              onChange={(e) => setOrigem(e.target.value as OrigemContratacao)}
              options={[
                { value: 'Licitacao', label: ORIGEM_ROTULO.Licitacao },
                { value: 'Dispensa', label: ORIGEM_ROTULO.Dispensa },
                { value: 'Inexigibilidade', label: ORIGEM_ROTULO.Inexigibilidade },
              ]}
            />
          )}
        </FormField>

        {origem === 'Licitacao' && (
          <LicitacaoPicker
            label="Licitação de origem"
            required
            error={errors.licitacaoId}
            value={licitacaoId}
            onChange={(id) => {
              setLicitacaoId(id);
              if (id !== '') setErrors((prev) => ({ ...prev, licitacaoId: undefined }));
            }}
          />
        )}

        <FormField label="Objeto" required error={errors.objeto} help="Máx. 500 caracteres.">
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={500}
              value={objeto}
              onChange={(e) => setObjeto(e.target.value)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-4">
            <FormField label="Valor contratado (R$)" required error={errors.valor}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Início da vigência" required error={errors.vigenciaInicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vigenciaInicio}
                  onChange={(e) => setVigenciaInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Fim da vigência" required error={errors.vigenciaFim}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vigenciaFim}
                  onChange={(e) => setVigenciaFim(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Identificador do empenho" help="Opcional (cobertura orçamentária).">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={empenhoId}
                  onChange={(e) => setEmpenhoId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Número do empenho" help="Opcional.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={numeroEmpenho}
                  onChange={(e) => setNumeroEmpenho(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        {ehContratacaoDireta && (
          <FormField
            label="Justificativa da contratação direta"
            required
            error={errors.justificativaContratacaoDireta}
            help="Obrigatória nas contratações por Dispensa/Inexigibilidade (art. 74/75)."
          >
            {({ id, describedBy, invalid }) => (
              <Textarea
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={justificativa}
                onChange={(e) => setJustificativa(e.target.value)}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
