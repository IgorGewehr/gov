// Seletor de permissões agrupado por módulo (checkboxes gov.br br-checkbox),
// compartilhado pelo PapelFormModal (criação) e pelo PapelPermissoesModal (edição).
// Carrega o catálogo canônico (GET /api/identidade/permissoes), agrupa por prefixo
// de módulo e expõe seleção/deseleção individual e "todas do grupo".
import { useId, useMemo } from 'react';
import { Alert, Spinner } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { useCatalogoPermissoes } from './papel.api';
import { agruparPermissoes } from './papel.helpers';

export interface PermissoesCheckboxGroupProps {
  /** Conjunto de chaves de permissão selecionadas. */
  selecionadas: Set<string>;
  /** Callback ao alternar UMA permissão. */
  onToggle: (chave: string) => void;
  /** Callback ao marcar/desmarcar TODAS as permissões de um grupo. */
  onToggleGrupo: (chaves: string[], marcar: boolean) => void;
  /** Carrega o catálogo somente quando true (modal aberto). */
  enabled?: boolean;
}

export function PermissoesCheckboxGroup({
  selecionadas,
  onToggle,
  onToggleGrupo,
  enabled = true,
}: PermissoesCheckboxGroupProps) {
  const baseId = useId();
  const query = useCatalogoPermissoes(enabled);
  const grupos = useMemo(() => agruparPermissoes(query.data ?? []), [query.data]);

  if (query.isLoading) {
    return (
      <div className="app-center">
        <Spinner label="Carregando catálogo de permissões…" />
      </div>
    );
  }

  if (query.isError) {
    return <Alert variant="danger">{errorMessage(query.error)}</Alert>;
  }

  if (grupos.length === 0) {
    return <Alert variant="info">Nenhuma permissão disponível no catálogo.</Alert>;
  }

  return (
    <fieldset className="border-0 p-0 m-0">
      <legend className="sr-only">Permissões do papel agrupadas por módulo</legend>
      {grupos.map((grupo) => {
        const chaves = grupo.permissoes.map((p) => p.chave);
        const todasMarcadas = chaves.every((c) => selecionadas.has(c));
        const grupoId = `${baseId}-grupo-${grupo.prefixo}`;
        return (
          <div className="mb-3" key={grupo.prefixo}>
            <div className="d-flex align-items-center justify-content-between mb-1">
              <h3 className="text-up-01 text-weight-semi-bold mb-0">
                {grupo.label} <span className="text-base">({grupo.prefixo}.*)</span>
              </h3>
              <div className="br-checkbox">
                <input
                  id={`${grupoId}-todos`}
                  type="checkbox"
                  checked={todasMarcadas}
                  onChange={(e) => onToggleGrupo(chaves, e.target.checked)}
                />
                <label htmlFor={`${grupoId}-todos`}>Selecionar todas</label>
              </div>
            </div>
            <div className="row">
              {grupo.permissoes.map((permissao) => {
                const inputId = `${grupoId}-${permissao.chave}`;
                return (
                  <div className="col-12 col-md-6" key={permissao.chave}>
                    <div className="br-checkbox">
                      <input
                        id={inputId}
                        type="checkbox"
                        checked={selecionadas.has(permissao.chave)}
                        onChange={() => onToggle(permissao.chave)}
                      />
                      <label htmlFor={inputId}>
                        {permissao.chave}
                        {permissao.descricao ? (
                          <span className="d-block text-base text-down-01">{permissao.descricao}</span>
                        ) : null}
                      </label>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        );
      })}
    </fieldset>
  );
}
