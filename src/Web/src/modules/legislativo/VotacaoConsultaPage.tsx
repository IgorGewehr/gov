// Tela de CONSULTA de Votacao (o backend expoe GET por id, sem listagem). Padrao
// de busca sob demanda: identificador -> link para o detalhe + abertura do
// formulario de inicio de votacao (mutation).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, EmptyState, FormField, Input, PageHeader } from '../../components/ui';
import { Can } from '../../auth/Can';
import { VotacaoFormModal } from './VotacaoFormModal';

export function VotacaoConsultaPage() {
  const navigate = useNavigate();
  const [votacaoId, setVotacaoId] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const id = votacaoId.trim();
    if (id !== '') navigate(`/legislativo/votacoes/${id}`);
  }

  return (
    <>
      <PageHeader
        title="Votações"
        description="Consulte o placar e o resultado de uma votação pelo identificador."
        actions={
          <Can permission="legislativo.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Iniciar votação
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador da votação" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={votacaoId}
                    onChange={(e) => setVotacaoId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={votacaoId.trim() === ''}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <EmptyState
        icon="fas fa-square-poll-vertical"
        title="Consulte uma votação"
        description="Informe o identificador da votação e clique em Consultar para ver o placar e o resultado."
      />

      <VotacaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
