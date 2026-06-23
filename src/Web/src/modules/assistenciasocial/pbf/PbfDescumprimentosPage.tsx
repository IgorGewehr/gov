// Painel de descumprimentos de condicionalidades do PBF (query ObterDescumprimentos).
// Busca ativa do CRAS: filtra por competencia + efeito minimo; cada familia em
// descumprimento vira um Card com semaforo de efeito e o painel de condicionalidades.
// Acao "Abrir acompanhamento" (gerenciar) cria/recupera o acompanhamento de uma familia.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  CardSecao,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Metrica,
  MetricaGrade,
  PageHeader,
  QueryState,
  Select,
  Tag,
  Toolbar,
} from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { useDescumprimentos } from './pbf.api';
import type { EfeitoDescumprimento } from './pbf.api';
import { EFEITO_MINIMO_OPTIONS, MES_OPTIONS, efeitoLabel, efeitoTagVariant } from './pbf.helpers';
import { CondicionalidadesPanel } from './CondicionalidadesPanel';
import { AbrirAcompanhamentoModal } from './AbrirAcompanhamentoModal';

const hoje = new Date();

export function PbfDescumprimentosPage() {
  const [ano, setAno] = useState(String(hoje.getFullYear()));
  const [mes, setMes] = useState(String(hoje.getMonth() + 1));
  const [efeitoMinimo, setEfeitoMinimo] = useState<string>('Advertencia');
  const [consulta, setConsulta] = useState<{
    ano: number;
    mes: number;
    efeito: EfeitoDescumprimento | null;
  } | null>(null);
  const [abrirAberto, setAbrirAberto] = useState(false);

  const query = useDescumprimentos(
    consulta?.ano ?? 0,
    consulta?.mes ?? 0,
    consulta?.efeito ?? null,
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const anoNum = Number(ano);
    const mesNum = Number(mes);
    if (Number.isNaN(anoNum) || anoNum < 1900 || Number.isNaN(mesNum) || mesNum < 1 || mesNum > 12)
      return;
    setConsulta({
      ano: anoNum,
      mes: mesNum,
      efeito: (efeitoMinimo as EfeitoDescumprimento) || null,
    });
  }

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social · PBF"
        title="Condicionalidades — descumprimentos"
        description="Busca ativa do CRAS: famílias em descumprimento de condicionalidades do Bolsa Família por competência, com o efeito gradativo gerencial (advertência/bloqueio/suspensão)."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                <i className="fas fa-folder-plus" aria-hidden="true" /> Abrir acompanhamento
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-4">
                <FormField label="Ano" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
                      max="9999"
                      step="1"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={ano}
                      onChange={(e) => setAno(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Mês" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      options={MES_OPTIONS}
                      value={mes}
                      onChange={(e) => setMes(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Efeito mínimo" help="Gravidade mínima a incluir na busca ativa.">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={EFEITO_MINIMO_OPTIONS}
                      value={efeitoMinimo}
                      onChange={(e) => setEfeitoMinimo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Informe ano, mês e o efeito mínimo e clique em Consultar."
        />
      ) : (
        <QueryState
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(familias) =>
            familias.length === 0 ? (
              <EmptyState
                icon="fas fa-circle-check"
                title="Nenhum descumprimento na competência"
                description="Não há famílias em descumprimento para o filtro informado."
              />
            ) : (
              <>
                <MetricaGrade>
                  <Metrica
                    label="Famílias em descumprimento"
                    valor={familias.length}
                    secundario={`Competência ${consulta.mes.toString().padStart(2, '0')}/${consulta.ano}`}
                  />
                  <Metrica
                    label="Em suspensão"
                    valor={familias.filter((f) => f.efeito === 'Suspensao').length}
                    tom="perigo"
                  />
                  <Metrica
                    label="Em bloqueio"
                    valor={familias.filter((f) => f.efeito === 'Bloqueio').length}
                    tom="alerta"
                  />
                </MetricaGrade>

                <div className="mt-4">
                  {familias.map((f) => (
                    <CardSecao
                      key={f.acompanhamentoId}
                      className="mb-3"
                      titulo={`Família ${f.familiaId}`}
                      subtitulo={`Competência ${f.competencia} · ${f.descumprimentosEfetivos} descumprimento(s) efetivo(s)`}
                      acao={<Tag variant={efeitoTagVariant(f.efeito)}>{efeitoLabel(f.efeito)}</Tag>}
                    >
                      <CondicionalidadesPanel acompanhamento={f} />
                    </CardSecao>
                  ))}
                </div>
              </>
            )
          }
        </QueryState>
      )}

      <AbrirAcompanhamentoModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        anoInicial={consulta?.ano ?? hoje.getFullYear()}
        mesInicial={consulta?.mes ?? hoje.getMonth() + 1}
      />
    </>
  );
}
