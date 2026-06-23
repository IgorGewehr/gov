// Modal de CONSULTA "Prescrição" (query AvaliarPrescricaoDivida) de uma Dívida Ativa numa data de
// referência. Espelha AvaliacaoPrescricaoDivida: termo inicial + data-limite (CTN art. 174),
// flag EstaPrescrita e os encargos apurados (originário, correção, multa, juros, atualizado) na
// data informada — cálculo determinístico (sem relógio no servidor). Leitura: tributos.ver.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, FormRow, Input, Modal, QueryState } from '../../components/ui';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useAvaliarPrescricao } from './api';

export interface PrescricaoModalProps {
  open: boolean;
  onClose: () => void;
  dividaAtivaId: string;
}

function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function PrescricaoModal({ open, onClose, dividaAtivaId }: PrescricaoModalProps) {
  const [dataReferencia, setDataReferencia] = useState(hojeIso());
  const [consulta, setConsulta] = useState('');

  const query = useAvaliarPrescricao(dividaAtivaId, consulta, open && consulta.length > 0);

  function fechar(): void {
    setDataReferencia(hojeIso());
    setConsulta('');
    onClose();
  }

  function avaliar(event: FormEvent): void {
    event.preventDefault();
    setConsulta(dataReferencia);
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Avaliar prescrição da Dívida Ativa"
      footer={
        <Button variant="secondary" onClick={fechar}>
          Fechar
        </Button>
      }
    >
      <form className="br-form mb-3" onSubmit={avaliar} noValidate>
        <FormRow
          acao={
            <Button variant="primary" type="submit" disabled={dataReferencia === ''} loading={query.isFetching}>
              Avaliar
            </Button>
          }
        >
          <FormField label="Data de referência" required help="Data do fato para apurar prazo e encargos.">
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="date"
                aria-describedby={describedBy}
                invalid={invalid}
                value={dataReferencia}
                onChange={(e) => setDataReferencia(e.target.value)}
              />
            )}
          </FormField>
        </FormRow>
      </form>

      {consulta === '' ? (
        <Alert variant="info" title="Prescrição (CTN art. 174)">
          Informe a data de referência e clique em Avaliar para ver a data-limite da prescrição e os
          encargos apurados nessa data.
        </Alert>
      ) : (
        <QueryState
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(dados) => (
            <>
              {dados.estaPrescrita ? (
                <Alert variant="danger" title="Crédito PRESCRITO nesta data">
                  Em {formatarData(consulta)} o prazo prescricional (data-limite{' '}
                  {formatarData(dados.dataPrescricao)}) já transcorreu.
                </Alert>
              ) : (
                <Alert variant="success" title="Crédito exigível nesta data">
                  Data-limite da prescrição: {formatarData(dados.dataPrescricao)}.
                </Alert>
              )}
              <dl className="mb-0">
                <dt>Termo inicial da prescrição</dt>
                <dd>{formatarData(dados.termoInicialPrescricao)}</dd>
                <dt>Data-limite (CTN art. 174)</dt>
                <dd>{formatarData(dados.dataPrescricao)}</dd>
                <dt>Valor originário</dt>
                <dd>{formatarMoeda(dados.valorOriginario)}</dd>
                <dt>Correção monetária</dt>
                <dd>{formatarMoeda(dados.correcaoMonetaria)}</dd>
                <dt>Multa de mora</dt>
                <dd>{formatarMoeda(dados.multa)}</dd>
                <dt>Juros de mora</dt>
                <dd>{formatarMoeda(dados.juros)}</dd>
                <dt>
                  <strong>Valor atualizado</strong>
                </dt>
                <dd>
                  <strong>{formatarMoeda(dados.valorAtualizado)}</strong>
                </dd>
              </dl>
            </>
          )}
        </QueryState>
      )}
    </Modal>
  );
}
