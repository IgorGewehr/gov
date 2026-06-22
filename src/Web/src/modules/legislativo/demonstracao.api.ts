// Camada de API da DEMONSTRACAO do modulo Legislativo. Expoe o seed que popula
// dados de exemplo (vereadores, sessao, proposicoes e votacao) para o PoC.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { legislativoKeys } from './legislativo.shared';

function semearDemonstracao(): Promise<void> {
  return http.post<void>('/legislativo/demonstracao/seed');
}

/**
 * Semeia a demonstracao (acao gated por `legislativo.demo.semear`). Ao concluir,
 * invalida TODO o cache do modulo para que as telas reflitam os dados criados.
 */
export function useSemearDemonstracao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: semearDemonstracao,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: legislativoKeys.all });
    },
  });
}
