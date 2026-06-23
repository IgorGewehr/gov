// Formulário de CONFIGURAÇÃO da tabela de uma TAXA (ConfigurarTabelaTaxaCommand):
// código + descrição + espécie (polícia/serviço/licença) + modo de cálculo
// (fixo/por unidade/por faixa) + exercício + valor-base + fundamento legal. No modo
// PorFaixa, exige ao menos uma faixa (limite inferior/superior + valor). Nenhum valor
// é hardcoded — tudo vem da LEI MUNICIPAL (CTM). Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Select, useToast } from '../../components/ui';
import { Modal } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  ESPECIE_TAXA_LABEL,
  ESPECIE_TAXA_VALOR,
  MODO_CALCULO_LABEL,
  MODO_CALCULO_VALOR,
  useConfigurarTabelaTaxa,
} from './taxas.api';
import type {
  ConfigurarTabelaTaxaInput,
  EspecieTaxa,
  FaixaTaxaInput,
  ModoCalculoTaxa,
} from './taxas.api';

export interface TabelaTaxaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FaixaLinha {
  limiteInferior: string;
  limiteSuperior: string;
  valor: string;
}

const FAIXA_VAZIA: FaixaLinha = { limiteInferior: '', limiteSuperior: '', valor: '' };

const ESPECIE_OPCOES = (Object.keys(ESPECIE_TAXA_VALOR) as EspecieTaxa[]).map((k) => ({
  value: k,
  label: ESPECIE_TAXA_LABEL[k],
}));
const MODO_OPCOES = (Object.keys(MODO_CALCULO_VALOR) as ModoCalculoTaxa[]).map((k) => ({
  value: k,
  label: MODO_CALCULO_LABEL[k],
}));

