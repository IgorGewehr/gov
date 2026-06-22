// Props comuns aos modais de ação da Matrícula (abertos da MatriculaListPage).
export interface MatriculaAcaoBaseProps {
  open: boolean;
  onClose: () => void;
  matriculaId: string;
  /** Aluno usado para invalidar a lista de matrículas após a ação. */
  alunoId: string;
}
