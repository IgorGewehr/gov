// Camada de API do submódulo Domicílio Eletrônico do Contribuinte (DEC).
// Espelha o contrato REAL da Minimal API /api/tributos/domicilio-eletronico:
//   POST /contribuintes/{id}/aderir    -> AderirDomicilioEletronico -> { id }      [tributos.gerenciar]
//   POST /contribuintes/{id}/mensagens -> DisponibilizarMensagemFiscal -> Resultado [tributos.gerenciar]
//   POST /contribuintes/{id}/cancelar  -> CancelarDomicilioEletronico -> { ok }     [tributos.gerenciar]
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Tipo da comunicação fiscal (back: TipoMensagemFiscal). */
export type TipoMensagemFiscal = 'Intimacao' | 'Notificacao' | 'Aviso';

/** AderirDomicilioPayload. */
export interface AderirDomicilioInput {
  /** Data de adesão ("yyyy-MM-dd"). */
  dataAdesao: string;
  /** Prazo (dias) para a ciência tácita (padrão 15). */
  diasCienciaTacita: number;
}

/** DisponibilizarMensagemPayload. */
export interface DisponibilizarMensagemInput {
  tipo: TipoMensagemFiscal;
  assunto: string;
  corpo: string;
  /** Data de disponibilização ("yyyy-MM-dd"). */
  dataDisponibilizacao: string;
  /** Prazo (dias) de manifestação contado da ciência; 0 se sem prazo. */
  diasPrazoManifestacao: number;
  /** Referência ao ato de origem (ex.: nº do lançamento/CDA). */
  referenciaExterna: string | null;
}

/** Resultado da disponibilização (espelha ResultadoMensagemFiscal). */
export interface ResultadoMensagemFiscal {
  mensagemId: string;
  /** Data-limite para a ciência tácita ("yyyy-MM-dd"). */
  dataLimiteCienciaTacita: string;
}

function aderir(contribuinteId: string, input: AderirDomicilioInput): Promise<{ id: string }> {
  return http.post<{ id: string }>(
    `/tributos/domicilio-eletronico/contribuintes/${contribuinteId}/aderir`,
    input,
  );
}

function disponibilizar(
  contribuinteId: string,
  input: DisponibilizarMensagemInput,
): Promise<ResultadoMensagemFiscal> {
  return http.post<ResultadoMensagemFiscal>(
    `/tributos/domicilio-eletronico/contribuintes/${contribuinteId}/mensagens`,
    input,
  );
}

function cancelar(contribuinteId: string): Promise<{ ok: boolean }> {
  return http.post<{ ok: boolean }>(
    `/tributos/domicilio-eletronico/contribuintes/${contribuinteId}/cancelar`,
    {},
  );
}

/** Adere o contribuinte ao Domicílio Eletrônico (cria a caixa postal fiscal). */
export function useAderirDomicilio(contribuinteId: string) {
  return useMutation({ mutationFn: (input: AderirDomicilioInput) => aderir(contribuinteId, input) });
}

/** Disponibiliza (envia) uma mensagem fiscal ao domicílio do contribuinte. */
export function useDisponibilizarMensagem(contribuinteId: string) {
  return useMutation({
    mutationFn: (input: DisponibilizarMensagemInput) => disponibilizar(contribuinteId, input),
  });
}

/** Cancela a adesão do contribuinte ao Domicílio Eletrônico. */
export function useCancelarDomicilio(contribuinteId: string) {
  return useMutation({ mutationFn: () => cancelar(contribuinteId) });
}
