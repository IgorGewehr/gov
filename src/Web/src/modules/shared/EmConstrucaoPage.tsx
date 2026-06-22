// Página "Em construção" acessível, usada pelos stubs de módulo até que os
// agentes de módulo implementem as telas. Mantém o build e o registry fechados.
import { EmptyState, PageHeader } from '../../components/ui';

export interface EmConstrucaoPageProps {
  /** Nome do módulo, ex.: "Saúde". */
  modulo: string;
}

export function EmConstrucaoPage({ modulo }: EmConstrucaoPageProps) {
  return (
    <>
      <PageHeader title={modulo} description={`Módulo ${modulo} do Tensorroot.Gov.`} />
      <EmptyState
        icon="fas fa-helmet-safety"
        title="Em construção"
        description="Este módulo ainda está em desenvolvimento e estará disponível em breve."
      />
    </>
  );
}
