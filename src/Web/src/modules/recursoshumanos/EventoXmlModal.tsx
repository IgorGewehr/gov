// Modal de INSPEÇÃO/AUDITORIA de um evento eSocial: estado, ambiente, hash do XML
// gerado, protocolo do lote e recibo (nrRecibo), e o XML completo (somente leitura)
// para conferência do leiaute S-1.3. Não muta nada.
import { Button, Modal, Tag } from '../../components/ui';
import type { EventoESocial } from './esocial.api';
import {
  estadoEventoTagVariant,
  rotuloAmbiente,
  rotuloEstado,
  rotuloTipoEvento,
} from './esocial.helpers';

export interface EventoXmlModalProps {
  evento: EventoESocial | null;
  onClose: () => void;
}

export function EventoXmlModal({ evento, onClose }: EventoXmlModalProps) {
  if (evento === null) return null;

  return (
    <Modal
      open
      onClose={onClose}
      title={rotuloTipoEvento(evento.tipo)}
      size="large"
      footer={
        <Button variant="secondary" onClick={onClose}>
          Fechar
        </Button>
      }
    >
      <dl className="row">
        <div className="col-sm-6 mb-3">
          <dt className="text-gray-60 text-down-01">Estado</dt>
          <dd className="mb-0">
            <Tag variant={estadoEventoTagVariant(evento.estado)}>{rotuloEstado(evento.estado)}</Tag>
          </dd>
        </div>
        <div className="col-sm-6 mb-3">
          <dt className="text-gray-60 text-down-01">Ambiente</dt>
          <dd className="mb-0">{rotuloAmbiente(evento.ambiente)}</dd>
        </div>
        <div className="col-sm-12 mb-3">
          <dt className="text-gray-60 text-down-01">Id do evento</dt>
          <dd className="mb-0" style={{ wordBreak: 'break-all' }}>
            {evento.idEvento}
          </dd>
        </div>
        <div className="col-sm-6 mb-3">
          <dt className="text-gray-60 text-down-01">Protocolo do lote</dt>
          <dd className="mb-0">{evento.protocoloLote ?? '—'}</dd>
        </div>
        <div className="col-sm-6 mb-3">
          <dt className="text-gray-60 text-down-01">Recibo (nrRecibo)</dt>
          <dd className="mb-0">{evento.numeroRecibo ?? '—'}</dd>
        </div>
        <div className="col-sm-12 mb-3">
          <dt className="text-gray-60 text-down-01">Hash SHA-256 do XML gerado</dt>
          <dd className="mb-0" style={{ wordBreak: 'break-all' }}>
            {evento.hashXmlGerado}
          </dd>
        </div>
      </dl>

      <h3 className="text-up-01 mb-2">
        XML do evento {evento.assinado ? '(assinado disponível)' : '(gerado, ainda não assinado)'}
      </h3>
      {/* Landmark rolável (XML longo) exposto à tecnologia assistiva via role/aria-label. */}
      <pre
        role="region"
        aria-label="Conteúdo do XML do evento eSocial"
        style={{
          maxHeight: '24rem',
          overflow: 'auto',
          background: 'var(--gray-2, #f8f8f8)',
          padding: '1rem',
          borderRadius: '4px',
          fontSize: '0.8125rem',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}
      >
        {evento.xml}
      </pre>
    </Modal>
  );
}
