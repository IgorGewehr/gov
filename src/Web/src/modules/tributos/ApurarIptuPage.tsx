// Tela APURAR IPTU — coração do submódulo. Seleciona imóvel (via rota) + exercício
// e mostra a MEMÓRIA DE CÁLCULO (valor do terreno + construção = valor venal ×
// alíquota = imposto, com desconto de cota única em destaque). A partir daqui se
// dispara o LANÇAMENTO (gera lançamento + DAM). Apuração é sob demanda (Apurar).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
  Toolbar,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useApurarIptu } from './iptu.api';
import type { ApuracaoIptu } from './iptu.api';
import { formatarPercentual } from './iptu.helpers';
import { TributosSubNav } from './TributosSubNav';
import { LancarIptuModal } from './LancarIptuModal';

const PERM_GERENCIAR = 'tributos.gerenciar';
const ANO_ATUAL = new Date().getFullYear();

function Linha({ rotulo, children, destaque }: { rotulo: string; children: React.ReactNode; destaque?: boolean }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className={`mb-0 ${destaque ? 'text-up-02 text-semi-bold' : 'text-semi-bold'}`}>{children}</dd>
    </div>
  );
}

export function ApurarIptuPage() {
  const { id = '' } = useParams<{ id: string }>();
  const [exercicioCampo, setExercicioCampo] = useState(String(ANO_ATUAL));
  const [exercicio, setExercicio] = useState(0);
  const [lancarAberto, setLancarAberto] = useState(false);

  const query = useApurarIptu(id, exercicio, exercicio > 0);

  function apurar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioCampo);
    if (Number.isInteger(ano) && ano > 0) setExercicio(ano);
  }

  return (
    <>
      <PageHeader
        title="Apurar IPTU"
        description="Calcule o imposto de um imóvel num exercício e veja a memória de cálculo."
        actions={
          <Link className="br-button secondary" to="/tributos/imoveis">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar aos imóveis
          </Link>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={apurar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching && exercicio > 0}>
                <i className="fas fa-calculator" aria-hidden="true" /> Apurar
              </Button>
            }
          >
            <FormField label="Exercício" required help={`Imóvel ${id}`}>
              {({ id: campoId, describedBy, invalid }) => (
                <Input
                  id={campoId}
                  type="number"
                  min={1900}
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicioCampo}
                  onChange={(e) => setExercicioCampo(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {exercicio === 0 ? (
        <EmptyState
          icon="fas fa-calculator"
          title="Informe o exercício"
          description="Digite o ano de exercício e clique em Apurar para ver a memória de cálculo."
        />
      ) : (
        <QueryState<ApuracaoIptu>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(apuracao) => (
            <>
              <Card
                className="mb-4"
                header={<strong>Apuração do IPTU — exercício {apuracao.exercicio}</strong>}
                footer={
                  <Can permission={PERM_GERENCIAR}>
                    <Toolbar>
                      <Button variant="primary" onClick={() => setLancarAberto(true)}>
                        <i className="fas fa-file-invoice-dollar" aria-hidden="true" /> Lançar e gerar DAM
                      </Button>
                    </Toolbar>
                  </Can>
                }
              >
                <dl className="row">
                  <Linha rotulo="Valor do terreno">{formatarMoeda(apuracao.valorTerreno)}</Linha>
                  <Linha rotulo="Valor da construção">{formatarMoeda(apuracao.valorConstrucao)}</Linha>
                  <Linha rotulo="Valor venal (terreno + construção)" destaque>
                    {formatarMoeda(apuracao.valorVenal)}
                  </Linha>
                  {/* O backend devolve a alíquota em % (ex.: 1.0 = 1%); convertemos p/ fração. */}
                  <Linha rotulo="Alíquota aplicada">
                    {formatarPercentual(apuracao.aliquotaPercentual / 100)}
                  </Linha>
                  <Linha rotulo="Imposto bruto (venal × alíquota)">
                    {formatarMoeda(apuracao.impostoBruto)}
                  </Linha>
                  <Linha rotulo="Isenção">{formatarMoeda(apuracao.valorIsencao)}</Linha>
                  <Linha rotulo="Desconto">{formatarMoeda(apuracao.valorDesconto)}</Linha>
                  <Linha rotulo="Imposto devido" destaque>
                    {formatarMoeda(apuracao.impostoDevido)}
                  </Linha>
                </dl>
              </Card>

              <LancarIptuModal
                open={lancarAberto}
                onClose={() => setLancarAberto(false)}
                imovelId={apuracao.imovelId}
                exercicio={apuracao.exercicio}
              />
            </>
          )}
        </QueryState>
      )}
    </>
  );
}
