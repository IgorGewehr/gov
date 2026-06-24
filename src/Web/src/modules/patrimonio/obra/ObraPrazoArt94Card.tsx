// Cartão do STATUS do prazo do art. 94 §3 (Lei 14.133/2021): publicação/registro
// do contrato após a assinatura (25 d.u.) ou da obra após a conclusão (45 d.u.).
// Status INDICATIVO de tela (a vencer/vencido) calculado das datas da ficha; o
// alerta OFICIAL vem do varredor transversal no backend (calendário do tenant).
import { CardSecao, Metrica, MetricaGrade, Tag } from '../../../components/ui';
import { formatarData } from '../../../i18n/format';
import type { SituacaoObraNome } from './obra.api';
import { calcularPrazoArt94 } from './obra.helpers';

export interface ObraPrazoArt94CardProps {
  situacao: SituacaoObraNome;
  dataAssinaturaContrato: string;
  dataConclusao?: string | null;
}

export function ObraPrazoArt94Card({
  situacao,
  dataAssinaturaContrato,
  dataConclusao,
}: ObraPrazoArt94CardProps) {
  const prazo = calcularPrazoArt94(situacao, dataAssinaturaContrato, dataConclusao);

  if (!prazo) {
    return (
      <CardSecao
        titulo="Prazo do art. 94 §3"
        subtitulo="Publicação/registro do contrato (Lei 14.133/2021)."
        nota="Não aplicável à situação atual da obra."
      >
        <p className="text-gray-60 mb-0">Sem relógio de prazo ativo nesta situação.</p>
      </CardSecao>
    );
  }

  const vencido = prazo.status === 'vencido';
  const tipoLabel =
    prazo.tipo === 'Assinatura25'
      ? 'Após assinatura (25 dias úteis)'
      : 'Após conclusão (45 dias úteis)';

  return (
    <CardSecao
      titulo="Prazo do art. 94 §3"
      subtitulo="Publicação/registro do contrato (Lei 14.133/2021)."
      acao={
        <Tag variant={vencido ? 'danger' : 'success'}>{vencido ? 'Vencido' : 'A vencer'}</Tag>
      }
      nota="Status indicativo (dias úteis, sem feriados). O alerta oficial vem do varredor transversal com o calendário do tenant."
    >
      <MetricaGrade>
        <Metrica label="Relógio aplicável" valor={tipoLabel} />
        <Metrica label="Data-base" valor={formatarData(prazo.base)} />
        <Metrica label="Vencimento estimado" valor={formatarData(prazo.vencimento)} tom={vencido ? 'perigo' : 'neutro'} />
        <Metrica
          label="Dias úteis restantes"
          valor={prazo.diasUteisRestantes}
          secundario={vencido ? 'Prazo vencido' : 'Dentro do prazo'}
          tom={vencido ? 'perigo' : 'sucesso'}
        />
      </MetricaGrade>
    </CardSecao>
  );
}
