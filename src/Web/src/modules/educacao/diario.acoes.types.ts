// Props comuns aos modais de ação do Diário de Classe (abertos da DiarioClasseDetailPage).
export interface DiarioAcaoBaseProps {
  open: boolean;
  onClose: () => void;
  matriculaId: string;
  diarioId: string;
}
