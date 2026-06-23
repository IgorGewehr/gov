// Seções mensais (somente leitura) do autosserviço "Minha Folha": meu contracheque
// (reusa o visual do contracheque existente) e meu espelho de ponto. Cada seção dispara
// sua query sob demanda. A UI NUNCA envia servidorId — é dado-próprio resolvido do JWT.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  FormField,
  QueryState,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { MESES, formatarMinutos } from './recursosHumanos.helpers';
import { ANO_ATUAL, CampoAno, MES_ATUAL, Total } from './minhaFolha.bits';
import { useMeuContracheque, useMeuEspelhoPonto } from './minhaFolha.api';
import type {
  LinhaMeuContracheque,
  MeuContracheque,
  MeuEspelhoDePonto,
  TipoFolhaMinha,
} from './minhaFolha.api';
import { TIPOS_FOLHA_MINHA } from './recursosHumanos.helpers';

// ---------------------------------------------------------------------------
// 1) Meu contracheque (reusa o visual do contracheque existente)
// ---------------------------------------------------------------------------

export function MeuContrachequeSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(MES_ATUAL));
  const [tipo, setTipo] = useState<TipoFolhaMinha>('Mensal');
  const [consultou, setConsultou] = useState(false);

  const query = useMeuContracheque(Number(ano), Number(mes), tipo, consultou);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultou(true);
  }

  const columns: Column<LinhaMeuContracheque>[] = [
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
    <>
      <form className="br-form mb-3" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col-sm-4 col-md-3">
            <FormField label="Mês">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4 col-md-3">
            <CampoAno label="Ano" value={ano} onChange={setAno} />
          </div>
          <div className="col-sm-4 col-md-3">
            <FormField label="Tipo de folha">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={tipo}
                  onChange={(e) => setTipo(e.target.value as TipoFolhaMinha)}
                  options={TIPOS_FOLHA_MINHA}
                />
              )}
            </FormField>
          </div>
          <div className="col-auto mb-3">
            <Button variant="primary" type="submit" loading={query.isFetching}>
              Consultar
            </Button>
          </div>
        </div>
      </form>

      {!consultou ? (
        <EmptyState
          icon="fas fa-receipt"
          title="Selecione a competência"
          description="Escolha mês, ano e tipo e clique em Consultar para ver seu contracheque."
        />
      ) : (
        <QueryState<MeuContracheque>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-receipt"
              title="Sem contracheque"
              description="Não há contracheque seu nesta competência/tipo."
            />
          }
        >
          {(cc) => (
            <>
              <DataTable
                caption="Linhas do meu contracheque"
                columns={columns}
                rows={cc.linhas}
                rowKey={(l) => `${l.rubrica}-${l.tipo}-${l.valor}`}
              />
              <dl className="row mt-3">
                <Total rotulo="Proventos" valor={cc.totalProventos} />
                <Total rotulo="Descontos" valor={cc.totalDescontos} />
                <Total rotulo="Líquido a pagar" valor={cc.liquidoAPagar} />
              </dl>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// 2) Meu espelho de ponto
// ---------------------------------------------------------------------------

export function MeuEspelhoPontoSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(MES_ATUAL));
  const [consultou, setConsultou] = useState(false);

  const query = useMeuEspelhoPonto(Number(ano), Number(mes), consultou);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultou(true);
  }

  return (
    <>
      <form className="br-form mb-3" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col-sm-4 col-md-3">
            <FormField label="Mês">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4 col-md-3">
            <CampoAno label="Ano" value={ano} onChange={setAno} />
          </div>
          <div className="col-auto mb-3">
            <Button variant="primary" type="submit" loading={query.isFetching}>
              Consultar
            </Button>
          </div>
        </div>
      </form>

      {!consultou ? (
        <EmptyState
          icon="fas fa-clock"
          title="Selecione a competência"
          description="Escolha mês e ano e clique em Consultar para ver seu espelho de ponto."
        />
      ) : (
        <QueryState<MeuEspelhoDePonto>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-clock"
              title="Sem apuração"
              description="Não há apuração de jornada sua nesta competência."
            />
          }
        >
          {(e) => (
            <>
              <div className="d-flex align-items-center mb-3" style={{ gap: '0.5rem' }}>
                <strong>Competência {e.competencia}</strong>
                <Tag variant={e.fechada ? 'success' : 'warning'}>
                  {e.fechada ? 'Fechada' : 'Aberta'}
                </Tag>
              </div>
              <dl className="row">
                <ItemMin rotulo="Minutos trabalhados" minutos={e.minutosTrabalhados} />
                <ItemMin rotulo="Minutos devidos" minutos={e.minutosDevidos} />
                <ItemMin rotulo="Horas extras" minutos={e.minutosExtras} />
                <ItemMin rotulo="Faltas" minutos={e.minutosFalta} />
                <ItemMin rotulo="Banco de horas (saldo)" minutos={e.saldoBancoHorasMinutos} />
              </dl>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}

/** Item do espelho: rótulo + duração formatada. */
function ItemMin({ rotulo, minutos }: { rotulo: string; minutos: number }) {
  return (
    <div className="col-sm-6 col-md-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{formatarMinutos(minutos)}</dd>
    </div>
  );
}
