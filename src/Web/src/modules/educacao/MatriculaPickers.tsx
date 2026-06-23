// Pickers reais da Matrícula Inicial — substituem o GUID digitado.
//   AlunoPicker: busca por termo (GET /educacao/alunos?termo=) e seleciona da lista.
//   TurmaPicker: lista turmas ABERTAS da escola (GET /educacao/turmas) mostrando
//                VAGAS DISPONÍVEIS; a escolha define escolaId + turmaId coerentes.
import { useMemo, useState } from 'react';
import { Button, FormField, Input, Select, Spinner } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { useBuscarAlunos } from './aluno.api';
import type { AlunoItemLista } from './aluno.api';
import { useBuscarTurmas, SITUACAO_TURMA_VALOR } from './turma.api';
import type { TurmaItemLista } from './turma.api';

// --- Aluno picker ---

export interface AlunoPickerProps {
  selecionado: AlunoItemLista | null;
  onSelecionar: (aluno: AlunoItemLista | null) => void;
  error?: string;
}

export function AlunoPicker({ selecionado, onSelecionar, error }: AlunoPickerProps) {
  const [termoCampo, setTermoCampo] = useState('');
  const [termo, setTermo] = useState('');

  // Só busca alunos ATIVOS (situacao=1) — apenas eles podem ser matriculados.
  const query = useBuscarAlunos(
    { termo: termo || undefined, situacao: 1, pagina: 1, tamanho: 10 },
    termo.trim().length > 0,
  );

  if (selecionado) {
    return (
      <FormField label="Aluno" required>
        {() => (
          <div className="br-card p-3 d-flex justify-content-between align-items-center">
            <span>
              <strong>{selecionado.nomeSocial || selecionado.nome}</strong>
              <span className="d-block text-down-01 text-secondary">
                Nasc.: {formatarData(selecionado.dataNascimento)} · {selecionado.situacao}
              </span>
            </span>
            <Button variant="secondary" className="small" onClick={() => onSelecionar(null)}>
              <i className="fas fa-times" aria-hidden="true" /> Trocar
            </Button>
          </div>
        )}
      </FormField>
    );
  }

  return (
    <FormField label="Aluno" required error={error} help="Busque pelo nome (somente alunos ativos).">
      {({ id, describedBy, invalid }) => (
        <>
          <div className="d-flex">
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={termoCampo}
              onChange={(e) => setTermoCampo(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  setTermo(termoCampo.trim());
                }
              }}
              placeholder="Digite o nome do aluno"
            />
            <Button
              variant="secondary"
              className="ml-2"
              type="button"
              onClick={() => setTermo(termoCampo.trim())}
              loading={query.isFetching}
            >
              Buscar
            </Button>
          </div>

          {query.isFetching && <Spinner label="Buscando alunos..." />}
          {termo && !query.isFetching && (query.data?.itens.length ?? 0) === 0 && (
            <p className="text-secondary text-down-01 mt-2">Nenhum aluno ativo encontrado.</p>
          )}
          {(query.data?.itens.length ?? 0) > 0 && (
            <ul className="br-list mt-2" role="listbox" aria-label="Resultados da busca de alunos">
              {query.data?.itens.map((aluno) => (
                <li key={aluno.id} className="br-item">
                  <button
                    type="button"
                    className="br-button block text-left"
                    onClick={() => onSelecionar(aluno)}
                  >
                    {aluno.nomeSocial || aluno.nome}
                    <span className="text-down-01 text-secondary ml-2">
                      ({formatarData(aluno.dataNascimento)})
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </FormField>
  );
}

// --- Turma picker ---

export interface TurmaPickerProps {
  selecionada: TurmaItemLista | null;
  onSelecionar: (turma: TurmaItemLista | null) => void;
  escolaId?: string;
  error?: string;
}

export function TurmaPicker({ selecionada, onSelecionar, escolaId, error }: TurmaPickerProps) {
  // Lista apenas turmas ABERTAS (situacao=2) — as únicas que admitem enturmação.
  const query = useBuscarTurmas({
    escolaId: escolaId || undefined,
    situacao: SITUACAO_TURMA_VALOR.Aberta,
    pagina: 1,
    tamanho: 100,
  });

  const opcoes = useMemo<SelectOption[]>(
    () =>
      (query.data?.itens ?? []).map((t) => ({
        value: t.id,
        label: `${t.serie} · ${t.turno} · ${t.etapa} — ${t.vagasDisponiveis}/${t.vagas} vagas`,
        disabled: t.vagasDisponiveis <= 0,
      })),
    [query.data],
  );

  return (
    <FormField
      label="Turma"
      required
      error={error}
      help={
        query.isFetching
          ? 'Carregando turmas abertas...'
          : 'Apenas turmas abertas; turmas sem vaga aparecem desabilitadas.'
      }
    >
      {({ id, describedBy, invalid }) => (
        <>
          <Select
            id={id}
            aria-describedby={describedBy}
            invalid={invalid}
            options={opcoes}
            placeholder="Selecione a turma"
            value={selecionada?.id ?? ''}
            onChange={(e) => {
              const escolhida = (query.data?.itens ?? []).find((t) => t.id === e.target.value) ?? null;
              onSelecionar(escolhida);
            }}
          />
          {selecionada && (
            <p className="text-down-01 text-secondary mt-1">
              Vagas disponíveis: <strong>{selecionada.vagasDisponiveis}</strong> de {selecionada.vagas}
            </p>
          )}
          {!query.isFetching && opcoes.length === 0 && (
            <p className="text-secondary text-down-01 mt-2">Nenhuma turma aberta encontrada.</p>
          )}
        </>
      )}
    </FormField>
  );
}
