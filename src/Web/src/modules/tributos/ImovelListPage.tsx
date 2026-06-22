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
  FormField,
  Input,
  PageHeader,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useImoveisPorContribuinte } from './iptu.api';
import type { ImovelResumo } from './iptu.api';
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
      sortAccessor: (i) => i.inscricaoImobiliaria,
      render: (i) => i.inscricaoImobiliaria,
    },
    {
      key: 'endereco',
      header: 'Endereço',
      render: (i) => `${i.logradouro}, ${i.numero} — ${i.bairro}`,
    },
    { key: 'zona', header: 'Zona', sortAccessor: (i) => i.zona, render: (i) => i.zona },
    {
      key: 'uso',
      header: 'Uso',
      render: (i) => <Tag variant={usoTagVariant(i.uso)}>{USO_IMOVEL_LABEL[i.uso]}</Tag>,
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
        <Button variant="tertiary" onClick={() => navigate(`/tributos/imoveis/${i.id}/iptu`)}>
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
            <Button variant="primary" onClick={() => setImovelAberto(true)}>
              <i className="fas fa-house-circle-check" aria-hidden="true" /> Cadastrar imóvel
            </Button>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={contribuinteId}
                    onChange={(e) => setContribuinteId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={contribuinteId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador do contribuinte e clique em Consultar."
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
