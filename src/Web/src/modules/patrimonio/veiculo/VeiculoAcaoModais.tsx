// Barrel dos modais de AÇÃO da DetailPage de Veiculo — um por command de frota.
// Cada command vive em ./acoes/<Command>Modal.tsx; este arquivo apenas reexporta
// para preservar a API pública de import. Helpers internos compartilhados ficam
// em ./acoes/acaoModais.shared.
//
// Commands cobertos:
//   RegistrarAbastecimento, AbrirOrdemServico, ConcluirManutencao,
//   RegistrarMulta, RegistrarLicenciamento, DesignarMotorista.
export { RegistrarAbastecimentoModal } from './acoes/RegistrarAbastecimentoModal';
export { AbrirOrdemServicoModal } from './acoes/AbrirOrdemServicoModal';
export { ConcluirManutencaoModal } from './acoes/ConcluirManutencaoModal';
export { RegistrarMultaModal } from './acoes/RegistrarMultaModal';
export { RegistrarLicenciamentoModal } from './acoes/RegistrarLicenciamentoModal';
export { DesignarMotoristaModal } from './acoes/DesignarMotoristaModal';
