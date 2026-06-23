// Camada de API do NÚCLEO FISCAL dos mínimos constitucionais (Onda 0 — navegabilidade).
// Contrato real (confirmado no backend):
//   - GET /api/transparencia/fiscal/minimos?exercicio=  (perm transparencia.ver)
//       -> ApuracaoMinimoResultado[] — semáforo Saúde/Educação.
//         src/Modules/Transparencia/.../Fiscal/ApurarMinimos.cs (ApuracaoMinimoResultado).
//   - GET /api/saude/fiscal/asps/{exercicio}             (perm saude.ver)
//       -> ApuracaoAspsResultado — complemento ASPS 15% (LC 141/2012).
//         src/Modules/Saude/.../Fiscal/ApurarAsps.cs (ApuracaoAspsResultado).
// Enums serializam como STRING (JsonStringEnumConverter no ApiHost/Program.cs); nomes de
// propriedade em camelCase (default System.Text.Json). NÃO há criação/mutação aqui (read-only).
import { useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';
import { transparenciaKeys } from './keys';

// ---------------------------------------------------------------------------
// DTOs (espelham os records do backend)
// ---------------------------------------------------------------------------

/** Setor sujeito a mínimo constitucional (função de governo MOG 42/1999). */
export type SetorMinimo = 'Saude' | 'Educacao';

/** Situação da aferição de um mínimo (atingido / não atingido). */
export type SituacaoMinimo = 'Atingido' | 'NaoAtingido';

/**
 * Linha do semáforo de um mínimo constitucional apurado para o exercício.
 * Percentuais vêm como fração 0..1 (ex.: 0,1532 = 15,32%).
 */
export interface ApuracaoMinimoResultado {
  setor: SetorMinimo;
  receitaBase: number;
  aplicado: number;
  percentualAplicado: number;
  percentualMinimo: number;
  situacao: SituacaoMinimo;
}

/** Complemento ASPS (Saúde 15%, LC 141/2012) — `atingido` já vem resolvido (bool). */
export interface ApuracaoAspsResultado {
  exercicio: number;
  receitaBase: number;
  aplicadoAsps: number;
  percentualAplicado: number;
  percentualMinimo: number;
  atingido: boolean;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function apurarMinimos(
  exercicio: number,
  signal?: AbortSignal,
): Promise<ApuracaoMinimoResultado[]> {
  return http.get<ApuracaoMinimoResultado[]>('/transparencia/fiscal/minimos', {
    query: { exercicio },
    signal,
  });
}

function apurarAsps(exercicio: number, signal?: AbortSignal): Promise<ApuracaoAspsResultado> {
  return http.get<ApuracaoAspsResultado>(`/saude/fiscal/asps/${exercicio}`, { signal });
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

function exercicioValido(exercicio: number): boolean {
  return Number.isInteger(exercicio) && exercicio > 0;
}

/** Semáforo dos mínimos constitucionais (Saúde/Educação) de um exercício. */
export function useMinimosConstitucionais(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: transparenciaKeys.minimos(exercicio),
    queryFn: ({ signal }) => apurarMinimos(exercicio, signal),
    enabled: enabled && exercicioValido(exercicio),
  });
}

/**
 * Complemento ASPS (Saúde 15%). Servido por OUTRO módulo (Saúde, perm `saude.ver`):
 * tenant pode não ter Saúde licenciada / usuário pode não ter a permissão — por isso
 * o consumo é OPCIONAL e a tela degrada graciosamente quando a query falha.
 */
export function useAspsComplemento(exercicio: number, enabled = true) {
  return useQuery({
    queryKey: transparenciaKeys.asps(exercicio),
    queryFn: ({ signal }) => apurarAsps(exercicio, signal),
    enabled: enabled && exercicioValido(exercicio),
    retry: false,
  });
}
