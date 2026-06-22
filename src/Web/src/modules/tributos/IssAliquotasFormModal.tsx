// Formulário de CONFIGURAÇÃO das alíquotas do ISS (command ConfigurarAliquotasIss):
// uma linha POR ITEM da Lista de Serviços (LC 116/2003), com a alíquota (digitada em
// % e convertida para FRAÇÃO DECIMAL) e os indicadores de RETENÇÃO na fonte e
// SUBSTITUIÇÃO tributária. Lista dinâmica (adicionar/remover). Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useConfigurarAliquotasIss } from './iss.api';
import type { ConfigurarAliquotasIssInput, ItemAliquotaIssInput } from './iss.api';
import { percentualParaFracao } from './iptu.helpers';

const ANO_ATUAL = new Date().getFullYear();

export interface IssAliquotasFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface ItemLinha {
  itemListaServico: string;
  descricao: string;
  aliquota: string;
  retencao: boolean;
  substituicao: boolean;
}

const ITEM_VAZIO: ItemLinha = {
  itemListaServico: '',
  descricao: '',
  aliquota: '',
  retencao: false,
  substituicao: false,
};

export function IssAliquotasFormModal({ open, onClose }: IssAliquotasFormModalProps) {
  const toast = useToast();
  const mutation = useConfigurarAliquotasIss();

  const [exercicio, setExercicio] = useState(String(ANO_ATUAL));
  const [itens, setItens] = useState<ItemLinha[]>([{ ...ITEM_VAZIO }]);
  const [erro, setErro] = useState<string | null>(null);

  function atualizar(idx: number, patch: Partial<ItemLinha>): void {
    setItens((p) => p.map((it, i) => (i === idx ? { ...it, ...patch } : it)));
  }

  function fechar(): void {
    setExercicio(String(ANO_ATUAL));
    setItens([{ ...ITEM_VAZIO }]);
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido.');
      return;
    }
    const validos = itens.filter((i) => i.itemListaServico.trim() !== '');
    if (validos.length === 0) {
      setErro('Cadastre ao menos um item da Lista de Serviços (LC 116).');
      return;
    }
    const itensInput: ItemAliquotaIssInput[] = validos.map((i) => ({
      itemListaServico: i.itemListaServico.trim(),
      descricao: i.descricao.trim(),
      aliquota: percentualParaFracao(i.aliquota),
      retencao: i.retencao,
      substituicao: i.substituicao,
    }));
    // LC 116: alíquota mínima 2% e máxima 5%; validamos a faixa legal.
    if (itensInput.some((i) => Number.isNaN(i.aliquota) || i.aliquota < 0.02 || i.aliquota > 0.05)) {
      setErro('As alíquotas do ISS devem estar entre 2% e 5% (LC 116/2003).');
      return;
    }
    setErro(null);

    const input: ConfigurarAliquotasIssInput = { exercicio: ano, itens: itensInput };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Alíquotas do ISS ${ano} configuradas (id ${r.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível configurar as alíquotas.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Configurar alíquotas do ISS (LC 116)"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-iss-aliquotas" loading={mutation.isPending}>
            Configurar
          </Button>
        </>
      }
    >
      <form id="form-iss-aliquotas" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <FormField label="Exercício" required>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
          )}
        </FormField>

        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Itens da Lista de Serviços</legend>
          {itens.map((item, idx) => (
            <div className="row align-items-end" key={idx}>
              <div className="col-sm-2">
                <FormField label="Item LC 116">
                  {({ id }) => (
                    <Input id={id} value={item.itemListaServico} placeholder="7.02" onChange={(e) => atualizar(idx, { itemListaServico: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Descrição">
                  {({ id }) => (
                    <Input id={id} value={item.descricao} onChange={(e) => atualizar(idx, { descricao: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2">
                <FormField label="Alíquota (%)">
                  {({ id }) => (
                    <Input id={id} type="number" min="2" max="5" step="0.01" inputMode="decimal" value={item.aliquota} placeholder="5,00" onChange={(e) => atualizar(idx, { aliquota: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2 mb-3">
                <div className="br-checkbox">
                  <input id={`ret-${idx}`} type="checkbox" checked={item.retencao} onChange={(e) => atualizar(idx, { retencao: e.target.checked })} />
                  <label htmlFor={`ret-${idx}`}>Retenção</label>
                </div>
                <div className="br-checkbox">
                  <input id={`sub-${idx}`} type="checkbox" checked={item.substituicao} onChange={(e) => atualizar(idx, { substituicao: e.target.checked })} />
                  <label htmlFor={`sub-${idx}`}>Substituição</label>
                </div>
              </div>
              <div className="col-sm-2 mb-3">
                <Button variant="tertiary" onClick={() => setItens((p) => p.filter((_, i) => i !== idx))} disabled={itens.length === 1} aria-label={`Remover item ${idx + 1}`}>
                  <i className="fas fa-trash" aria-hidden="true" /> Remover
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={() => setItens((p) => [...p, { ...ITEM_VAZIO }])}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
          </Button>
        </fieldset>
      </form>
    </Modal>
  );
}
