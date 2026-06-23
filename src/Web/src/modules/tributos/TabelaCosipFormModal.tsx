// Formulário de CONFIGURAÇÃO da tabela de COSIP/CIP (ConfigurarTabelaCosipCommand —
// CF art. 149-A): exercício + fundamento legal + faixas de consumo (kWh) por CLASSE
// de consumidor, progressivas. Nenhum valor é hardcoded — tudo vem da LEI MUNICIPAL.
// Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  CLASSE_CONSUMIDOR_LABEL,
  CLASSE_CONSUMIDOR_VALOR,
  useConfigurarTabelaCosip,
} from './cosip.api';
import type { ClasseConsumidorCosip, ConfigurarTabelaCosipInput, FaixaCosipInput } from './cosip.api';

export interface TabelaCosipFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FaixaLinha {
  classe: ClasseConsumidorCosip;
  consumoMinimoKwh: string;
  consumoMaximoKwh: string;
  valor: string;
}

const FAIXA_VAZIA: FaixaLinha = {
  classe: 'Residencial',
  consumoMinimoKwh: '',
  consumoMaximoKwh: '',
  valor: '',
};

const CLASSE_OPCOES = (Object.keys(CLASSE_CONSUMIDOR_VALOR) as ClasseConsumidorCosip[]).map((k) => ({
  value: k,
  label: CLASSE_CONSUMIDOR_LABEL[k],
}));

export function TabelaCosipFormModal({ open, onClose }: TabelaCosipFormModalProps) {
  const toast = useToast();
  const mutation = useConfigurarTabelaCosip();

  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [fundamentoLegal, setFundamentoLegal] = useState('');
  const [faixas, setFaixas] = useState<FaixaLinha[]>([{ ...FAIXA_VAZIA }]);
  const [erro, setErro] = useState<string | null>(null);

  function atualizar(idx: number, patch: Partial<FaixaLinha>): void {
    setFaixas((p) => p.map((f, i) => (i === idx ? { ...f, ...patch } : f)));
  }

  function fechar(): void {
    setExercicio(String(new Date().getFullYear()));
    setFundamentoLegal('');
    setFaixas([{ ...FAIXA_VAZIA }]);
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido (≥ 1900).');
      return;
    }
    if (fundamentoLegal.trim() === '') {
      setErro('Informe o fundamento legal (lei municipal de COSIP).');
      return;
    }
    const validas = faixas.filter((f) => f.consumoMinimoKwh.trim() !== '' && f.valor.trim() !== '');
    if (validas.length === 0) {
      setErro('Cadastre ao menos uma faixa (consumo mínimo + valor).');
      return;
    }
    setErro(null);

    const faixasInput: FaixaCosipInput[] = validas.map((f) => ({
      classe: CLASSE_CONSUMIDOR_VALOR[f.classe],
      consumoMinimoKwh: Number(f.consumoMinimoKwh.replace(',', '.')),
      consumoMaximoKwh: f.consumoMaximoKwh.trim() === '' ? null : Number(f.consumoMaximoKwh.replace(',', '.')),
      valor: Number(f.valor.replace(',', '.')),
    }));

    const input: ConfigurarTabelaCosipInput = {
      exercicio: ano,
      fundamentoLegal: fundamentoLegal.trim(),
      faixas: faixasInput,
    };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Tabela de COSIP (exercício ${ano}) configurada (id ${r.id}).`, 'Sucesso');
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
      title="Configurar tabela de COSIP (CF art. 149-A)"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-tabela-cosip" loading={mutation.isPending}>
            Configurar
          </Button>
        </>
      }
    >
      <form id="form-tabela-cosip" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <div className="row">
          <div className="col-md-4">
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-8">
            <FormField label="Fundamento legal" required help="Lei municipal de COSIP/CIP.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fundamentoLegal} onChange={(e) => setFundamentoLegal(e.target.value)} placeholder="Lei Municipal nº 1.234/2026" />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Faixas de consumo por classe</legend>
          {faixas.map((faixa, idx) => (
            <div className="row align-items-end" key={idx}>
              <div className="col-sm-3">
                <FormField label="Classe">
                  {({ id }) => (
                    <Select id={id} options={CLASSE_OPCOES} value={faixa.classe} onChange={(e) => atualizar(idx, { classe: e.target.value as ClasseConsumidorCosip })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="Consumo mín. (kWh)">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.consumoMinimoKwh} onChange={(e) => atualizar(idx, { consumoMinimoKwh: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2">
                <FormField label="Máx. (kWh)" help="Vazio = sem teto.">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.consumoMaximoKwh} onChange={(e) => atualizar(idx, { consumoMaximoKwh: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2">
                <FormField label="Valor (R$)">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={faixa.valor} onChange={(e) => atualizar(idx, { valor: e.target.value })} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2 mb-3">
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
      </form>
    </Modal>
  );
}