export function TabelaTaxaFormModal({ open, onClose }: TabelaTaxaFormModalProps) {
  const toast = useToast();
  const mutation = useConfigurarTabelaTaxa();

  const [codigo, setCodigo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [especie, setEspecie] = useState<EspecieTaxa>('PoderPolicia');
  const [modo, setModo] = useState<ModoCalculoTaxa>('ValorFixo');
  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [valorBase, setValorBase] = useState('');
  const [fundamentoLegal, setFundamentoLegal] = useState('');
  const [faixas, setFaixas] = useState<FaixaLinha[]>([{ ...FAIXA_VAZIA }]);
  const [erro, setErro] = useState<string | null>(null);

  function atualizarFaixa(idx: number, patch: Partial<FaixaLinha>): void {
    setFaixas((p) => p.map((f, i) => (i === idx ? { ...f, ...patch } : f)));
  }

  function fechar(): void {
    setCodigo('');
    setDescricao('');
    setEspecie('PoderPolicia');
    setModo('ValorFixo');
    setExercicio(String(new Date().getFullYear()));
    setValorBase('');
    setFundamentoLegal('');
    setFaixas([{ ...FAIXA_VAZIA }]);
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (codigo.trim() === '' || descricao.trim() === '') {
      setErro('Informe o código e a descrição da taxa.');
      return;
    }
    if (fundamentoLegal.trim() === '') {
      setErro('Informe o fundamento legal (artigo do CTM).');
      return;
    }
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido (≥ 1900).');
      return;
    }

    let faixasInput: FaixaTaxaInput[] | null = null;
    if (modo === 'PorFaixa') {
      const validas = faixas.filter((f) => f.limiteInferior.trim() !== '' && f.valor.trim() !== '');
      if (validas.length === 0) {
        setErro('O modo Por faixa exige ao menos uma faixa (limite inferior + valor).');
        return;
      }
      faixasInput = validas.map((f) => ({
        limiteInferior: Number(f.limiteInferior.replace(',', '.')),
        limiteSuperior: f.limiteSuperior.trim() === '' ? null : Number(f.limiteSuperior.replace(',', '.')),
        valor: Number(f.valor.replace(',', '.')),
      }));
    }
    setErro(null);

    const input: ConfigurarTabelaTaxaInput = {
      codigo: codigo.trim(),
      descricao: descricao.trim(),
      especie: ESPECIE_TAXA_VALOR[especie],
      modoCalculo: MODO_CALCULO_VALOR[modo],
      exercicio: ano,
      valorBase: Number((valorBase || '0').replace(',', '.')),
      fundamentoLegal: fundamentoLegal.trim(),
      faixas: faixasInput,
    };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Tabela da taxa ${codigo.trim()} configurada (id ${r.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível configurar a tabela.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Configurar tabela de taxa (CTM)"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-tabela-taxa" loading={mutation.isPending}>
            Configurar
          </Button>
        </>
      }
    >
      <form id="form-tabela-taxa" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <div className="row">
          <div className="col-md-3">
            <FormField label="Código (CTM)" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={codigo} onChange={(e) => setCodigo(e.target.value)} placeholder="T-001" />
              )}
            </FormField>
          </div>
          <div className="col-md-9">
            <FormField label="Descrição" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Taxa de coleta de lixo" />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-5">
            <FormField label="Espécie" required>
              {({ id }) => (
                <Select id={id} options={ESPECIE_OPCOES} value={especie} onChange={(e) => setEspecie(e.target.value as EspecieTaxa)} />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Modo de cálculo" required>
              {({ id }) => (
                <Select id={id} options={MODO_OPCOES} value={modo} onChange={(e) => setModo(e.target.value as ModoCalculoTaxa)} />
              )}
            </FormField>
          </div>
          <div className="col-md-3">
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-4">
            <FormField label="Valor-base (R$)" help="Fixo/unitário; ignorado no modo Por faixa.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valorBase} onChange={(e) => setValorBase(e.target.value)} placeholder="0,00" />
              )}
            </FormField>
          </div>
          <div className="col-md-8">
            <FormField label="Fundamento legal" required help="Artigo do CTM (lei municipal).">
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fundamentoLegal} onChange={(e) => setFundamentoLegal(e.target.value)} placeholder="Art. 120 do CTM" />
              )}
            </FormField>
          </div>
        </div>

        {modo === 'PorFaixa' && (
          <fieldset className="mb-3">
            <legend className="text-up-01 text-semi-bold">Faixas da quantidade-base</legend>
            {faixas.map((faixa, idx) => (
              <div className="row align-items-end" key={idx}>
                <div className="col-sm-3">
                  <FormField label="Limite inferior">
                    {({ id }) => (
                      <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.limiteInferior} onChange={(e) => atualizarFaixa(idx, { limiteInferior: e.target.value })} />
                    )}
                  </FormField>
                </div>
                <div className="col-sm-3">
                  <FormField label="Limite superior" help="Vazio = sem teto.">
                    {({ id }) => (
                      <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.limiteSuperior} onChange={(e) => atualizarFaixa(idx, { limiteSuperior: e.target.value })} />
                    )}
                  </FormField>
                </div>
                <div className="col-sm-3">
                  <FormField label="Valor (R$)">
                    {({ id }) => (
                      <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.valor} onChange={(e) => atualizarFaixa(idx, { valor: e.target.value })} />
                    )}
                  </FormField>
                </div>
                <div className="col-sm-3 mb-3">
                  <Button variant="tertiary" onClick={() => setFaixas((p) => p.filter((_, i) => i !== idx))} disabled={faixas.length === 1} aria-label={`Remover faixa ${idx + 1}`}>
                    <i className="fas fa-trash" aria-hidden="true" /> Remover
                  </Button>
                </div>
              </div>
            ))}
            <Button variant="secondary" onClick={() => setFaixas((p) => [...p, { ...FAIXA_VAZIA }])}>
              <i className="fas fa-plus" aria-hidden="true" /> Adicionar faixa
            </Button>
          </fieldset>
        )}
      </form>
    </Modal>
  );
}
