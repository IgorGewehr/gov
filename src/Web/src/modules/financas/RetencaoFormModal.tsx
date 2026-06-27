// Formulário de adição de retenção/consignação a uma liquidação (POST /liquidacoes/{id}/retencoes).
// IRRF/PJ: cálculo automático pela tabela vigente (escolha do enquadramento); demais naturezas: valor
// informado. Mapeia ProblemDetails + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { mensagemErro } from './financas.helpers';
import {
  NATUREZA_RETENCAO_LABEL,
  NaturezaRetencao,
  useAdicionarRetencao,
  useTabelaIrrf,
  type AdicionarRetencaoInput,
} from './retencoes.api';

export interface RetencaoFormModalProps {
  open: boolean;
  onClose: () => void;
  liquidacaoId: string;
}

const NATUREZAS = Object.entries(NATUREZA_RETENCAO_LABEL).map(([value, label]) => ({ value, label }));

export function RetencaoFormModal({ open, onClose, liquidacaoId }: RetencaoFormModalProps) {
  const toast = useToast();
  const mutation = useAdicionarRetencao(liquidacaoId);
  const tabela = useTabelaIrrf();

  const [natureza, setNatureza] = useState(String(NaturezaRetencao.IrrfPessoaJuridica));
  const [enquadramento, setEnquadramento] = useState('');
  const [valorInformado, setValorInformado] = useState('');
  const [baseCalculo, setBaseCalculo] = useState('');
  const [codigoReceita, setCodigoReceita] = useState('');
  const [descricao, setDescricao] = useState('');

  const ehIrrfPj = Number(natureza) === NaturezaRetencao.IrrfPessoaJuridica;
  const usaTabela = ehIrrfPj && valorInformado.trim() === '';

  function fechar(): void {
    onClose();
  }

  function enviar(event: FormEvent): void {
    event.preventDefault();
    const input: AdicionarRetencaoInput = {
      natureza: Number(natureza),
      enquadramentoIrrf: usaTabela ? enquadramento || null : null,
      baseCalculo: baseCalculo.trim() === '' ? null : Number(baseCalculo),
      valorInformado: valorInformado.trim() === '' ? null : Number(valorInformado),
      codigoReceita: codigoReceita.trim() === '' ? null : codigoReceita.trim(),
      descricao: descricao.trim() === '' ? null : descricao.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Retenção adicionada.', 'Sucesso');
        fechar();
      },
      onError: (e) => toast.error(mensagemErro(e, 'Não foi possível adicionar a retenção.')),
    });
  }

  const enquadramentos = (tabela.data?.faixas ?? []).map((f) => ({
    value: f.codigo,
    label: `${f.descricao} — ${(f.aliquota * 100).toFixed(2).replace('.', ',')}% (DARF ${f.codigoReceitaDarf})`,
  }));

  return (
    <Modal open={open} onClose={fechar} title="Adicionar retenção / consignação">
      <form className="br-form" onSubmit={enviar}>
        <FormField label="Natureza" required>
          {({ id }) => (
            <Select
              id={id}
              value={natureza}
              onChange={(e) => setNatureza(e.target.value)}
              options={NATUREZAS}
            />
          )}
        </FormField>

        {usaTabela ? (
          <FormField label="Enquadramento IRRF (IN RFB 1234/2012)" required>
            {({ id }) => (
              <Select
                id={id}
                value={enquadramento}
                onChange={(e) => setEnquadramento(e.target.value)}
                options={enquadramentos}
                placeholder="Selecione a natureza do bem/serviço"
              />
            )}
          </FormField>
        ) : (
          <FormField label="Valor a reter (R$)" required>
            {({ id }) => (
              <Input
                id={id}
                type="number"
                step="0.01"
                min="0"
                value={valorInformado}
                onChange={(e) => setValorInformado(e.target.value)}
              />
            )}
          </FormField>
        )}

        <FormField label="Base de cálculo (R$) — opcional (default = valor liquidado)">
          {({ id }) => (
            <Input
              id={id}
              type="number"
              step="0.01"
              min="0"
              value={baseCalculo}
              onChange={(e) => setBaseCalculo(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Código de receita (DARF/guia) — opcional">
          {({ id }) => <Input id={id} value={codigoReceita} onChange={(e) => setCodigoReceita(e.target.value)} />}
        </FormField>

        <FormField label="Descrição — opcional">
          {({ id }) => <Input id={id} value={descricao} onChange={(e) => setDescricao(e.target.value)} />}
        </FormField>

        <div className="d-flex justify-content-end mt-3" style={{ gap: '0.5rem' }}>
          <Button type="button" variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" loading={mutation.isPending}>
            Adicionar
          </Button>
        </div>
      </form>
    </Modal>
  );
}
