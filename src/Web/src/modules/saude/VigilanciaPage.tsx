// Página da VIGILÂNCIA SANITÁRIA (VISA) — orquestra as 4 frentes em abas internas (gov.br
// br-tab): Estabelecimentos fiscalizáveis, Inspeções/vistorias, Autos do processo
// administrativo e Licenças/alvarás. Acesso gated em "saude.vigilancia.ver"; cada ação
// interna tem seu próprio gating granular (gerenciar/inspecionar/autuar/licenciar).
import { useState } from 'react';
import { Alert, PageHeader } from '../../components/ui';
import { useHasPermission } from '../../auth/Can';
import { SaudeSubNav } from './SaudeSubNav';
import { VisaEstabelecimentosTab } from './VisaEstabelecimentosTab';
import { VisaInspecoesTab } from './VisaInspecoesTab';
import { VisaAutosTab } from './VisaAutosTab';
import { VisaLicencasTab } from './VisaLicencasTab';

type Aba = 'estabelecimentos' | 'inspecoes' | 'autos' | 'licencas';

const ABAS: ReadonlyArray<{ id: Aba; label: string }> = [
  { id: 'estabelecimentos', label: 'Estabelecimentos' },
  { id: 'inspecoes', label: 'Inspeções' },
  { id: 'autos', label: 'Autos' },
  { id: 'licencas', label: 'Licenças' },
];

export function VigilanciaPage() {
  const podeVer = useHasPermission('saude.vigilancia.ver');
  const [aba, setAba] = useState<Aba>('estabelecimentos');

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Vigilância Sanitária (VISA)"
        description="Estabelecimentos fiscalizáveis, inspeções/vistorias, autos do processo administrativo sanitário e licenças/alvarás."
      />

      {!podeVer ? (
        <Alert variant="warning">
          Você não tem permissão para visualizar a Vigilância Sanitária.
        </Alert>
      ) : (
        <>
          <nav className="br-tab mb-4" aria-label="Frentes da Vigilância Sanitária">
            <ul className="tab-nav" role="tablist">
              {ABAS.map((a) => (
                <li
                  key={a.id}
                  className={`tab-item${aba === a.id ? ' is-active' : ''}`}
                  role="presentation"
                >
                  <button
                    type="button"
                    role="tab"
                    id={`tab-visa-${a.id}`}
                    aria-controls={`painel-visa-${a.id}`}
                    aria-selected={aba === a.id}
                    onClick={() => setAba(a.id)}
                  >
                    <span className="name">{a.label}</span>
                  </button>
                </li>
              ))}
            </ul>
          </nav>

          {aba === 'estabelecimentos' && (
            <div role="tabpanel" id="painel-visa-estabelecimentos" aria-labelledby="tab-visa-estabelecimentos">
              <VisaEstabelecimentosTab />
            </div>
          )}
          {aba === 'inspecoes' && (
            <div role="tabpanel" id="painel-visa-inspecoes" aria-labelledby="tab-visa-inspecoes">
              <VisaInspecoesTab />
            </div>
          )}
          {aba === 'autos' && (
            <div role="tabpanel" id="painel-visa-autos" aria-labelledby="tab-visa-autos">
              <VisaAutosTab />
            </div>
          )}
          {aba === 'licencas' && (
            <div role="tabpanel" id="painel-visa-licencas" aria-labelledby="tab-visa-licencas">
              <VisaLicencasTab />
            </div>
          )}
        </>
      )}
    </>
  );
}
