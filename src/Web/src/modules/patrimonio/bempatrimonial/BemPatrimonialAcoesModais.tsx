// Barrel dos modais de AÇÃO (transições de estado) do agregado BemPatrimonial.
// Cada command vive em ./acoes/<Command>Modal.tsx; este arquivo apenas reexporta
// para preservar a API pública de import. Helpers internos compartilhados ficam
// em ./acoes/acoesModais.shared.
//
// Commands cobertos:
//   TombarBem, DepreciarBem, ReavaliarBem, RegistrarImpairment,
//   TransferirBem, CederBem, BaixarBem, AlienarBem.
export type { AcaoModalProps } from './acoes/acoesModais.shared';

export { TombarBemModal } from './acoes/TombarBemModal';
export { DepreciarBemModal } from './acoes/DepreciarBemModal';
export { ReavaliarBemModal } from './acoes/ReavaliarBemModal';
export { RegistrarImpairmentModal } from './acoes/RegistrarImpairmentModal';
export { TransferirBemModal } from './acoes/TransferirBemModal';
export { CederBemModal } from './acoes/CederBemModal';
export { BaixarBemModal } from './acoes/BaixarBemModal';
export { AlienarBemModal } from './acoes/AlienarBemModal';
