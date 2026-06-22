// Formulário de liquidação de despesa (POST /liquidacoes) em Modal. useMutation +
// validação por campo + mapeamento de ProblemDetails.fieldErrors + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { TIPO_DOCUMENTO_COMPROBATORIO, useLiquidarDespesa } from './financas.api';
import type { LiquidarDespesaInput } from './financas.api';
import { TIPOS_DOCUMENTO, hojeIso, mensagemErro, tratarErroCampos } from './financas.helpers';

export interface LiquidacaoFormModalProps {
  open: boolean;
  onClose: () => void;
  empenhoIdInicial?: string;
}

interface Campos {
  empenhoId?: string;
  valor?: string;
  dataLiquidacao?: string;
  tipoDocumento?: string;
  numeroDocumento?: string;
  chaveNfse?: string;
  dataEmissaoDocumento?: string;
}

const CONHECIDOS: Record<string, true> = {
  empenhoId: true, valor: true, dataLiquidacao: true, tipoDocumento: true,
  numeroDocumento: true, chaveNfse: true, dataEmissaoDocumento: true,
};

export function LiquidacaoFormModal({ open, onClose, empenhoIdInicial = '' }: LiquidacaoFormModalProps) {
  const toast = useToast();
  const mutation = useLiquidarDespesa();

  const [empenhoId, setEmpenhoId] = useState(empenhoIdInicial);
  const [valor, setValor] = useState('');
  const [dataLiquidacao, setDataLiquidacao] = useState(hojeIso());
  const [tipoDocumento, setTipoDocumento] = useState('');
  const [numeroDocumento, setNumeroDocumento] = useState('');
  const [chaveNfse, setChaveNfse] = useState('');
  const [dataEmissao, setDataEmissao] = useState('');
  const [errors, setErrors] = useState<Campos>({});

  const exigeChaveNfse = Number(tipoDocumento) === TIPO_DOCUMENTO_COMPROBATORIO.NfseChaveAcesso;

  function validar(): Campos {
    const next: Campos = {};
    if (empenhoId.trim() === '') next.empenhoId = 'Informe o empenho.';
    const v = Number(valor);
    if (valor.trim() === '' || Number.isNaN(v) || v <= 0) next.valor = 'Informe um valor maior que zero.';
    if (dataLiquidacao.trim() === '') next.dataLiquidacao = 'Informe a data da liquidação.';
    if (tipoDocumento === '') next.tipoDocumento = 'Selecione o tipo de documento.';
    if (exigeChaveNfse && chaveNfse.trim().length !== 44)
      next.chaveNfse = 'A chave da NFS-e deve ter 44 dígitos.';
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

    const input: LiquidarDespesaInput = {
      empenhoId: empenhoId.trim(),
      valor: Number(valor),
      dataLiquidacao,
      tipoDocumento: Number(tipoDocumento),
      numeroDocumento: numeroDocumento.trim() || null,
      chaveNfse: exigeChaveNfse ? chaveNfse.trim() : null,
      dataEmissaoDocumento: dataEmissao.trim() || null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Despesa liquidada (2º estágio).', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível liquidar a despesa.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Liquidar despesa"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-liquidar" loading={mutation.isPending}>
            Liquidar
          </Button>
        </>
      }
    >
      <form id="form-liquidar" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Identificador do empenho" required error={errors.empenhoId}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid}
              value={empenhoId} onChange={(e) => setEmpenhoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>
        <FormField label="Valor (R$)" required error={errors.valor}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="0" step="0.01" inputMode="decimal"
              aria-describedby={describedBy} invalid={invalid}
              value={valor} onChange={(e) => setValor(e.target.value)} />
          )}
        </FormField>
        <FormField label="Data da liquidação" required error={errors.dataLiquidacao}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
              value={dataLiquidacao} onChange={(e) => setDataLiquidacao(e.target.value)} />
          )}
        </FormField>
        <FormField label="Tipo de documento comprobatório" required error={errors.tipoDocumento}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={tipoDocumento} onChange={(e) => setTipoDocumento(e.target.value)}
              placeholder="Selecione…" options={TIPOS_DOCUMENTO} />
          )}
        </FormField>
        {exigeChaveNfse ? (
          <FormField label="Chave da NFS-e (44 dígitos)" required error={errors.chaveNfse}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={44}
                inputMode="numeric" value={chaveNfse} onChange={(e) => setChaveNfse(e.target.value)} />
            )}
          </FormField>
        ) : (
          <FormField label="Número do documento" help="Opcional." error={errors.numeroDocumento}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={60}
                value={numeroDocumento} onChange={(e) => setNumeroDocumento(e.target.value)} />
            )}
          </FormField>
        )}
        <FormField label="Data de emissão do documento" help="Opcional." error={errors.dataEmissaoDocumento}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
              value={dataEmissao} onChange={(e) => setDataEmissao(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
