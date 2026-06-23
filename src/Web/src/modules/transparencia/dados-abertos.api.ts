// Camada de API da visão de DADOS ABERTOS / PORTAL PÚBLICO (LAI — dados abertos).
// IMPORTANTE: o que é PÚBLICO (catálogo + CSV) vive FORA de /api, no grupo anônimo
//   /publico/transparencia/{slug}/...  (TransparenciaPublicaEndpoints.cs):
//     GET /publico/transparencia/{slug}/dados-abertos            -> catálogo (dicionário)
//     GET /publico/transparencia/{slug}/dados-abertos/{ds}.csv   -> download CSV (stream)
// A ÚNICA superfície INTERNA (autenticada) é a configuração do portal:
//     POST /api/transparencia/portal-publico {slug,nomeEnte}      (perm transparencia.gerenciar)
//          -> { slug }  (slug efetivo, normalizado kebab-case)
// Esta tela INTERNA configura o slug e expõe os links públicos de download. O catálogo
// é um DICIONÁRIO FIXO (determinístico, sem I/O — CatalogoDadosAbertos no backend),
// espelhado aqui em dados-abertos.helpers.ts.
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

export interface ConfigurarPortalInput {
  slug: string;
  nomeEnte: string;
}

/** Resposta do POST de configuração — slug efetivo (normalizado pelo backend). */
export interface ConfigurarPortalResposta {
  slug: string;
}

function configurarPortal(input: ConfigurarPortalInput): Promise<ConfigurarPortalResposta> {
  return http.post<ConfigurarPortalResposta>('/transparencia/portal-publico', input);
}

/** Cria/atualiza o slug + nome de exibição do portal público do ente (gated). */
export function useConfigurarPortal() {
  return useMutation({ mutationFn: configurarPortal });
}

/** Base pública (fora de /api) onde o portal anônimo é servido. */
export const PORTAL_PUBLICO_BASE = '/publico/transparencia';

/** URL pública (anônima) da raiz do portal de um ente. */
export function urlPortalPublico(slug: string): string {
  return `${PORTAL_PUBLICO_BASE}/${encodeURIComponent(slug)}`;
}

/** URL pública de download CSV de um dataset (opcionalmente filtrado por exercício). */
export function urlDownloadCsv(slug: string, dataset: string, exercicio?: number | null): string {
  const base = `${urlPortalPublico(slug)}/dados-abertos/${encodeURIComponent(dataset)}.csv`;
  return exercicio ? `${base}?exercicio=${exercicio}` : base;
}
