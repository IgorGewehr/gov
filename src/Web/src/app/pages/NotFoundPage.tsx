// Página 404 acessível.
import { Link } from 'react-router-dom';
import { EmptyState, PageHeader } from '../../components/ui';

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Página não encontrada" />
      <EmptyState
        icon="fas fa-compass"
        title="Não encontramos esta página"
        description="O endereço pode ter mudado ou não existe."
        action={
          <Link className="br-button primary" to="/">
            Voltar ao início
          </Link>
        }
      />
    </>
  );
}
