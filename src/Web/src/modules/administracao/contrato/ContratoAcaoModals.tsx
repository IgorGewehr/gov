// Barrel dos modais de AÇÃO por command do agregado Contrato, abertos a partir da
// DetailPage. Cada command vive em ./acoes/<Command>Modal.tsx; este arquivo apenas
// reexporta para preservar a API pública de import. Helpers internos compartilhados
// ficam em ./acoes/acaoModals.shared.
//
// Commands cobertos:
//   - PublicarContratoNoPncp  (PublicarNoPncpModal)
//   - CelebrarAditivo         (CelebrarAditivoModal)
//   - ApostilarContrato       (ApostilarModal)
//   - PrestarGarantia         (PrestarGarantiaModal)
//   - RescindirContrato       (RescindirModal — destrutivo)
//   - Confirmação simples     (ConfirmacaoModal)
export { PublicarNoPncpModal } from './acoes/PublicarNoPncpModal';
export { CelebrarAditivoModal } from './acoes/CelebrarAditivoModal';
export { ApostilarModal } from './acoes/ApostilarModal';
export { PrestarGarantiaModal } from './acoes/PrestarGarantiaModal';
export { RescindirModal } from './acoes/RescindirModal';
export { ConfirmacaoModal } from './acoes/ConfirmacaoModal';
export type { ConfirmacaoModalProps } from './acoes/ConfirmacaoModal';
