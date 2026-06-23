// Query keys da Vigilância Sanitária (VISA) — fonte única para invalidação consistente.
export const visaKeys = {
  all: ['saude', 'vigilancia'] as const,
  estabelecimentos: () => [...visaKeys.all, 'estabelecimentos'] as const,
  estabelecimentosBusca: (filtro: unknown) =>
    [...visaKeys.estabelecimentos(), 'busca', filtro] as const,
  inspecoes: () => [...visaKeys.all, 'inspecoes'] as const,
  agenda: (de: string, ate: string, situacao: string | null) =>
    [...visaKeys.inspecoes(), 'agenda', de, ate, situacao] as const,
  inspecao: (id: string) => [...visaKeys.inspecoes(), 'detalhe', id] as const,
  autos: (status?: string | null) =>
    status === undefined ? [...visaKeys.all, 'autos'] as const : [...visaKeys.all, 'autos', status] as const,
  licencas: () => [...visaKeys.all, 'licencas'] as const,
  licencasEstab: (estabId: string) => [...visaKeys.licencas(), 'estabelecimento', estabId] as const,
  licencasAVencer: (dias: number | null) => [...visaKeys.licencas(), 'a-vencer', dias] as const,
};
