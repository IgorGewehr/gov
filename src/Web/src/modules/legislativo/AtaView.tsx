// ATA da sessao — gera/exibe a ata textual de uma sessao. Informa-se a sessao e
// o backend devolve a ata gerada (GET .../sessoes/{id}/ata). Exibicao acessivel
// com texto preformatado (preserva quebras) e data de geracao.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
} from '../../components/ui';
import { useAtaSessao } from './sessao.api';
import type { AtaSessao } from './sessao.api';
import { formatarDataHora } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';

export function AtaView() {
  const [sessaoInput, setSessaoInput] = useState('');
  const [sessaoId, setSessaoId] = useState('');
  const ata = useAtaSessao(sessaoId);

  function gerar(event: FormEvent): void {
    event.preventDefault();
    setSessaoId(sessaoInput.trim());
  }

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Ata da sessão"
        description="Gere e visualize a ata textual de uma sessão plenária."
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={gerar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" disabled={sessaoInput.trim() === ''}>
                Gerar ata
              </Button>
            }
          >
            <FormField label="Identificador da sessão" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={sessaoInput}
                  onChange={(e) => setSessaoInput(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {sessaoId === '' ? (
        <EmptyState
          icon="fas fa-file-lines"
          title="Gere uma ata"
          description="Informe o identificador da sessão e clique em Gerar ata para visualizar o documento."
        />
      ) : (
        <QueryState<AtaSessao>
          isLoading={ata.isLoading}
          isError={ata.isError}
          error={ata.error}
          data={ata.data}
        >
          {(dados) => (
            <Card
              header={<strong>Ata da sessão {dados.sessaoId}</strong>}
              footer={
                <small className="text-gray-60">
                  Gerada em {formatarDataHora(dados.geradaEm)}
                </small>
              }
            >
              <pre
                className="text-pre-wrap"
                style={{ whiteSpace: 'pre-wrap', fontFamily: 'inherit', margin: 0 }}
              >
                {dados.conteudo}
              </pre>
            </Card>
          )}
        </QueryState>
      )}
    </>
  );
}
