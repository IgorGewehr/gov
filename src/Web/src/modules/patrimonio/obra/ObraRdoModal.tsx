// [Command RegistrarRdo] Registra um Registro Diário de Obra (RDO — fiscalização
// contínua, I-8/I-9): data, condição de tempo, efetivo, equipamentos, atividades
// executadas, responsável técnico e ocorrências do dia.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Textarea,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAuth } from '../../../auth/useAuth';
import { useRegistrarRdo } from './obra.api';
import { hojeIso } from './obra.helpers';

export interface ObraRdoModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
}

export function ObraRdoModal({ open, onClose, obraId }: ObraRdoModalProps) {
  const toast = useToast();
  const { user } = useAuth();
  const mutation = useRegistrarRdo(obraId);

  const [data, setData] = useState(hojeIso());
  const [condicaoTempo, setCondicaoTempo] = useState('');
  const [efetivo, setEfetivo] = useState('');
  const [equipamentos, setEquipamentos] = useState('');
  const [atividades, setAtividades] = useState('');
  const [responsavelTecnicoId, setResponsavelTecnicoId] = useState(user?.id ?? '');
  const [ocorrencias, setOcorrencias] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const efetivoNum = Number(efetivo);
    if (data.trim() === '' || condicaoTempo.trim() === '' || atividades.trim() === '') {
      setErro('Informe data, condição de tempo e atividades executadas.');
      return;
    }
    if (!Number.isInteger(efetivoNum) || efetivoNum < 0) {
      setErro('Efetivo de mão de obra deve ser um inteiro não negativo.');
      return;
    }
    if (responsavelTecnicoId.trim() === '') {
      setErro('Informe o responsável técnico.');
      return;
    }
    setErro(null);

    mutation.mutate(
      {
        data,
        condicaoTempo: condicaoTempo.trim(),
        efetivoMaoDeObra: efetivoNum,
        equipamentosMobilizados: equipamentos.trim(),
        atividadesExecutadas: atividades.trim(),
        responsavelTecnicoId: responsavelTecnicoId.trim(),
        ocorrencias: ocorrencias.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('RDO registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o RDO.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar RDO (diário de obra)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-rdo" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-rdo" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        <div className="row">
          <div className="col-sm-5">
            <FormField label="Data" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={data}
                  onChange={(e) => setData(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Condição de tempo" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={condicaoTempo}
                  onChange={(e) => setCondicaoTempo(e.target.value)}
                  placeholder="Ex.: Bom / Chuvoso"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="Efetivo">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="number"
                  min={0}
                  aria-describedby={describedBy}
                  value={efetivo}
                  onChange={(e) => setEfetivo(e.target.value)}
                  placeholder="0"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Equipamentos mobilizados">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={equipamentos}
              onChange={(e) => setEquipamentos(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Atividades executadas" required>
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              value={atividades}
              onChange={(e) => setAtividades(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Responsável técnico (identificador)"
          required
          help="Pré-preenchido com o usuário atual."
        >
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={responsavelTecnicoId}
              onChange={(e) => setResponsavelTecnicoId(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Ocorrências do dia" help="Opcional.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              rows={2}
              aria-describedby={describedBy}
              value={ocorrencias}
              onChange={(e) => setOcorrencias(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
