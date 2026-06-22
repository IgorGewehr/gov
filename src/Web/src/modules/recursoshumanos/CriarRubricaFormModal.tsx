// Formulário de criação de RUBRICA parametrizável da folha (verba S-1010), em Modal.
// Padrão-ouro: mutation + validação por campo + Toast + mapeamento de erros do backend.
// O valor pode ser fixo (R$) OU percentual (% sobre a base) — exclusivos entre si.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCriarRubrica } from './rubrica.api';
import type { CriarRubricaInput } from './rubrica.api';
import { NATUREZAS_RUBRICA } from './recursosHumanos.helpers';

export interface CriarRubricaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Competência consultada (pré-preenche a vigência da nova rubrica). */
  anoInicial: number;
  mesInicial: number;
}

interface FormErrors {
  codigo?: string;
  descricao?: string;
  natureza?: string;
  valor?: string;
}

interface Incidencias {
  incideInss: boolean;
  incideRpps: boolean;
  incideIrrf: boolean;
  incideFgts: boolean;
}

const INCIDENCIAS: { campo: keyof Incidencias; rotulo: string }[] = [
  { campo: 'incideInss', rotulo: 'Integra base de INSS' },
  { campo: 'incideRpps', rotulo: 'Integra base de RPPS' },
  { campo: 'incideIrrf', rotulo: 'Integra base do IRRF' },
  { campo: 'incideFgts', rotulo: 'Integra base do FGTS' },
];

export function CriarRubricaFormModal({
  open,
  onClose,
  anoInicial,
  mesInicial,
}: CriarRubricaFormModalProps) {
  const toast = useToast();
  const mutation = useCriarRubrica();

  const [codigo, setCodigo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [natureza, setNatureza] = useState('');
  const [modoValor, setModoValor] = useState<'nenhum' | 'fixo' | 'percentual'>('nenhum');
  const [valorFixo, setValorFixo] = useState('');
  const [percentual, setPercentual] = useState('');
  const [incidencias, setIncidencias] = useState<Incidencias>({
    incideInss: false,
    incideRpps: false,
    incideIrrf: false,
    incideFgts: false,
  });
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (codigo.trim() === '') next.codigo = 'Informe o código da rubrica.';
    if (descricao.trim() === '') next.descricao = 'Informe a descrição.';
    if (natureza.trim() === '') next.natureza = 'Selecione a natureza.';
    if (modoValor === 'fixo') {
      const v = Number(valorFixo);
      if (valorFixo.trim() === '' || Number.isNaN(v) || v <= 0)
        next.valor = 'Informe um valor fixo maior que zero.';
    } else if (modoValor === 'percentual') {
      const p = Number(percentual);
      if (percentual.trim() === '' || Number.isNaN(p) || p <= 0 || p > 100)
        next.valor = 'Informe um percentual entre 0 e 100.';
    }
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

    const input: CriarRubricaInput = {
      codigo: codigo.trim(),
      descricao: descricao.trim(),
      natureza: Number(natureza),
      anoVigencia: anoInicial,
      mesVigencia: mesInicial,
      ...incidencias,
      // Percentual é fração decimal (0..1): 14% -> 0,14. Mutuamente exclusivo com valor fixo.
      valorFixo: modoValor === 'fixo' ? Number(valorFixo) : null,
      percentual: modoValor === 'percentual' ? Number(percentual) / 100 : null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Rubrica ${input.codigo} criada.`, 'Sucesso');
        setCodigo('');
        setDescricao('');
        setNatureza('');
        setModoValor('nenhum');
        setValorFixo('');
        setPercentual('');
        setIncidencias({
          incideInss: false,
          incideRpps: false,
          incideIrrf: false,
          incideFgts: false,
        });
        setErrors({});
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível criar a rubrica.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Nova rubrica"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-criar-rubrica"
            loading={mutation.isPending}
          >
            Criar
          </Button>
        </>
      }
    >
      <form id="form-criar-rubrica" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Código (S-1010)" required error={errors.codigo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value)}
              maxLength={30}
            />
          )}
        </FormField>

        <FormField label="Descrição" required error={errors.descricao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              maxLength={200}
            />
          )}
        </FormField>

        <FormField label="Natureza" required error={errors.natureza}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={natureza}
              onChange={(e) => setNatureza(e.target.value)}
              placeholder="Selecione a natureza"
              options={NATUREZAS_RUBRICA}
            />
          )}
        </FormField>

        <FormField
          label="Vigência"
          help={`A rubrica passa a valer a partir de ${String(mesInicial).padStart(2, '0')}/${anoInicial}.`}
        >
          {() => (
            <p className="text-down-01 text-gray-60 mb-0">
              Competência {String(mesInicial).padStart(2, '0')}/{anoInicial} (consultada).
            </p>
          )}
        </FormField>

        <FormField label="Forma de cálculo" help="Valor fixo e percentual são exclusivos.">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={modoValor}
              onChange={(e) => setModoValor(e.target.value as 'nenhum' | 'fixo' | 'percentual')}
              options={[
                { value: 'nenhum', label: 'Sem valor predefinido (lançado por evento)' },
                { value: 'fixo', label: 'Valor fixo (R$)' },
                { value: 'percentual', label: 'Percentual sobre a base (%)' },
              ]}
            />
          )}
        </FormField>

        {modoValor === 'fixo' && (
          <FormField label="Valor fixo (R$)" required error={errors.valor}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                aria-describedby={describedBy}
                invalid={invalid}
                value={valorFixo}
                onChange={(e) => setValorFixo(e.target.value)}
              />
            )}
          </FormField>
        )}

        {modoValor === 'percentual' && (
          <FormField label="Percentual (%)" required error={errors.valor}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="number"
                min="0"
                max="100"
                step="0.01"
                inputMode="decimal"
                aria-describedby={describedBy}
                invalid={invalid}
                value={percentual}
                onChange={(e) => setPercentual(e.target.value)}
              />
            )}
          </FormField>
        )}

        <fieldset className="mt-2">
          <legend className="text-down-01 text-gray-60">Incidências (bases de cálculo)</legend>
          {INCIDENCIAS.map(({ campo, rotulo }) => (
            <div className="br-checkbox" key={campo}>
              <input
                id={`rubrica-${campo}`}
                type="checkbox"
                checked={incidencias[campo]}
                onChange={() =>
                  setIncidencias((prev) => ({ ...prev, [campo]: !prev[campo] }))
                }
              />
              <label htmlFor={`rubrica-${campo}`}>{rotulo}</label>
            </div>
          ))}
        </fieldset>
      </form>
    </Modal>
  );
}
