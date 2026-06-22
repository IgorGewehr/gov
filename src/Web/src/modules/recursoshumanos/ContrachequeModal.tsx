// Modal de CONSULTA do contracheque de um servidor numa folha (GET contracheque).
// Seleciona o servidor (ativos do tenant) e dispara a query sob demanda, exibindo as
// linhas (rubricas) e os totais. Padrão-ouro: QueryState + DataTable, somente leitura.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  FormField,
  Modal,
  QueryState,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { useContracheque } from './folha.api';
import type { Contracheque, LinhaContracheque } from './folha.api';
import { useServidoresAtivos } from './servidor.api';

export interface ContrachequeModalProps {
  open: boolean;
  onClose: () => void;
  folhaId: string;
}

export function ContrachequeModal({ open, onClose, folhaId }: ContrachequeModalProps) {
  const servidoresQuery = useServidoresAtivos();
  const [servidorSel, setServidorSel] = useState('');
  const [servidorId, setServidorId] = useState('');

  const query = useContracheque(folhaId, servidorId, servidorId !== '');

  const servidorOptions = (servidoresQuery.data ?? []).map((s) => ({
    value: s.id,
    label: `${s.matricula} — ${s.nomeServidor}`,
  }));

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setServidorId(servidorSel);
  }

  function fechar(): void {
    setServidorSel('');
    setServidorId('');
    onClose();
  }

  const columns: Column<LinhaContracheque>[] = [
    { key: 'rubrica', header: 'Rubrica', render: (l) => l.rubrica },
    {
      key: 'tipo',
      header: 'Tipo',
      render: (l) => (
        <Tag variant={l.tipo === 'Provento' ? 'success' : 'danger'}>{l.tipo}</Tag>
      ),
    },
    { key: 'valor', header: 'Valor', align: 'end', render: (l) => formatarMoeda(l.valor) },
  ];

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Contracheque do servidor"
      footer={
        <Button variant="secondary" onClick={fechar}>
          Fechar
        </Button>
      }
    >
      <form className="br-form mb-3" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col">
            <FormField
              label="Servidor"
              required
              help={servidoresQuery.isLoading ? 'Carregando servidores…' : undefined}
            >
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={servidorSel}
                  onChange={(e) => setServidorSel(e.target.value)}
                  placeholder="Selecione o servidor"
                  options={servidorOptions}
                  disabled={servidoresQuery.isLoading}
                />
              )}
            </FormField>
          </div>
          <div className="col-auto mb-3">
            <Button
              variant="primary"
              type="submit"
              loading={query.isFetching}
              disabled={servidorSel === ''}
            >
              Consultar
            </Button>
          </div>
        </div>
      </form>

      {servidorId === '' ? (
        <EmptyState
          icon="fas fa-receipt"
          title="Selecione um servidor"
          description="Escolha o servidor e clique em Consultar para ver o contracheque."
        />
      ) : (
        <QueryState<Contracheque>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-receipt"
              title="Sem contracheque"
              description="Não há contracheque deste servidor nesta folha."
            />
          }
        >
          {(cc) => (
            <>
              <DataTable
                caption="Linhas do contracheque"
                columns={columns}
                rows={cc.linhas}
                rowKey={(l) => `${l.rubrica}-${l.tipo}-${l.valor}`}
              />
              <dl className="row mt-3">
                <div className="col-sm-4 mb-2">
                  <dt className="text-gray-60 text-down-01">Proventos</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(cc.totalProventos)}</dd>
                </div>
                <div className="col-sm-4 mb-2">
                  <dt className="text-gray-60 text-down-01">Descontos</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(cc.totalDescontos)}</dd>
                </div>
                <div className="col-sm-4 mb-2">
                  <dt className="text-gray-60 text-down-01">Líquido a pagar</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(cc.liquidoAPagar)}</dd>
                </div>
              </dl>
            </>
          )}
        </QueryState>
      )}
    </Modal>
  );
}
