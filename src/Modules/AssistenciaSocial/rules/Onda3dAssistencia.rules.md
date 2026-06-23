# Onda3d Assistência — Regras (Rules-as-Code) — PBF + Censo SUAS + EstimativaIgd

> Largura operacional da Assistência reusando `Familia`/`RMA`/`Prontuário`/`Benefício` por Id (FK lógica),
> **sem nenhuma credencial oficial** (CLAUDE.md §8). A base MDS é autoritativa; o que é feito aqui é o
> acompanhamento/gestão **local** — a integração oficial (Sistema de Condicionalidades/SICON/CECAD,
> Censo SUAS/SAGI, IGD oficial, AgilizaSUAS) fica **deferida para M10** atrás da ACL.
> Marco legal: PBF (Lei 14.601/2023; Decreto 11.617/2023; Portaria MDS 978/2023); Tipificação Nacional
> (Res. CNAS 109/2009); NOB-RH/SUAS. Valores/cortes/prazos são parâmetros versionados por tenant.

## 3d.1 — PBF: acompanhamento de condicionalidades
- **I-1.** O acompanhamento é único por **(tenant, família, competência)** — abertura idempotente na competência.
- **I-2.** A família do CadÚnico deve existir no tenant (FK lógica) — base federal autoritativa, benefício
  federal **nunca** é alterado por este agregado.
- **I-3.** Cada registro de condicionalidade é de um membro, num eixo (Educação frequência; Saúde
  vacina/nutrição infantil; Saúde pré-natal) e um status (pendente/cumprida/descumprida/justificada).
- **I-4.** O **efeito gradativo** deriva da contagem de descumprimentos EFETIVOS (descumprida e não
  justificada): 1 → advertência; 2 → bloqueio; 3+ → suspensão. É efeito GERENCIAL local (busca ativa do CRAS).
- **I-5.** Justificar um descumprimento exige motivo registrado, retira-o da contagem efetiva e **recalcula**
  o efeito (pode reduzir a gradação).
- **I-6.** Quando o efeito ESCALA para impacto (≥ bloqueio) emite-se `CondicionalidadeDescumprida` (apenas
  efeito + contagem agregada — nunca dado sigiloso do membro; LGPD art. 11).

## 3d.2 — Censo SUAS: unidades + consolidado anual
- **I-7.** A unidade socioassistencial (CRAS/CREAS/Centro POP) promove o `UnidadeAtendimentoId` a cadastro
  de gestão (estrutura, equipe de referência — NOB-RH/SUAS, serviços ofertados com capacidade).
- **I-8.** Serviço ofertado respeita a Tipificação Nacional: PAIF só em CRAS; PAEFI só em CREAS; SCFV em ambos.
- **I-9.** O formulário do Censo é único por **(tenant, unidade, exercício)**; consolida DERIVANDO o volume
  anual de atendimentos do RMA e as famílias referenciadas do cadastro de famílias (sem dupla digitação).
- **I-10.** Reconsolidar é idempotente enquanto em consolidação; o fechamento **sela** o exercício (terminal).
- **I-11.** Tudo isolado por tenant (Global Query Filter); indicadores agregados — nunca dado sigiloso (LGPD art. 11).

## 3d.3 — EstimativaIgd (local, não oficial)
- **I-12.** O índice é a média simples de três fatores LOCAIS em [0,1] (atualização cadastral × cumprimento
  de condicionalidades × gestão de benefícios), clampeados em [0,1].
- **I-13.** Toda estimativa carrega o **rótulo obrigatório** "estimativa gerencial local — não é o IGD oficial (MDS)".

## Cenários (BDD)
**Cenário: Descumprimento de condicionalidade aplica efeito gradativo**
- **Dado** o acompanhamento de condicionalidades de uma família numa competência
- **Quando** dois descumprimentos efetivos são registrados
- **Então** o efeito escala para bloqueio e `CondicionalidadeDescumprida` é emitido (busca ativa do CRAS).

**Cenário: Censo consolida o volume do RMA da unidade**
- **Dado** RMAs consolidados de uma unidade no exercício e famílias referenciadas
- **Quando** o Censo da unidade é consolidado
- **Então** o volume anual deriva do somatório dos RMAs e as famílias referenciadas do cadastro.

**Cenário: Estimativa do IGD a partir de indicadores locais**
- **Dado** os fatores locais de atualização cadastral, condicionalidades e gestão de benefícios
- **Quando** a estimativa do IGD é calculada
- **Então** o índice é a média dos fatores (0–1) com o rótulo de estimativa local não oficial.

<!-- manifest
commands: AbrirAcompanhamentoCondicionalidade, RegistrarCondicionalidade, JustificarDescumprimento, CadastrarUnidadeSocioassistencial, ConfigurarUnidade, ConsolidarCenso, FecharCenso
queries: ObterDescumprimentos, ListarUnidadesSocioassistenciais, ObterCenso, EstimarIgd
domainEvents: CondicionalidadeDescumprida
integrationEventsPublished: 
integrationEventsConsumed: 
-->
