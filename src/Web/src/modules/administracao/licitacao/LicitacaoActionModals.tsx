// Barrel dos modais de AÇÃO (commands/transições) do agregado Licitacao,
// acionados na DetailPage. Cada command vive em ./acoes/<Command>Modal.tsx;
// este arquivo apenas reexporta para preservar a API pública de import.
// Helpers internos compartilhados ficam em ./acoes/licitacaoModais.shared e a
// base dos comandos com motivo obrigatório em ./acoes/MotivoModalBase.
//
// Commands cobertos:
//   - PublicarEditalPncpModal   -> PublicarEditalNoPncpCommand
//   - JulgarPropostasModal      -> JulgarPropostasCommand
//   - HabilitarLicitanteModal   -> HabilitarLicitanteCommand
//   - HomologarLicitacaoModal   -> HomologarLicitacaoCommand
//   - DeclararDesertaModal      -> DeclararLicitacaoDesertaCommand
//   - DeclararFracassadaModal   -> DeclararLicitacaoFracassadaCommand
//   - RevogarLicitacaoModal     -> RevogarLicitacaoCommand
//   - AnularLicitacaoModal      -> AnularLicitacaoCommand
export { PublicarEditalPncpModal } from './acoes/PublicarEditalPncpModal';
export { JulgarPropostasModal } from './acoes/JulgarPropostasModal';
export { HabilitarLicitanteModal } from './acoes/HabilitarLicitanteModal';
export { HomologarLicitacaoModal } from './acoes/HomologarLicitacaoModal';
export { DeclararDesertaModal } from './acoes/DeclararDesertaModal';
export { DeclararFracassadaModal } from './acoes/DeclararFracassadaModal';
export { RevogarLicitacaoModal } from './acoes/RevogarLicitacaoModal';
export { AnularLicitacaoModal } from './acoes/AnularLicitacaoModal';
