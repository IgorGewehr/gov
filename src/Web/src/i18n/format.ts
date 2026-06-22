// Formatação centralizada pt-BR (DS §6 — proibida string de locale solta no JSX).
const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const data = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
const dataHora = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
});

/** Formata valor numérico em Real (R$). */
export function formatarMoeda(valor: number): string {
  return moeda.format(valor);
}

/** Formata uma data ISO (yyyy-mm-dd / ISO 8601) em dd/mm/aaaa. */
export function formatarData(iso: string | null | undefined): string {
  if (!iso) return '—';
  const parsed = new Date(iso);
  return Number.isNaN(parsed.getTime()) ? '—' : data.format(parsed);
}

/** Formata um instante ISO 8601 (UTC) em dd/mm/aaaa hh:mm:ss no fuso local. */
export function formatarDataHora(iso: string | null | undefined): string {
  if (!iso) return '—';
  const parsed = new Date(iso);
  return Number.isNaN(parsed.getTime()) ? '—' : dataHora.format(parsed);
}
