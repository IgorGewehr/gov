// Tela de TRANSMISSÃO do ITBI — coração do submódulo ITBI. Informa o imóvel, o
// exercício, o valor declarado e se é SFH e gera o PREVIEW (GET .../preview): a
// base de cálculo é o MAIOR entre o valor venal de referência e o valor declarado,
// com a MEMÓRIA de cálculo. A partir do preview dispara o LANÇAMENTO, que gera a
// guia/DAM. Preview é sob demanda; o lançamento é gated por "tributos.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Toolbar,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { usePreviewItbi } from './itbi.api';
import type { PreviewItbi } from './itbi.api';
import { formatarPercentual } from './iptu.helpers';
import { TributosSubNav } from './TributosSubNav';
import { LancarItbiModal } from './LancarItbiModal';
import { ItbiAliquotaFormModal } from './ItbiAliquotaFormModal';

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

interface Consulta {
  imovelId: string;
  exercicio: number;
  valorDeclarado: number;
  sfh: boolean;
}

export function TransmitirItbiPage() {
  const [imovelId, setImovelId] = useState('');
  const [exercicioCampo, setExercicioCampo] = useState(String(ANO_ATUAL));
  const [valorCampo, setValorCampo] = useState('');
  const [sfhCampo, setSfhCampo] = useState(false);
  const [consulta, setConsulta] = useState<Consulta | null>(null);
  const [lancarAberto, setLancarAberto] = useState(false);
  const [aliquotasAberto, setAliquotasAberto] = useState(false);

  const query = usePreviewItbi(
    consulta?.imovelId ?? '',
    consulta?.exercicio ?? 0,
    consulta?.valorDeclarado ?? -1,
    consulta !== null,
  );

  function gerarPreview(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioCampo);
    const valor = Number(valorCampo);
    if (imovelId.trim() === '' || !Number.isInteger(ano) || ano < 1900 || Number.isNaN(valor) || valor < 0) {
      return;
    }
    setConsulta({ imovelId: imovelId.trim(), exercicio: ano, valorDeclarado: valor, sfh: sfhCampo });
  }

  return (
    <>
      <PageHeader
        title="Transmissão de ITBI"
        description="Informe o imóvel e o valor declarado, confira o preview e lance a guia da transmissão."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={() => setAliquotasAberto(true)}>
                <i className="fas fa-percent" aria-hidden="true" /> Configurar alíquotas
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={gerarPreview}>
          <div className="row">
            <div className="col-12 col-md-6">
              <FormField label="Identificador do imóvel" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={imovelId}
                    onChange={(e) => setImovelId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-2">
              <FormField label="Exercício" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicioCampo} onChange={(e) => setExercicioCampo(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-4">
              <FormField label="Valor declarado (R$)" required help="Valor da transação informado.">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valorCampo} onChange={(e) => setValorCampo(e.target.value)} placeholder="0,00" />
                )}
              </FormField>
            </div>
          </div>

          <div className="br-checkbox">
            <input id="itbi-sfh" type="checkbox" checked={sfhCampo} onChange={(e) => setSfhCampo(e.target.checked)} />
            <label htmlFor="itbi-sfh">Operação enquadrada no SFH (Sistema Financeiro da Habitação)</label>
          </div>

          <Toolbar>
            <Button variant="primary" type="submit" loading={query.isFetching && consulta !== null}>
              <i className="fas fa-magnifying-glass-dollar" aria-hidden="true" /> Preview
            </Button>
          </Toolbar>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-right-left"
          title="Informe a transmissão"
          description="Preencha o imóvel, o exercício e o valor declarado e clique em Preview para ver a base de cálculo."
        />
      ) : (
        <QueryState<PreviewItbi>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(preview) => (
            <>
              <Card
                className="mb-4"
                header={<strong>Preview do ITBI — exercício {preview.exercicio}</strong>}
                footer={
                  <Can permission={PERM_GERENCIAR}>
                    <Toolbar>
                      <Button variant="primary" onClick={() => setLancarAberto(true)}>
                        <i className="fas fa-file-invoice-dollar" aria-hidden="true" /> Lançar e gerar guia
                      </Button>
                    </Toolbar>
                  </Can>
                }
              >
                <dl className="row">
                  <Linha rotulo="Valor venal de referência">{formatarMoeda(preview.valorVenalReferencia)}</Linha>
                  <Linha rotulo="Valor declarado">{formatarMoeda(preview.valorDeclarado)}</Linha>
                  <Linha rotulo="Base de cálculo" destaque>
                    {formatarMoeda(preview.baseCalculo)}
                  </Linha>
                  {/* Alíquota vem em % do backend (ex.: 2.0 = 2%); convertemos p/ fração. */}
                  <Linha rotulo={`Alíquota aplicada${consulta.sfh ? ' (SFH)' : ''}`}>
                    {formatarPercentual(preview.aliquotaPercentual / 100)}
                  </Linha>
                  <Linha rotulo="Imposto bruto">{formatarMoeda(preview.impostoBruto)}</Linha>
                  <Linha rotulo="Isenção">{formatarMoeda(preview.valorIsencao)}</Linha>
                  <Linha rotulo="Imposto devido" destaque>
                    {formatarMoeda(preview.impostoDevido)}
                  </Linha>
                </dl>

                <Alert variant="info" title="Base de cálculo do ITBI">
                  A base é o <strong>valor declarado</strong> (Tema 1.113/STJ — presunção de veracidade). O
                  valor venal de referência só dispara a triagem.
                </Alert>
                {preview.haDivergenciaReferencia && (
                  <Alert variant="warning" title="Divergência com o valor de referência">
                    O valor declarado diverge do venal de referência. A base permanece o declarado; eventual
                    arbitramento exige processo com contraditório.
                  </Alert>
                )}
              </Card>

              <LancarItbiModal
                open={lancarAberto}
                onClose={() => setLancarAberto(false)}
                imovelId={preview.imovelId}
                exercicio={preview.exercicio}
                valorDeclarado={preview.valorDeclarado}
                sfh={consulta.sfh}
                baseCalculo={preview.baseCalculo}
                impostoDevido={preview.impostoDevido}
              />
            </>
          )}
        </QueryState>
      )}

      <ItbiAliquotaFormModal open={aliquotasAberto} onClose={() => setAliquotasAberto(false)} />
    </>
  );
}
