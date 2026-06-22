// Barril dos modais de acao do ProntuarioSuas. Cada modal vive em seu proprio
// arquivo (< 300 linhas); este modulo reexporta para manter os imports estaveis.
//   - RegistrarAtendimentoModal   -> RegistrarAtendimentoCommand (mantem Aberto)
//   - EncerrarAcompanhamentoModal -> EncerrarAcompanhamentoCommand (destrutiva)
//   - RegistrarAcessoModal        -> RegistrarAcessoProntuarioCommand (append-only)
export { RegistrarAtendimentoModal } from './RegistrarAtendimentoModal';
export { EncerrarAcompanhamentoModal } from './EncerrarAcompanhamentoModal';
export { RegistrarAcessoModal } from './RegistrarAcessoModal';
