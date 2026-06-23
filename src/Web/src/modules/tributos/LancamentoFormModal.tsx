// Formulário de LANÇAMENTO de crédito tributário (command LancarCredito) em Modal.
// Espelha o validator: ContribuinteId NotEmpty; TipoTributo IsInEnum; Ano >= 1900;
// Mes 1..12; ValorPrincipal > 0. A inscrição em Dívida Ativa (que exige fundamento
// legal + encargos parametrizáveis) é feita na ação dedicada "Inscrever em Dívida
// Ativa" (InscreverDividaModal), a partir do identificador do lançamento gerado aqui.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { TIPO_TRIBUTO_VALOR, useLancarCredito } from './api';
import type { LancarCreditoInput, TipoTributo } from './api';
import { TIPO_TRIBUTO_LABEL } from './dividaAtiva.helpers';

const ANO_MIN = 1900;

const TIPO_TRIBUTO_OPCOES: SelectOption[] = (
  ['Iptu', 'Iss', 'Itbi', 'Taxa'] as TipoTributo[]
).map((t) => ({ value: t, label: TIPO_TRIBUTO_LABEL[t] }));

export interface LancamentoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o contribuinte quando aberto a partir de uma consulta. */
  contribuinteIdInicial?: string;
}

interface FormErrors {
  contribuinteId?: string;
  tipoTributo?: string;
  ano?: string;
  mes?: string;
  valorPrincipal?: string;
  vencimento?: string;
}

export function LancamentoFormModal({ open, onClose, contribuinteIdInicial = '' }: LancamentoFormModalProps) {
  const toast = useToast();
  const lancar = useLancarCredito();

  const [contribuinteId, setContribuinteId] = useState(contribuinteIdInicial);
  const [tipoTributo, setTipoTributo] = useState<TipoTributo | ''>('');
  const [ano, setAno] = useState('');
  const [mes, setMes] = useState('');
  const [valorPrincipal, setValorPrincipal] = useState('');
  const [vencimento, setVencimento] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const pendente = lancar.isPending;

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (contribuinteId.trim() === '') next.contribuinteId = 'Informe o identificador do contribuinte.';
    if (tipoTributo === '') next.tipoTributo = 'Selecione a espécie tributária.';
    const anoNum = Number(ano);
    if (ano.trim() === '' || !Number.isInteger(anoNum) || anoNum < ANO_MIN)
      next.ano = `Informe um ano válido (>= ${ANO_MIN}).`;
    const mesNum = Number(mes);
    if (mes.trim() === '' || !Number.isInteger(mesNum) || mesNum < 1 || mesNum > 12)
      next.mes = 'Informe um mês entre 1 e 12.';
    const valor = Number(valorPrincipal);
    if (valorPrincipal.trim() === '' || Number.isNaN(valor) || valor <= 0)
      next.valorPrincipal = 'Informe um valor principal maior que zero.';
    if (vencimento.trim() === '') next.vencimento = 'Informe a data de vencimento.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setTipoTributo('');
    setAno('');
    setMes('');
    setValorPrincipal('');
    setVencimento('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || tipoTributo === '') return;

    const input: LancarCreditoInput = {
      contribuinteId: contribuinteId.trim(),
      tipoTributo: TIPO_TRIBUTO_VALOR[tipoTributo],
      ano: Number(ano),
      mes: Number(mes),
      valorPrincipal: Number(valorPrincipal),
      vencimento: vencimento.trim(),
    };

    lancar.mutate(input, {
      onSuccess: (resultado) => {
        toast.success(`Crédito lançado (id ${resultado.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          const conhecidos: Record<string, number> = {
            contribuinteId: 1,
            tipoTributo: 1,
            ano: 1,
            mes: 1,
            valorPrincipal: 1,
            vencimento: 1,
          };
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in conhecidos) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar o crédito.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lançar crédito tributário"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-lancar-credito" loading={pendente}>
            Lançar
          </Button>
        </>
      }
    >
      <form id="form-lancar-credito" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Identificador do contribuinte" required error={errors.contribuinteId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={contribuinteId}
              onChange={(e) => setContribuinteId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Espécie tributária" required error={errors.tipoTributo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={TIPO_TRIBUTO_OPCOES}
              value={tipoTributo}
              onChange={(e) => setTipoTributo(e.target.value as TipoTributo | '')}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-6">
            <FormField label="Ano da competência" required error={errors.ano}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={ANO_MIN}
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Mês da competência" required error={errors.mes}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={1}
                  max={12}
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Valor principal (R$)" required error={errors.valorPrincipal}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorPrincipal}
              onChange={(e) => setValorPrincipal(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Vencimento" required error={errors.vencimento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={vencimento}
              onChange={(e) => setVencimento(e.target.value)}
            />
          )}
        </FormField>

        <Alert variant="info" title="Inscrição em Dívida Ativa">
          Após lançar, use a ação &ldquo;Inscrever em Dívida Ativa&rdquo; (com fundamento legal e
          encargos) informando o identificador do lançamento — somente se já vencido e em aberto.
        </Alert>
      </form>
    </Modal>
  );
}
