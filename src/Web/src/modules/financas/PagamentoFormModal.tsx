// Formulário de emissão de Ordem de Pagamento (POST /ordens-pagamento) em Modal.
// Dados bancários + lista dinâmica de itens (liquidação + valor). useMutation + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { useEmitirOrdem } from './financas.api';
import type { EmitirOrdemInput, ItemPagamento } from './financas.api';
import { hojeIso, mensagemErro, tratarErroCampos } from './financas.helpers';

export interface PagamentoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface ItemForm {
  liquidacaoId: string;
  valor: string;
}

interface Campos {
  numero?: string;
  dataPagamento?: string;
  banco?: string;
  agencia?: string;
  conta?: string;
  itens?: string;
}

const CONHECIDOS: Record<string, true> = {
  numero: true, dataPagamento: true, banco: true, agencia: true, conta: true, pix: true, itens: true,
};

const ITEM_VAZIO: ItemForm = { liquidacaoId: '', valor: '' };

export function PagamentoFormModal({ open, onClose }: PagamentoFormModalProps) {
  const toast = useToast();
  const mutation = useEmitirOrdem();

  const [numero, setNumero] = useState('');
  const [dataPagamento, setDataPagamento] = useState(hojeIso());
  const [banco, setBanco] = useState('');
  const [agencia, setAgencia] = useState('');
  const [conta, setConta] = useState('');
  const [pix, setPix] = useState('');
  const [itens, setItens] = useState<ItemForm[]>([{ ...ITEM_VAZIO }]);
  const [errors, setErrors] = useState<Campos>({});

  function atualizarItem(indice: number, campo: keyof ItemForm, valor: string): void {
    setItens((atual) => atual.map((it, i) => (i === indice ? { ...it, [campo]: valor } : it)));
  }

  function validar(): { campos: Campos; itensValidos: ItemPagamento[] } {
    const campos: Campos = {};
    if (numero.trim() === '') campos.numero = 'Informe o número da ordem.';
    if (dataPagamento.trim() === '') campos.dataPagamento = 'Informe a data do pagamento.';
    if (banco.trim() === '') campos.banco = 'Informe o banco.';
    if (agencia.trim() === '') campos.agencia = 'Informe a agência.';
    if (conta.trim() === '') campos.conta = 'Informe a conta.';

    const itensValidos: ItemPagamento[] = [];
    for (const it of itens) {
      const v = Number(it.valor);
      if (it.liquidacaoId.trim() !== '' && !Number.isNaN(v) && v > 0) {
        itensValidos.push({ liquidacaoId: it.liquidacaoId.trim(), valor: v });
      }
    }
    if (itensValidos.length === 0) campos.itens = 'Informe ao menos um item (liquidação e valor > 0).';
    return { campos, itensValidos };
  }

  function fechar(): void {
    setErrors({});
    setItens([{ ...ITEM_VAZIO }]);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const { campos, itensValidos } = validar();
    setErrors(campos);
    if (Object.keys(campos).length > 0) return;

    const input: EmitirOrdemInput = {
      numero: numero.trim(),
      dataPagamento,
      banco: banco.trim(),
      agencia: agencia.trim(),
      conta: conta.trim(),
      pix: pix.trim() || null,
      itens: itensValidos,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Ordem ${input.numero} emitida.`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível emitir a ordem de pagamento.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir ordem de pagamento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-ordem-pagamento" loading={mutation.isPending}>
            Emitir
          </Button>
        </>
      }
    >
      <form id="form-ordem-pagamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Número da ordem" required error={errors.numero}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={30}
              value={numero} onChange={(e) => setNumero(e.target.value)} placeholder="2026OP000045" />
          )}
        </FormField>
        <FormField label="Data do pagamento" required error={errors.dataPagamento}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
              value={dataPagamento} onChange={(e) => setDataPagamento(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-12 col-md-4">
            <FormField label="Banco" required error={errors.banco}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={10}
                  value={banco} onChange={(e) => setBanco(e.target.value)} placeholder="001" />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Agência" required error={errors.agencia}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={10}
                  value={agencia} onChange={(e) => setAgencia(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Conta" required error={errors.conta}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={20}
                  value={conta} onChange={(e) => setConta(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Chave Pix" help="Opcional.">
          {({ id, describedBy }) => (
            <Input id={id} aria-describedby={describedBy} maxLength={140}
              value={pix} onChange={(e) => setPix(e.target.value)} />
          )}
        </FormField>

        <fieldset className="mt-3">
          <legend className="text-up-01 text-semi-bold">Itens (liquidações)</legend>
          {errors.itens && (
            <p className="feedback danger" role="alert" id="erro-itens">
              <i className="fas fa-times-circle" aria-hidden="true" /> {errors.itens}
            </p>
          )}
          {itens.map((it, i) => (
            <div className="row align-items-end" key={i}>
              <div className="col">
                <FormField label={`Liquidação ${i + 1}`}>
                  {({ id, describedBy }) => (
                    <Input id={id} aria-describedby={describedBy} value={it.liquidacaoId}
                      onChange={(e) => atualizarItem(i, 'liquidacaoId', e.target.value)}
                      placeholder="00000000-0000-0000-0000-000000000000" />
                  )}
                </FormField>
              </div>
              <div className="col-auto">
                <FormField label="Valor (R$)">
                  {({ id, describedBy }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal"
                      aria-describedby={describedBy} value={it.valor}
                      onChange={(e) => atualizarItem(i, 'valor', e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-auto mb-3">
                <Button variant="secondary" onClick={() => setItens((a) => a.filter((_, idx) => idx !== i))}
                  disabled={itens.length === 1} aria-label={`Remover item ${i + 1}`}>
                  <i className="fas fa-trash" aria-hidden="true" />
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={() => setItens((a) => [...a, { ...ITEM_VAZIO }])}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
          </Button>
        </fieldset>
      </form>
    </Modal>
  );
}
