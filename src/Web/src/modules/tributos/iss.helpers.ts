// Helpers de apresentação do submódulo ISS.
import type { TagVariant } from '../../components/ui';
import type { FormaRecolhimentoIss } from './iss.api';

/** Rótulo PT-BR para a forma de recolhimento do ISS. */
export const FORMA_RECOLHIMENTO_LABEL: Record<FormaRecolhimentoIss, string> = {
  Proprio: 'Próprio',
  Retido: 'Retido na fonte',
  Substituicao: 'Substituição tributária',
};

/** Variante de Tag para a forma de recolhimento. */
export function formaRecolhimentoTagVariant(forma: FormaRecolhimentoIss): TagVariant {
  switch (forma) {
    case 'Proprio':
      return 'info';
    case 'Retido':
      return 'warning';
    case 'Substituicao':
      return 'success';
    default:
      return 'default';
  }
}
