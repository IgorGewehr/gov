// Camada de API do BANCO DE HORAS no modulo RecursosHumanos.
// Contrato real: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.BancoDeHoras.cs
//   GET  /banco-de-horas/servidores/{id}                 -> extrato (saldo + lancamentos)
//   POST /banco-de-horas/servidores/{id}/lancamentos     -> lancamento manual -> { saldoMinutos }
//   POST /banco-de-horas/prescricao                      -> rotina de prescricao -> { minutosPrescritos }
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';

/** Natureza de um lancamento do banco de horas. */
export type TipoLancamentoBancoHoras = 'Credito' | 'Debito' | 'Prescricao';

/** Linha do extrato do banco de horas. */
export interface LancamentoBancoHorasView {
  tipo: TipoLancamentoBancoHoras;
  minutos: number;
  data: string;
  descricao: string;
}

/** Extrato do banco de horas de um servidor. */
export interface ExtratoBancoDeHorasView {
  servidorId: string;
  saldoMinutos: number;
  lancamentos: LancamentoBancoHorasView[];
}

/** Entrada de um lancamento manual. */
export interface LancarBancoHorasInput {
  minutos: number;
  credito: boolean;
  data: string;
  descricao: string;
}

const BASE = '/recursoshumanos/banco-de-horas';

/** Extrato (saldo + livro-razao) do banco de horas do servidor. */
export function useExtratoBancoDeHoras(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.bancoDeHoras(servidorId),
    queryFn: () => http.get<ExtratoBancoDeHorasView>(`${BASE}/servidores/${servidorId}`),
    enabled: servidorId.length > 0,
  });
}

/** Lancamento manual (credito/debito) no banco de horas. */
export function useLancarBancoDeHoras(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: LancarBancoHorasInput) =>
      http.post<{ saldoMinutos: number }>(`${BASE}/servidores/${servidorId}/lancamentos`, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.bancoDeHoras(servidorId) });
    },
  });
}

/** Executa a rotina de prescricao em lote. */
export function useExecutarPrescricaoBancoDeHoras() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => http.post<{ minutosPrescritos: number }>(`${BASE}/prescricao`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rhKeys.all });
    },
  });
}

/** Formata minutos como saldo legivel (ex.: "+12h 30min" / "-2h 00min"). */
export function formatarSaldoHoras(minutos: number): string {
  const sinal = minutos < 0 ? '-' : '+';
  const abs = Math.abs(minutos);
  const horas = Math.floor(abs / 60);
  const min = abs % 60;
  return `${sinal}${horas}h ${String(min).padStart(2, '0')}min`;
}
