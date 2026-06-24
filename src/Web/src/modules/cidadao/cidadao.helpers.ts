// Formatadores e config do Portal do Cidadao (pt-BR; foco em clareza para o leigo).
import type { TagVariant } from '../../components/ui';

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

/** Formata um valor decimal como Real (R$ 1.234,56). */
export function formatarMoeda(valor: number): string {
  return moeda.format(valor);
}

/** Formata uma data ISO (AAAA-MM-DD) como dd/mm/aaaa, sem fuso (evita -1 dia). */
export function formatarData(iso: string): string {
  const [ano, mes, dia] = iso.slice(0, 10).split('-');
  if (!ano || !mes || !dia) return iso;
  return `${dia}/${mes}/${ano}`;
}

/** Formata a competencia fiscal AAAA-MM como MM/AAAA. */
export function formatarCompetencia(competencia: string): string {
  const [ano, mes] = competencia.split('-');
  return ano && mes ? `${mes}/${ano}` : competencia;
}

/** Mascara um CPF/CNPJ (somente digitos) para exibicao amigavel. */
export function formatarDocumento(digitos: string): string {
  const d = digitos.replace(/\D/g, '');
  if (d.length === 11) return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
  return digitos;
}

/** Cor da Tag para a situacao de um lancamento/divida (orientacao visual ao cidadao). */
export function situacaoTagVariant(situacao: string): TagVariant {
  const s = situacao.toLowerCase();
  if (s.includes('pag') || s.includes('quit') || s.includes('parcel')) return 'success';
  if (s.includes('venc') || s.includes('ativ') || s.includes('inscrit')) return 'warning';
  return 'info';
}

/**
 * Municipios habilitados ao cadastro/login local do cidadao. O backend resolve o tenant
 * a partir do tenantId enviado no payload (CidadaoEndpoints.cs). Como nao ha endpoint
 * publico de listagem de municipios, a lista vem da env VITE_CIDADAO_MUNICIPIOS
 * ("Nome|guid;Nome|guid"); o fallback e o municipio piloto (config em runtime futura).
 */
export interface MunicipioOpcao {
  nome: string;
  tenantId: string;
}

export function municipiosDisponiveis(): MunicipioOpcao[] {
  const bruto = import.meta.env.VITE_CIDADAO_MUNICIPIOS as string | undefined;
  if (bruto && bruto.trim() !== '') {
    return bruto
      .split(';')
      .map((par) => par.split('|'))
      .filter(([nome, id]) => nome?.trim() && id?.trim())
      .map(([nome, id]) => ({ nome: nome.trim(), tenantId: id.trim() }));
  }
  return [];
}
