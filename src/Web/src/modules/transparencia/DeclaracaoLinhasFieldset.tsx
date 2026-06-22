// Editor das linhas da Matriz de Saldos Contábeis (PCASP) da Declaração Fiscal.
// Extraído do FormModal para manter cada arquivo < 300 linhas (CLAUDE.md §13).
import { Button, FormField, Input, Select, Textarea } from '../../components/ui';
import type { NaturezaSaldo } from './api';

export interface LinhaForm {
  contaPcasp: string;
  naturezaSaldo: NaturezaSaldo;
  valor: string;
  informacaoComplementar: string;
}

export function linhaVazia(): LinhaForm {
  return { contaPcasp: '', naturezaSaldo: 1, valor: '', informacaoComplementar: '' };
}

const naturezaOptions = [
  { value: '1', label: 'Devedor' },
  { value: '2', label: 'Credor' },
];

export interface DeclaracaoLinhasFieldsetProps {
  linhas: LinhaForm[];
  erro?: string;
  onAtualizar: (index: number, patch: Partial<LinhaForm>) => void;
  onAdicionar: () => void;
  onRemover: (index: number) => void;
}

export function DeclaracaoLinhasFieldset({
  linhas,
  erro,
  onAtualizar,
  onAdicionar,
  onRemover,
}: DeclaracaoLinhasFieldsetProps) {
  return (
    <fieldset className="br-fieldset">
      <legend className="text-semi-bold mb-2">Linhas da Matriz de Saldos Contábeis (PCASP)</legend>
      {erro && (
        <span className="feedback danger d-block mb-2" role="alert">
          <i className="fas fa-times-circle" aria-hidden="true" /> {erro}
        </span>
      )}

      {linhas.map((linha, index) => (
        <div className="row align-items-end mb-2" key={index}>
          <div className="col-12 col-sm-3">
            <FormField label="Conta PCASP" required>
              {({ id }) => (
                <Input
                  id={id}
                  value={linha.contaPcasp}
                  onChange={(e) => onAtualizar(index, { contaPcasp: e.target.value })}
                  placeholder="1.1.1.1.1.01.00"
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-3">
            <FormField label="Natureza">
              {({ id }) => (
                <Select
                  id={id}
                  options={naturezaOptions}
                  value={String(linha.naturezaSaldo)}
                  onChange={(e) =>
                    onAtualizar(index, { naturezaSaldo: Number(e.target.value) as NaturezaSaldo })
                  }
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-3">
            <FormField label="Valor (R$)" required>
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={linha.valor}
                  onChange={(e) => onAtualizar(index, { valor: e.target.value })}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-3 mb-3">
            <Button
              variant="secondary"
              onClick={() => onRemover(index)}
              disabled={linhas.length === 1}
              aria-label={`Remover linha ${index + 1}`}
            >
              <i className="fas fa-trash" aria-hidden="true" /> Remover
            </Button>
          </div>
          <div className="col-12">
            <FormField label="Informação complementar" help="Opcional.">
              {({ id, describedBy }) => (
                <Textarea
                  id={id}
                  rows={2}
                  aria-describedby={describedBy}
                  value={linha.informacaoComplementar}
                  onChange={(e) => onAtualizar(index, { informacaoComplementar: e.target.value })}
                />
              )}
            </FormField>
          </div>
        </div>
      ))}

      <Button variant="secondary" onClick={onAdicionar}>
        <i className="fas fa-plus" aria-hidden="true" /> Adicionar linha
      </Button>
    </fieldset>
  );
}
