// Formulário de PUBLICAÇÃO das tabelas de alíquotas do IPTU. O backend modela UMA
// tabela por chamada (`edificado` define PREDIAL × TERRITORIAL) com FAIXAS de
// progressividade por valor venal. Este formulário coleta ambas as tabelas e faz
// DUAS chamadas (predial e territorial). Alíquotas em % (ex.: 1,00 = 1%) — o
// backend espera o valor percentual, não fração.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { usePublicarAliquotas } from './iptu.api';
import type { FaixaAliquotaInput, PublicarAliquotasInput } from './iptu.api';

const ANO_ATUAL = new Date().getFullYear();

export interface AliquotasFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FaixaLinha {
  minimo: string;
  maximo: string;
  aliquota: string;
}

const FAIXA_VAZIA: FaixaLinha = { minimo: '', maximo: '', aliquota: '' };

export function AliquotasFormModal({ open, onClose }: AliquotasFormModalProps) {
  const toast = useToast();
  const mutation = usePublicarAliquotas();

  const [exercicio, setExercicio] = useState(String(ANO_ATUAL));
  const [fundamentoLegal, setFundamentoLegal] = useState('');
  const [faixasPredial, setFaixasPredial] = useState<FaixaLinha[]>([{ ...FAIXA_VAZIA }]);
  const [faixasTerritorial, setFaixasTerritorial] = useState<FaixaLinha[]>([{ ...FAIXA_VAZIA }]);
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setExercicio(String(ANO_ATUAL));
    setFundamentoLegal('');
    setFaixasPredial([{ ...FAIXA_VAZIA }]);
    setFaixasTerritorial([{ ...FAIXA_VAZIA }]);
    setErro(null);
    onClose();
  }

  function mapearFaixas(linhas: FaixaLinha[]): FaixaAliquotaInput[] {
    return linhas
      .filter((f) => f.maximo.trim() !== '' && f.aliquota.trim() !== '')
      .map((f) => ({
        valorVenalMinimo: f.minimo.trim() === '' ? 0 : Number(f.minimo),
        valorVenalMaximo: Number(f.maximo),
        // O backend espera o percentual (1.0 = 1%), não a fração.
        aliquotaPercentual: Number(f.aliquota.replace(',', '.')),
      }));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido.');
      return;
    }
    if (fundamentoLegal.trim() === '') {
      setErro('Informe o fundamento legal (lei municipal de alíquotas).');
      return;
    }
    const predial = mapearFaixas(faixasPredial);
    const territorial = mapearFaixas(faixasTerritorial);
    if (predial.length === 0 || territorial.length === 0) {
      setErro('Cadastre ao menos uma faixa predial e uma territorial (valor venal máximo + alíquota).');
      return;
    }
    setErro(null);

    const base = { exercicio: ano, fundamentoLegal: fundamentoLegal.trim() } as const;
    const tabelaPredial: PublicarAliquotasInput = { ...base, edificado: true, faixas: predial };
    const tabelaTerritorial: PublicarAliquotasInput = { ...base, edificado: false, faixas: territorial };

    // Duas chamadas: predial primeiro, territorial em seguida.
    mutation.mutate(tabelaPredial, {
      onSuccess: () => {
        mutation.mutate(tabelaTerritorial, {
          onSuccess: () => {
            toast.success(`Tabelas de alíquotas ${ano} (predial e territorial) publicadas.`, 'Sucesso');
            fechar();
          },
          onError: (error) =>
            toast.error(
              error instanceof ApiError
                ? error.userMessage
                : 'Tabela predial publicada, mas falhou ao publicar a territorial.',
            ),
        });
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
            <div className="col-sm-4">
              <FormField label="Venal mínimo (R$)">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.minimo} placeholder="0,00" onChange={(e) => setLinhas((p) => p.map((it, i) => (i === idx ? { ...it, minimo: e.target.value } : it)))} />
                )}
              </FormField>
            </div>
            <div className="col-sm-4">
              <FormField label="Venal máximo (R$)">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.maximo} onChange={(e) => setLinhas((p) => p.map((it, i) => (i === idx ? { ...it, maximo: e.target.value } : it)))} />
                )}
              </FormField>
            </div>
            <div className="col-sm-3">
              <FormField label="Alíquota (%)">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.aliquota} placeholder="1,00" onChange={(e) => setLinhas((p) => p.map((it, i) => (i === idx ? { ...it, aliquota: e.target.value } : it)))} />
                )}
              </FormField>
            </div>
            <div className="col-sm-1 mb-3">
              <Button variant="tertiary" onClick={() => setLinhas((p) => p.filter((_, i) => i !== idx))} aria-label={`Remover faixa ${idx + 1}`}>
                <i className="fas fa-trash" aria-hidden="true" />
              </Button>
            </div>
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLinhas((p) => [...p, { ...FAIXA_VAZIA }])}>
          <i className="fas fa-plus" aria-hidden="true" /> Adicionar faixa
        </Button>
      </fieldset>
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar tabelas de alíquotas do IPTU"
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
          <div className="col-md-4">
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-8">
            <FormField label="Fundamento legal" required help="Lei municipal de alíquotas do IPTU.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fundamentoLegal} onChange={(e) => setFundamentoLegal(e.target.value)} placeholder="Lei Municipal nº 1.234/2026" />
              )}
            </FormField>
          </div>
        </div>

        {renderFaixas('Faixas — predial (edificado)', faixasPredial, setFaixasPredial)}
        {renderFaixas('Faixas — territorial (não edificado)', faixasTerritorial, setFaixasTerritorial)}
      </form>
    </Modal>
  );
}
