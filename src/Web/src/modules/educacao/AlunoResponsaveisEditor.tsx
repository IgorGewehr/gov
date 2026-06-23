// Editor de responsáveis (lista dinâmica) reutilizado no cadastro de aluno.
// Cada responsável: nome, CPF (opcional), parentesco, telefone, flags financeiro/buscar.
// LGPD art. 14: dados de menores; ao menos 1 responsável é recomendado para o EducaCenso.
import { Button, FormField, Input, Select } from '../../components/ui';
import { opcoesParentesco } from './educacao.helpers';
import type { ResponsavelInput } from './aluno.api';

export interface AlunoResponsaveisEditorProps {
  responsaveis: ResponsavelInput[];
  onChange: (proximos: ResponsavelInput[]) => void;
}

const RESPONSAVEL_VAZIO: ResponsavelInput = {
  nome: '',
  cpf: '',
  parentesco: 1,
  telefone: '',
  responsavelFinanceiro: false,
  autorizadoBuscar: true,
};

export function AlunoResponsaveisEditor({ responsaveis, onChange }: AlunoResponsaveisEditorProps) {
  function atualizar(indice: number, patch: Partial<ResponsavelInput>): void {
    onChange(responsaveis.map((r, i) => (i === indice ? { ...r, ...patch } : r)));
  }

  function remover(indice: number): void {
    onChange(responsaveis.filter((_, i) => i !== indice));
  }

  function adicionar(): void {
    onChange([...responsaveis, { ...RESPONSAVEL_VAZIO }]);
  }

  return (
    <fieldset className="mt-3">
      <legend className="text-up-01 text-bold">Responsáveis</legend>
      {responsaveis.length === 0 && (
        <p className="text-secondary text-down-01">Nenhum responsável adicionado.</p>
      )}

      {responsaveis.map((responsavel, indice) => (
        <div key={indice} className="br-card p-3 mb-3">
          <div className="row">
            <div className="col-12 col-md-6">
              <FormField label="Nome do responsável" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={responsavel.nome}
                    onChange={(e) => atualizar(indice, { nome: e.target.value })}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md-3">
              <FormField label="CPF" help="Opcional">
                {({ id }) => (
                  <Input
                    id={id}
                    value={responsavel.cpf ?? ''}
                    onChange={(e) => atualizar(indice, { cpf: e.target.value })}
                    placeholder="000.000.000-00"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md-3">
              <FormField label="Parentesco" required>
                {({ id }) => (
                  <Select
                    id={id}
                    options={opcoesParentesco}
                    value={String(responsavel.parentesco)}
                    onChange={(e) =>
                      atualizar(indice, {
                        parentesco: Number(e.target.value) as ResponsavelInput['parentesco'],
                      })
                    }
                  />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-12 col-md-4">
              <FormField label="Telefone" help="Opcional">
                {({ id }) => (
                  <Input
                    id={id}
                    value={responsavel.telefone ?? ''}
                    onChange={(e) => atualizar(indice, { telefone: e.target.value })}
                    placeholder="(00) 00000-0000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md-8 d-flex align-items-end flex-wrap">
              <div className="br-checkbox mr-4 mb-2">
                <input
                  id={`resp-fin-${indice}`}
                  type="checkbox"
                  checked={responsavel.responsavelFinanceiro}
                  onChange={(e) => atualizar(indice, { responsavelFinanceiro: e.target.checked })}
                />
                <label htmlFor={`resp-fin-${indice}`}>Responsável financeiro</label>
              </div>
              <div className="br-checkbox mb-2">
                <input
                  id={`resp-busca-${indice}`}
                  type="checkbox"
                  checked={responsavel.autorizadoBuscar}
                  onChange={(e) => atualizar(indice, { autorizadoBuscar: e.target.checked })}
                />
                <label htmlFor={`resp-busca-${indice}`}>Autorizado a buscar</label>
              </div>
            </div>
          </div>

          <div className="d-flex justify-content-end">
            <Button variant="secondary" className="small" onClick={() => remover(indice)}>
              <i className="fas fa-trash" aria-hidden="true" /> Remover
            </Button>
          </div>
        </div>
      ))}

      <Button variant="secondary" className="small" onClick={adicionar}>
        <i className="fas fa-plus" aria-hidden="true" /> Adicionar responsável
      </Button>
    </fieldset>
  );
}
