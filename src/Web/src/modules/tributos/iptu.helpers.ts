// Helpers de apresentação do submódulo IPTU / Cadastro Imobiliário.
import type { TagVariant } from '../../components/ui';
import type { PadraoConstrutivo, UsoImovel } from './iptu.api';

/** Rótulo PT-BR para o uso predominante do imóvel. */
export const USO_IMOVEL_LABEL: Record<UsoImovel, string> = {
  Residencial: 'Residencial',
  Comercial: 'Comercial',
  Industrial: 'Industrial',
  Servicos: 'Serviços',
  Misto: 'Misto',
  Territorial: 'Territorial (sem construção)',
};

/** Rótulo PT-BR para o padrão construtivo (string livre no backend). */
export const PADRAO_CONSTRUTIVO_LABEL: Record<PadraoConstrutivo, string> = {
  Baixo: 'Baixo',
  Normal: 'Normal',
  Alto: 'Alto',
  Luxo: 'Luxo',
};

/** Variante de Tag para o uso (territorial em destaque). */
export function usoTagVariant(uso: UsoImovel): TagVariant {
  switch (uso) {
    case 'Residencial':
      return 'success';
    case 'Comercial':
    case 'Servicos':
      return 'info';
    case 'Industrial':
      return 'warning';
    case 'Misto':
      return 'info';
    case 'Territorial':
      return 'default';
    default:
      return 'default';
  }
}

const percentual = new Intl.NumberFormat('pt-BR', {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 4,
});

const area = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Formata uma fração decimal (0,02) como percentual pt-BR (2,00%). */
export function formatarPercentual(fracao: number): string {
  return percentual.format(fracao);
}

/** Formata uma área em m² (pt-BR) com sufixo. */
export function formatarArea(valor: number): string {
  return `${area.format(valor)} m²`;
}

/**
 * Converte um percentual digitado pelo usuário (ex.: "2" ou "2,5" => 2,5%) na
 * FRAÇÃO DECIMAL esperada pelo backend (0,025). Aceita vírgula ou ponto.
 */
export function percentualParaFracao(texto: string): number {
  const normalizado = Number(texto.replace(',', '.'));
  return Number.isFinite(normalizado) ? normalizado / 100 : NaN;
}
