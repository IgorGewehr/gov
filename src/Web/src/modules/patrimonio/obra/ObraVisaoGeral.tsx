// Aba "Visão geral" da ficha da obra: identificação (contrato, contratada, local,
// regime), valores (contratado × medido acumulado × saldo), % físico acumulado e o
// cartão do STATUS do prazo do art. 94 §3. Apenas leitura.
import { Card, CardSecao, Metrica, MetricaGrade } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import type { ObraDetalhe } from './obra.api';
import { regimeExecucaoLabel } from './obra.helpers';
import { ObraPrazoArt94Card } from './ObraPrazoArt94Card';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function ObraVisaoGeral({ obra }: { obra: ObraDetalhe }) {
  const saldo = obra.valorContratado - obra.valorMedidoAcumulado;
  return (
    <>
      <CardSecao
        titulo="Execução físico-financeira"
        subtitulo="O valor medido acumulado nunca excede o valor contratado (teto — I-1)."
        className="mb-4"
      >
        <MetricaGrade>
          <Metrica label="Valor contratado" valor={formatarMoeda(obra.valorContratado)} />
          <Metrica
            label="Medido acumulado"
            valor={formatarMoeda(obra.valorMedidoAcumulado)}
            secundario={`${((obra.valorMedidoAcumulado / (obra.valorContratado || 1)) * 100).toFixed(2)}% do contrato`}
          />
          <Metrica label="Saldo do contrato" valor={formatarMoeda(saldo)} tom={saldo <= 0 ? 'alerta' : 'neutro'} />
          <Metrica
            label="% físico acumulado"
            valor={`${obra.percentualFisicoAcumulado.toFixed(2)}%`}
            tom={obra.percentualFisicoAcumulado >= 100 ? 'sucesso' : 'neutro'}
          />
        </MetricaGrade>
      </CardSecao>

      <Card className="mb-4" header={<strong>Identificação</strong>}>
        <dl className="row">
          <Campo rotulo="Contrato de origem">{obra.contratoId}</Campo>
          <Campo rotulo="Contratada">{obra.fornecedorId}</Campo>
          <Campo rotulo="Município/UF">{`${obra.municipio}/${obra.uf}`}</Campo>
          <Campo rotulo="Regime de execução">{regimeExecucaoLabel(obra.regimeExecucao)}</Campo>
          <Campo rotulo="Assinatura do contrato">
            {formatarData(obra.dataAssinaturaContrato)}
          </Campo>
          <Campo rotulo="Ordem de início">
            {obra.dataInicioOrdemServico ? formatarData(obra.dataInicioOrdemServico) : '—'}
          </Campo>
          <Campo rotulo="Conclusão">
            {obra.dataConclusao ? formatarData(obra.dataConclusao) : '—'}
          </Campo>
          <Campo rotulo="Fiscal designado">{obra.fiscalDesignadoId ?? '—'}</Campo>
          {obra.bemPatrimonialId && (
            <Campo rotulo="Bem patrimonial incorporado">{obra.bemPatrimonialId}</Campo>
          )}
        </dl>
      </Card>

      <ObraPrazoArt94Card
        situacao={obra.situacao}
        dataAssinaturaContrato={obra.dataAssinaturaContrato}
        dataConclusao={obra.dataConclusao}
      />
    </>
  );
}
