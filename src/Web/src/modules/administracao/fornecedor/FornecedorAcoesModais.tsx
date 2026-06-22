// Barrel dos modais de ACAO (transicoes de estado) do agregado Fornecedor — um
// por command. Cada command vive em ./acoes/<Command>Modal.tsx; este arquivo
// apenas reexporta para preservar a API pública de import. Helpers internos
// compartilhados ficam em ./acoes/acoesModais.shared.
//
// Commands cobertos:
//   AplicarSancao -> AplicarSancaoCommand            (POST /fornecedores/{id}/sancoes)
//   AtualizarNivelSicaf -> AtualizarNivelSicafCommand (PUT .../nivel-sicaf)
//   Reabilitar -> ReabilitarFornecedorCommand        (confirmacao)
//   Inativar -> InativarFornecedorCommand            (confirmacao destrutiva)
export { AplicarSancaoModal } from './acoes/AplicarSancaoModal';
export { AtualizarNivelSicafModal } from './acoes/AtualizarNivelSicafModal';
export { ReabilitarModal } from './acoes/ReabilitarModal';
export { InativarModal } from './acoes/InativarModal';
