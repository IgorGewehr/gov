// Geração da remessa bancária CNAB240 (FEBRABAN) a partir de uma ordem de pagamento.
// POST /financas/ordens-pagamento/{id}/cnab240 -> arquivo .rem (download direto).
import { getAccessToken } from '../../api/authToken';

export interface GerarCnabInput {
  codigoBanco: string;
  tipoInscricao: number;
  numeroInscricao: string;
  convenio: string;
  dvAgencia?: string;
  dvConta?: string;
  dvAgenciaConta?: string;
  nomeEmpresa: string;
  formaLancamento: number;
  sequencialArquivo: number;
}

/**
 * Gera o arquivo de remessa CNAB240 e dispara o download. A transmissão real ao banco é diferida (M10).
 */
export async function gerarCnab240(ordemId: string, input: GerarCnabInput): Promise<void> {
  const token = getAccessToken();
  const resposta = await fetch(`/api/financas/ordens-pagamento/${ordemId}/cnab240`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(input),
  });

  if (!resposta.ok) {
    let detalhe = `Falha ao gerar a remessa CNAB240 (HTTP ${resposta.status}).`;
    try {
      const corpo = (await resposta.json()) as { detail?: string; title?: string };
      detalhe = corpo.detail ?? corpo.title ?? detalhe;
    } catch {
      // resposta sem corpo JSON — mantém o detalhe padrão.
    }
    throw new Error(detalhe);
  }

  const blob = await resposta.blob();
  const disposicao = resposta.headers.get('Content-Disposition') ?? '';
  const correspondencia = /filename="?([^";]+)"?/.exec(disposicao);
  const nome = correspondencia?.[1] ?? `CNAB240_${ordemId}.rem`;

  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = nome;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
