// Seções anuais (somente leitura) do autosserviço "Minha Folha": minhas férias e meu
// informe de rendimentos. Cada seção dispara sua query sob demanda. A UI NUNCA envia
// servidorId — é dado-próprio resolvido do JWT no backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { situacaoFolhaTagVariant } from './recursosHumanos.helpers';
import { ANO_ATUAL, CampoAno, Total } from './minhaFolha.bits';
import { useMeuInformeRendimentos, useMinhasFerias } from './minhaFolha.api';
import type {
  MeuInformeRendimentos,
  MinhasFerias,
  MinhasFeriasItem,
} from './minhaFolha.api';

// ---------------------------------------------------------------------------
// 3) Minhas férias (ano)
// ---------------------------------------------------------------------------

export function MinhasFeriasSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [consultou, setConsultou] = useState(false);

  const query = useMinhasFerias(Number(ano), consultou);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultou(true);
  }

  const columns: Column<MinhasFeriasItem>[] = [
    { key: 'competencia', header: 'Competência', render: (f) => f.competencia },
    {
      key: 'proventos',
      header: 'Proventos',
      align: 'end',
      render: (f) => formatarMoeda(f.totalProventos),
    },
    {
      key: 'descontos',
      header: 'Descontos',
      align: 'end',
      render: (f) => formatarMoeda(f.totalDescontos),
    },
    {
      key: 'liquido',
      header: 'Líquido',
      align: 'end',
      render: (f) => formatarMoeda(f.liquidoAPagar),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (f) => (
        <Tag variant={situacaoFolhaTagVariant(f.situacao)}>{f.situacao}</Tag>
      ),
    },
  ];

  return (
    <>
      <form className="br-form mb-3" onSubmit={consultar}>
        <div className="row align-items-end">
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
          icon="fas fa-umbrella-beach"
          title="Selecione o ano"
          description="Escolha o ano e clique em Consultar para ver suas férias."
        />
      ) : (
        <QueryState<MinhasFerias>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(ferias) => (
            <DataTable
              caption="Minhas folhas de férias no ano"
              columns={columns}
              rows={ferias.folhas}
              rowKey={(f) => f.competencia}
              empty={
                <EmptyState
                  icon="fas fa-umbrella-beach"
                  title="Sem férias"
                  description="Não há folhas de férias suas neste ano."
                />
              }
            />
          )}
        </QueryState>
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// 4) Meu informe de rendimentos (ano-calendário)
// ---------------------------------------------------------------------------

export function MeuInformeRendimentosSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL - 1));
  const [consultou, setConsultou] = useState(false);

  const query = useMeuInformeRendimentos(Number(ano), consultou);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultou(true);
  }

  return (
    <>
      <form className="br-form mb-3" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col-sm-4 col-md-3">
            <CampoAno label="Ano-calendário" value={ano} onChange={setAno} />
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
          icon="fas fa-file-invoice-dollar"
          title="Selecione o ano-calendário"
          description="Escolha o ano e clique em Consultar para ver seu informe de rendimentos."
        />
      ) : (
        <QueryState<MeuInformeRendimentos>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(inf) => (
            <>
              <p className="text-down-01 text-gray-60 mt-0">
                Projeção de leitura do ano-calendário {inf.anoCalendario} (folhas mensais + 13º).
                Não substitui a DIRF/eSocial oficiais.
              </p>
              <dl className="row">
                <Total rotulo="Rendimentos tributáveis" valor={inf.rendimentosTributaveis} />
                <Total rotulo="Previdência oficial" valor={inf.previdenciaOficial} />
                <Total rotulo="IRRF retido na fonte" valor={inf.impostoRetidoNaFonte} />
              </dl>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}
