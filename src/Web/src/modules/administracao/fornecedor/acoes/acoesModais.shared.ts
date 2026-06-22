// Internos compartilhados pelos modais de ação do agregado Fornecedor.
// Extraído de FornecedorAcoesModais.tsx — comportamento idêntico.

export interface AcaoModalProps {
  open: boolean;
  onClose: () => void;
  fornecedorId: string;
}
