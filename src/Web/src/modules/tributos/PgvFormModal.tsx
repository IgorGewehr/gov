// Formulário de PUBLICAÇÃO da Planta Genérica de Valores (command
// PublicarPlantaDeValores): para um exercício, configura ZONAS (VUT por m² de
// terreno; VUC por m² construído) e FATORES de correção (fração decimal). Listas
// dinâmicas (adicionar/remover linha). Acessível (Modal, foco preso).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { usePublicarPgv } from './iptu.api';
import type { FatorPgvInput, PublicarPgvInput, ZonaPgvInput } from './iptu.api';

const ANO_ATUAL = new Date().getFullYear();

/** Categorias de fator de correção da PGV (enum TipoFatorPgv do backend). */
const TIPOS_FATOR_PGV = [
  { value: 1, label: 'Padrão construtivo' },
  { value: 2, label: 'Depreciação' },
  { value: 3, label: 'Uso/Localização' },
] as const;

export interface PgvFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface ZonaLinha {
  zona: string;
  vut: string;
  vuc: string;
}
interface FatorLinha {
  tipo: string;
  chave: string;
  fator: string;
}

const ZONA_VAZIA: ZonaLinha = { zona: '', vut: '', vuc: '' };
const FATOR_VAZIO: FatorLinha = { tipo: String(TIPOS_FATOR_PGV[0].value), chave: '', fator: '' };

export function PgvFormModal({ open, onClose }: PgvFormModalProps) {
  const toast = useToast();
  const mutation = usePublicarPgv();

  const [exercicio, setExercicio] = useState(String(ANO_ATUAL));
  const [fundamentoLegal, setFundamentoLegal] = useState('');
  const [zonas, setZonas] = useState<ZonaLinha[]>([{ ...ZONA_VAZIA }]);
  const [fatores, setFatores] = useState<FatorLinha[]>([]);
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setExercicio(String(ANO_ATUAL));
    setFundamentoLegal('');
    setZonas([{ ...ZONA_VAZIA }]);
    setFatores([]);
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    const zonasValidas = zonas.filter((z) => z.zona.trim() !== '');
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido.');
      return;
    }
    if (fundamentoLegal.trim() === '') {
      setErro('Informe o fundamento legal (lei/decreto municipal da PGV).');
      return;
    }
    if (zonasValidas.length === 0) {
      setErro('Cadastre ao menos uma zona com VUT e VUC.');
      return;
    }
    const zonasInput: ZonaPgvInput[] = zonasValidas.map((z) => ({
      zonaFiscal: z.zona.trim(),
      valorM2Terreno: Number(z.vut),
      valorM2Construcao: Number(z.vuc),
    }));
    if (
      zonasInput.some(
        (z) =>
          Number.isNaN(z.valorM2Terreno) ||
          z.valorM2Terreno < 0 ||
          Number.isNaN(z.valorM2Construcao) ||
          z.valorM2Construcao < 0,
      )
    ) {
      setErro('VUT e VUC devem ser números maiores ou iguais a zero.');
      return;
    }
    const fatoresInput: FatorPgvInput[] = fatores
      .filter((f) => f.chave.trim() !== '')
      .map((f) => ({ tipo: Number(f.tipo), chave: f.chave.trim(), multiplicador: Number(f.fator) }));
    if (fatoresInput.some((f) => Number.isNaN(f.multiplicador) || f.multiplicador <= 0)) {
      setErro('Os fatores (multiplicadores) devem ser maiores que zero (ex.: 0,9; 1,1).');
      return;
    }
    setErro(null);

    const input: PublicarPgvInput = {
      exercicio: ano,
      fundamentoLegal: fundamentoLegal.trim(),
      zonas: zonasInput,
      fatores: fatoresInput,
    };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Planta de Valores ${ano} publicada (id ${r.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar a PGV.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar Planta de Valores (PGV)"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-pgv" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-pgv" className="br-form" onSubmit={submeter} noValidate>
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
            <FormField label="Fundamento legal" required help="Lei/decreto municipal que institui a PGV.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fundamentoLegal} onChange={(e) => setFundamentoLegal(e.target.value)} placeholder="Lei Municipal nº 1.234/2026" />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Zonas (VUT/VUC por m²)</legend>
          {zonas.map((z, idx) => (
            <div className="row align-items-end" key={idx}>
              <div className="col-sm-3">
                <FormField label="Zona">
                  {({ id }) => (
                    <Input id={id} aria-label="Zona" value={z.zona} placeholder="Z01" onChange={(e) => setZonas((p) => p.map((it, i) => (i === idx ? { ...it, zona: e.target.value } : it)))} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="VUT (R$/m² terreno)">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={z.vut} onChange={(e) => setZonas((p) => p.map((it, i) => (i === idx ? { ...it, vut: e.target.value } : it)))} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="VUC (R$/m² construído)">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={z.vuc} onChange={(e) => setZonas((p) => p.map((it, i) => (i === idx ? { ...it, vuc: e.target.value } : it)))} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3 mb-3">
                <Button variant="tertiary" onClick={() => setZonas((p) => p.filter((_, i) => i !== idx))} disabled={zonas.length === 1} aria-label={`Remover zona ${idx + 1}`}>
                  <i className="fas fa-trash" aria-hidden="true" /> Remover
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={() => setZonas((p) => [...p, { ...ZONA_VAZIA }])}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar zona
          </Button>
        </fieldset>

        <fieldset>
          <legend className="text-up-01 text-semi-bold">Fatores de correção (opcional)</legend>
          {fatores.map((f, idx) => (
            <div className="row align-items-end" key={idx}>
              <div className="col-sm-3">
                <FormField label="Chave">
                  {({ id }) => (
                    <Input id={id} value={f.chave} placeholder="esquina" onChange={(e) => setFatores((p) => p.map((it, i) => (i === idx ? { ...it, chave: e.target.value } : it)))} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-5">
                <FormField label="Tipo">
                  {({ id }) => (
                    <Select
                      id={id}
                      value={f.tipo}
                      options={TIPOS_FATOR_PGV.map((t) => ({ value: String(t.value), label: t.label }))}
                      onChange={(e) => setFatores((p) => p.map((it, i) => (i === idx ? { ...it, tipo: e.target.value } : it)))}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2">
                <FormField label="Fator">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={f.fator} placeholder="1,10" onChange={(e) => setFatores((p) => p.map((it, i) => (i === idx ? { ...it, fator: e.target.value } : it)))} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2 mb-3">
                <Button variant="tertiary" onClick={() => setFatores((p) => p.filter((_, i) => i !== idx))} aria-label={`Remover fator ${idx + 1}`}>
                  <i className="fas fa-trash" aria-hidden="true" /> Remover
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={() => setFatores((p) => [...p, { ...FATOR_VAZIO }])}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar fator
          </Button>
        </fieldset>
      </form>
    </Modal>
  );
}
