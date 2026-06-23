// Aba LICENÇAS da VISA: alerta de licenças a vencer (janela em dias) + consulta do histórico
// de um estabelecimento, com emissão, renovação e cassação. Endpoints reais sob
// /saude/vigilancia/licencas, gated em "saude.vigilancia.ver"; ações em "saude.vigilancia.licenciar".
import { useState } from 'react';
import {
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can, useHasPermission } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import {
  useCassarLicenca,
  useLicencasAVencer,
  useLicencasDoEstabelecimento,
} from './vigilancia.api';
import type { LicencaSanitariaDto } from './vigilancia.api';
import { situacaoLicencaLabel, situacaoLicencaVariant } from './vigilancia.helpers';
import { EstabelecimentoVisaPicker } from './EstabelecimentoVisaPicker';
import { EmitirLicencaModal, RenovarLicencaModal } from './VisaLicencaModals';
import { VisaMotivoModal } from './VisaMotivoModal';

const JANELA_PADRAO = 30;

export function VisaLicencasTab() {
  const podeLicenciar = useHasPermission('saude.vigilancia.licenciar');
  const [dias, setDias] = useState(JANELA_PADRAO);
  const [diasCampo, setDiasCampo] = useState(String(JANELA_PADRAO));
  const [estabId, setEstabId] = useState('');

  const [emitirAberto, setEmitirAberto] = useState(false);
  const [renovar, setRenovar] = useState<LicencaSanitariaDto | null>(null);
  const [cassar, setCassar] = useState<LicencaSanitariaDto | null>(null);

  const aVencer = useLicencasAVencer(dias);
  const doEstab = useLicencasDoEstabelecimento(estabId, estabId !== '');
  const cassarMut = useCassarLicenca(cassar?.id ?? '');

  function colunas(comAcoes: boolean): Column<LicencaSanitariaDto>[] {
    const base: Column<LicencaSanitariaDto>[] = [
      { key: 'numero', header: 'Número', render: (l) => l.numero },
      { key: 'emitida', header: 'Emissão', render: (l) => formatarData(l.emitidaEm) },
      { key: 'validade', header: 'Validade', render: (l) => formatarData(l.validadeAte) },
      {
        key: 'situacao',
        header: 'Situação',
        render: (l) => <Tag variant={situacaoLicencaVariant(l.situacao)}>{situacaoLicencaLabel[l.situacao]}</Tag>,
      },
    ];
    if (comAcoes && podeLicenciar) {
      base.push({
        key: 'acoes',
        header: 'Ações',
        sticky: true,
        render: (l) => (
          <div className="d-flex" style={{ gap: '0.5rem' }}>
            <Button variant="secondary" className="small" onClick={() => setRenovar(l)}>
              Renovar
            </Button>
            {l.situacao !== 'Cassada' && (
              <Button variant="secondary" className="small" onClick={() => setCassar(l)}>
                Cassar
              </Button>
            )}
          </div>
        ),
      });
    }
    return base;
  }

  return (
    <>
      <Toolbar className="mb-3">
        <Can permission="saude.vigilancia.licenciar">
          <Button variant="primary" onClick={() => setEmitirAberto(true)}>
            <i className="fas fa-plus" aria-hidden="true" /> Emitir licença
          </Button>
        </Can>
      </Toolbar>

      <CardSecao titulo="Licenças a vencer">
        <form
          className="br-form mb-3"
          onSubmit={(e) => {
            e.preventDefault();
            const n = Number(diasCampo);
            setDias(Number.isFinite(n) && n > 0 ? n : JANELA_PADRAO);
          }}
        >
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={aVencer.isFetching}>
                Atualizar
              </Button>
            }
          >
            <div className="row">
              <div className="col-6 col-md-3">
                <FormField label="Janela (dias)">
                  {({ id }) => (
                    <Input
                      id={id}
                      inputMode="numeric"
                      value={diasCampo}
                      onChange={(e) => setDiasCampo(e.target.value.replace(/\D/g, ''))}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>

        <DataTable
          caption={`Licenças vigentes a vencer em até ${dias} dias`}
          columns={colunas(false)}
          rows={aVencer.data}
          rowKey={(l) => l.id}
          loading={aVencer.isLoading}
          error={aVencer.isError ? errorMessage(aVencer.error) : null}
          empty={
            <EmptyState
              icon="fas fa-circle-check"
              title="Nenhuma licença a vencer"
              description="Nenhuma licença vigente vence dentro da janela informada."
            />
          }
        />
      </CardSecao>

      <CardSecao titulo="Histórico por estabelecimento" className="mt-4">
        <form className="br-form mb-3" onSubmit={(e) => e.preventDefault()}>
          <EstabelecimentoVisaPicker label="Estabelecimento" value={estabId} onChange={setEstabId} />
        </form>

        {estabId !== '' && (
          <DataTable
            caption="Licenças do estabelecimento"
            columns={colunas(true)}
            rows={doEstab.data}
            rowKey={(l) => l.id}
            loading={doEstab.isLoading}
            error={doEstab.isError ? errorMessage(doEstab.error) : null}
            empty={
              <EmptyState
                icon="fas fa-file-lines"
                title="Sem licenças"
                description="Este estabelecimento ainda não possui licenças emitidas."
              />
            }
          />
        )}
      </CardSecao>

      <EmitirLicencaModal open={emitirAberto} onClose={() => setEmitirAberto(false)} />
      {renovar && (
        <RenovarLicencaModal open={renovar !== null} onClose={() => setRenovar(null)} licencaId={renovar.id} />
      )}
      <VisaMotivoModal
        open={cassar !== null}
        onClose={() => setCassar(null)}
        title={`Cassar licença ${cassar?.numero ?? ''}`}
        label="Motivo da cassação"
        acaoLabel="Cassar"
        sucessoMensagem="Licença cassada."
        pendente={cassarMut.isPending}
        executar={(motivo) => cassarMut.mutateAsync(motivo)}
      />
    </>
  );
}
