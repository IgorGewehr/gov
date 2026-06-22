// Formulário de PUBLICAÇÃO da tabela de alíquotas do IPTU (command
// PublicarTabelaAliquotas): regime ÚNICA ou PROGRESSIVA, com alíquotas distintas
// PREDIAL × TERRITORIAL. Percentuais digitados em % são convertidos para FRAÇÃO
// DECIMAL no payload. No regime progressivo expõe faixas por valor venal.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { REGIME_ALIQUOTA_VALOR, usePublicarAliquotas } from './iptu.api';
import type { FaixaAliquotaInput, PublicarAliquotasInput, RegimeAliquota } from './iptu.api';
import { REGIME_ALIQUOTA_LABEL, percentualParaFracao } from './iptu.helpers';

const ANO_ATUAL = new Date().getFullYear();

const REGIME_OPCOES: SelectOption[] = (['Unica', 'Progressiva'] as RegimeAliquota[]).map((r) => ({
  value: r,
  label: REGIME_ALIQUOTA_LABEL[r],
}));

export interface AliquotasFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FaixaLinha {
  ate: string;
  aliquota: string;
}

export function AliquotasFormModal({ open, onClose }: AliquotasFormModalProps) {
  const toast = useToast();
  const mutation = usePublicarAliquotas();

  const [exercicio, setExercicio] = useState(String(ANO_ATUAL));
  const [regime, setRegime] = useState<RegimeAliquota>('Unica');
  const [aliquotaPredial, setPredial] = useState('');
  const [aliquotaTerritorial, setTerritorial] = useState('');
  const [faixasPredial, setFaixasPredial] = useState<FaixaLinha[]>([]);
  const [faixasTerritorial, setFaixasTerritorial] = useState<FaixaLinha[]>([]);
  const [descontoCotaUnica, setDesconto] = useState('');
  const [quantidadeParcelas, setParcelas] = useState('1');
  const [erro, setErro] = useState<string | null>(null);

  const progressiva = regime === 'Progressiva';

  function fechar(): void {
    setExercicio(String(ANO_ATUAL));
    setRegime('Unica');
    setPredial('');
    setTerritorial('');
    setFaixasPredial([]);
    setFaixasTerritorial([]);
    setDesconto('');
    setParcelas('1');
    setErro(null);
    onClose();
  }

  function mapearFaixas(linhas: FaixaLinha[]): FaixaAliquotaInput[] {
    return linhas
      .filter((f) => f.ate.trim() !== '' && f.aliquota.trim() !== '')
      .map((f) => ({ ateValorVenal: Number(f.ate), aliquota: percentualParaFracao(f.aliquota) }));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido.');
      return;
    }
    const predial = percentualParaFracao(aliquotaPredial);
    const territorial = percentualParaFracao(aliquotaTerritorial);
    const fxPredial = progressiva ? mapearFaixas(faixasPredial) : [];
    const fxTerritorial = progressiva ? mapearFaixas(faixasTerritorial) : [];
    if (!progressiva && (Number.isNaN(predial) || predial <= 0 || Number.isNaN(territorial) || territorial <= 0)) {
      setErro('Informe as alíquotas predial e territorial (%) maiores que zero.');
      return;
    }
    if (progressiva && (fxPredial.length === 0 || fxTerritorial.length === 0)) {
      setErro('No regime progressivo, cadastre ao menos uma faixa predial e uma territorial.');
      return;
    }
    const desconto = descontoCotaUnica.trim() === '' ? 0 : percentualParaFracao(descontoCotaUnica);
    if (Number.isNaN(desconto) || desconto < 0 || desconto >= 1) {
      setErro('O desconto de cota única deve ser um percentual entre 0% e 100%.');
      return;
    }
    const parcelas = Number(quantidadeParcelas);
    if (!Number.isInteger(parcelas) || parcelas < 1 || parcelas > 12) {
      setErro('Informe de 1 a 12 parcelas.');
      return;
    }
    setErro(null);

    const input: PublicarAliquotasInput = {
      exercicio: ano,
      regime: REGIME_ALIQUOTA_VALOR[regime],
      aliquotaPredial: progressiva ? 0 : predial,
      aliquotaTerritorial: progressiva ? 0 : territorial,
      faixasPredial: fxPredial,
      faixasTerritorial: fxTerritorial,
      descontoCotaUnica: desconto,
      quantidadeParcelas: parcelas,
    };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Tabela de alíquotas ${ano} publicada (id ${r.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar as alíquotas.'),
    });
  }

  function renderFaixas(
    titulo: string,
    linhas: FaixaLinha[],
    setLinhas: React.Dispatch<React.SetStateAction<FaixaLinha[]>>,
  ) {
    return (
      <fieldset className="mb-3">
        <legend className="text-up-01 text-semi-bold">{titulo}</legend>
        {linhas.map((f, idx) => (
          <div className="row align-items-end" key={idx}>
            <div className="col-sm-6">
              <FormField label="Até valor venal (R$)">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.ate} onChange={(e) => setLinhas((p) => p.map((it, i) => (i === idx ? { ...it, ate: e.target.value } : it)))} />
                )}
              </FormField>
            </div>
            <div className="col-sm-4">
              <FormField label="Alíquota (%)">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.aliquota} placeholder="2,00" onChange={(e) => setLinhas((p) => p.map((it, i) => (i === idx ? { ...it, aliquota: e.target.value } : it)))} />
                )}
              </FormField>
            </div>
            <div className="col-sm-2 mb-3">
              <Button variant="tertiary" onClick={() => setLinhas((p) => p.filter((_, i) => i !== idx))} aria-label={`Remover faixa ${idx + 1}`}>
                <i className="fas fa-trash" aria-hidden="true" />
              </Button>
            </div>
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLinhas((p) => [...p, { ate: '', aliquota: '' }])}>
          <i className="fas fa-plus" aria-hidden="true" /> Adicionar faixa
        </Button>
      </fieldset>
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar tabela de alíquotas do IPTU"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-aliquotas" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-aliquotas" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <div className="row">
          <div className="col-md-6">
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Regime" required>
              {({ id, describedBy }) => (
                <Select id={id} aria-describedby={describedBy} options={REGIME_OPCOES} value={regime} onChange={(e) => setRegime(e.target.value as RegimeAliquota)} />
              )}
            </FormField>
          </div>
        </div>

        {progressiva ? (
          <>
            {renderFaixas('Faixas — predial', faixasPredial, setFaixasPredial)}
            {renderFaixas('Faixas — territorial', faixasTerritorial, setFaixasTerritorial)}
          </>
        ) : (
          <div className="row">
            <div className="col-md-6">
              <FormField label="Alíquota predial (%)" required help="Ex.: 1,00 para 1%.">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={aliquotaPredial} onChange={(e) => setPredial(e.target.value)} placeholder="1,00" />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Alíquota territorial (%)" required help="Imóveis sem construção.">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={aliquotaTerritorial} onChange={(e) => setTerritorial(e.target.value)} placeholder="3,00" />
                )}
              </FormField>
            </div>
          </div>
        )}

        <div className="row">
          <div className="col-md-6">
            <FormField label="Desconto cota única (%)" help="Opcional; 0% se não houver.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" max="100" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={descontoCotaUnica} onChange={(e) => setDesconto(e.target.value)} placeholder="10,00" />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Parcelas padrão" required help="Quantidade sugerida no lançamento (1 a 12).">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={1} max={12} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={quantidadeParcelas} onChange={(e) => setParcelas(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
