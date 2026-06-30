// Tela de LISTA do Cadastro Imobiliário: consulta os imóveis de um contribuinte
// (GET /contribuintes/{id}/imoveis) sob demanda, com DataTable (estados +
// ordenação) e ações: cadastrar imóvel (cabeçalho) e Apurar IPTU (por linha).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormRow,
  PageHeader,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useImoveisPorContribuinte } from './iptu.api';
import type { ImovelResumo } from './iptu.api';
import { ContribuintePicker } from './ContribuintePicker';
import { USO_IMOVEL_LABEL, formatarArea, usoTagVariant } from './iptu.helpers';
import { TributosSubNav } from './TributosSubNav';
import { ImovelFormModal } from './ImovelFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

export function ImovelListPage() {
  const navigate = useNavigate();
  const [contribuinteId, setContribuinteId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [imovelAberto, setImovelAberto] = useState(false);

  const query = useImoveisPorContribuinte(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(contribuinteId.trim());
  }

  const columns: Column<ImovelResumo>[] = [
    {
      key: 'inscricao',
      header: 'Inscrição',
      sortAccessor: (i) => i.inscricaoMunicipal,
      render: (i) => i.inscricaoMunicipal,
    },
    {
      key: 'endereco',
      header: 'Logradouro',
      render: (i) => i.logradouro,
    },
    { key: 'zona', header: 'Zona', sortAccessor: (i) => i.zonaFiscal, render: (i) => i.zonaFiscal },
    {
      key: 'uso',
      header: 'Uso',
      render: (i) => <Tag variant={usoTagVariant(i.tipoUso)}>{USO_IMOVEL_LABEL[i.tipoUso]}</Tag>,
    },
    {
      key: 'terreno',
      header: 'Terreno',
      align: 'end',
      sortAccessor: (i) => i.areaTerreno,
      render: (i) => formatarArea(i.areaTerreno),
    },
    {
      key: 'construida',
      header: 'Construída',
      align: 'end',
      sortAccessor: (i) => i.areaConstruida,
      render: (i) => formatarArea(i.areaConstruida),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (i) => (
        <Button size="sm" variant="ghost" onClick={() => navigate(`/tributos/imoveis/${i.id}/iptu`)}>
          <i className="fas fa-calculator" aria-hidden="true" /> Apurar IPTU
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Cadastro Imobiliário"
        description="Consulte os imóveis de um contribuinte e apure o IPTU."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setImovelAberto(true)}>
                <i className="fas fa-house-circle-check" aria-hidden="true" /> Cadastrar imóvel
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" disabled={contribuinteId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <ContribuintePicker
              value={contribuinteId}
              onChange={setContribuinteId}
              label="Contribuinte"
              required
            />
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Busque e selecione um contribuinte (por nome ou CPF/CNPJ) e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Imóveis do contribuinte ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(i) => i.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-house"
              title="Nenhum imóvel encontrado"
              description="Este contribuinte não possui imóveis no cadastro imobiliário."
            />
          }
        />
      )}

      <ImovelFormModal
        open={imovelAberto}
        onClose={() => setImovelAberto(false)}
        contribuinteIdInicial={consultaAtiva}
      />
    </>
  );
}
