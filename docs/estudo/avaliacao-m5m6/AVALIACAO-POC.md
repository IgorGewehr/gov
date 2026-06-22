# Avaliação de Prova de Conceito — gatilho M5/M6

> **Olhar:** Comissão de Recebimento / banca técnica de PoC (Lei 14.133/2021, art. 17 §3º / 41 II).
> **Método:** leitura do CÓDIGO REAL em `src/`, contagem de testes em `tests/`, `progresso.json`, contra
> a régua `PLAYBOOK-PROVA-DE-CONCEITO.md`. **Ignorado** `docs/diagnostico/*` (foto pré-M0).
> **Data:** 2026-06-22. Avaliador independente — não assumo quem construiu.
> **Não rodei** `dotnet build/test` nem subi a porta 5080; baseio-me em código + evidências de runtime já registradas. `[a confirmar]` o que não pude verificar no código.

---

## 0. Estado factual lido (não a narrativa)

- **Motor de Folha EXISTE e é real** (`MotorDeCalculoFolha.cs`, 123 linhas): INSS/RPPS progressivo + IRRF (base após previdência/dependentes/pensão), **tabelas parametrizadas por tenant/competência** (sem alíquota hardcoded), **fail-closed** se faltar tabela do regime, líquido nunca negativo, arredondamento `AwayFromZero`. Testado por invariantes nomeadas (`Invariante_4_separacao_rpps_rgps`, `Invariante_6_abate_teto`, `Borda_10_liquido_nunca_negativo`, INSS faixas contíguas). **81 testes** no módulo RH.
- **Motores de Tributos EXISTEM e são reais**: `CalculoIptu`, `CalculoIss`, `CalculoItbi`, `CalculoValorVenal` (~100 linhas cada) + cadastro imobiliário (`Imovel`, `PlantaValores`/PGV, `FatorPgv`), `Dam` (carnê: cota única ou N parcelas com vencimentos), Dívida Ativa/CDA. Endpoints expostos em 3 arquivos (IPTU, ISS/ITBI, contribuinte/dívida). **Testes `MotorIptuTests` / `MotorItbiTests` existem.**
- **Conclusão sobre o gatilho:** o board e a `AUTOAVALIACAO-POC.md` (2026-06-22) ainda dizem "Folha sem motor" e "motor de Tributos ausente" — **o código de hoje SUPERA essa autoavaliação**. Os **dois reprovadores municipais de motor foram fechados.** `progresso.json` marca M5/M6 como `pending` mas descreve "fundação" entregue — **a verdade está no código: fundação SIM, módulo completo NÃO.**

---

## TRILHA LEGISLATIVA (Câmara)

### Nota de prontidão: **78%** — PoC de "software de processo legislativo": **APROVA** com margem. ERP-de-Câmara completo: **reprova** (folha/eSocial/portal cidadão).

**Pontos fortes reais (lidos no código):**
- **Núcleo legislativo profundo e fim-a-fim.** ~50 endpoints: proposições + tramitação + distribuição + emendas + pareceres + autógrafo + arquivamento; sessões + presença/quórum + **ata** (`GerarAtaDaSessao`); **votação nominal** com abrir/encerrar/**cancelar**, **placar**, **painel ao vivo**, votos nominais; **tribuna com cronômetro** (inscrição/início/pausa/retomada/encerramento — `PausaFala`, `InscricaoOrador`, `TribunaCronometro.tsx`); **comissões** com membros + extinção; **Normas Jurídicas** (cadastro/busca/revogação/alteração/origem); **Diário Oficial eletrônico** (edições/matérias/publicação/retificação + visão pública). **126 testes** — o módulo mais testado.
- **Frontend correspondente existe** (~40 telas .tsx): PainelAoVivo, PainelPlacar, TribunaCronometro, Votacao*, Sessao*, Proposicao*, Comissao*, Norma*, Diario*, AtaView.
- **Fecha os 3 gaps que a autoavaliação anterior listava** (Normas, Diário Oficial, cronômetro/oradores) — **eles agora existem no código.**
- **Integração com sanção/veto** (`ReceberSancaoHandler`/`ReceberVetoHandler`) — ponte Câmara↔Executivo.

**Gaps que poderiam pesar:**
- **Deliberação remota** (voto/tribuna online em tempo real): ausente — **diferencial, não mínimo universal** (Curitiba). Não reprova em PoC típica.
- **Terminais físicos de plenário / painel LED / hot-swap:** fora de escopo de software — só reprova se o edital for "hardware+software de plenário" (Vitória/ES). `[a confirmar]` no edital-alvo.
- **Transmissão de vídeo (TV Câmara):** não é nosso escopo; integra-se, não se entrega.
- **Geração automática da Ata Sintética ao FIM da sessão** (Vitória, literal): existe `GerarAtaDaSessao` + endpoint `/sessoes/{id}/ata`; `[a confirmar]` se dispara automaticamente no encerramento ou é ação manual.
- **ERP-de-Câmara** (Belacruz/CE, Salto/SP): folha da Câmara depende do mesmo módulo RH — ver trilha municipal.

---

## TRILHA MUNICIPAL (Executivo)

### Nota de prontidão: **66%** — PoC de ERP municipal COMPLETO eliminatória: **REPROVA HOJE**, mas por margem menor que a autoavaliação anterior (que dava ~60–65% com os motores ausentes). Núcleo fiscal e os dois motores agora rodam; os reprovadores migraram para **planejamento orçamentário, eSocial, portal do cidadão e arrecadação (PIX/QR)**.

**Pontos fortes reais (lidos no código):**
- **Núcleo contábil/fiscal — o mais escrutinado na PoC — está entregue e é o diferencial.** PCASP (plano de contas, partida dobrada via Outbox, balancete ΣD=ΣC), ciclo Lei 4.320 completo (Dotação→Empenho→Liquidação→Ordem de Pagamento→Restos a Pagar, com anulação/estorno), 4 DCASP (BO/BF/BP/DVP), **MSC**, **remessa TCE-RS SIAPC/PAD** (leiaute posicional, pré-validação RDI, empacotamento+hash, protocolo como ato humano), **reconciliação SICONFI**. ~30 endpoints em Finanças + Transparência. Assinatura A1/XMLDSig no módulo Cofre (16 testes).
- **Folha com motor legal correto** (ver §0) + ciclo de folha (abrir/eventos/apuração-legal/cálculo/fechamento/pagamento/**contracheque**).
- **Tributos com motores** IPTU/ISS/ITBI + cadastro imobiliário/PGV + carnê DAM + Dívida Ativa/CDA.
- **Compras/Licitações Lei 14.133 maduro:** licitação→julgar→homologar, contratos/aditivos, fornecedores/sanções/impedidos, **PNCP** (publicar edital + publicar contrato, com evento de integração).
- **Patrimônio/Almoxarifado/Frota** (71 testes) com depreciação.
- **Web nativo sem emulador** (SPA React + API .NET) — cláusula **literal e repetida** nos editais (Mata/RS §7.9, Riqueza §1.b). Trunfo citável, não marketing.

**Gaps que REPROVARIAM (eliminatórios ou corte de 95%/módulo):**
1. **🔴 PPA / LDO / LOA AUSENTES.** Busca no código: **0 referências** a `PlanoPlurianual`/`LeiDiretrizes`/`LeiOrcamentaria`/PPA/LDO/LOA. Existe só **execução** orçamentária (`DotacaoOrcamentaria`). ⚠️ **A `AUTOAVALIACAO-POC.md` afirma "PPA/LDO/LOA ✅ Temos (M3)" — isso é FALSO no código atual.** PPA/LDO/LOA é **[RECORRENTE]** (Playbook §2.3) e núcleo do módulo contábil/orçamentário. **Reprova o módulo mais cobrado.**
2. **🔴 eSocial ausente como transmissão.** As 17 ocorrências de "eSocial" em RH são apenas **classificação de rubrica (natureza S-1010)**, não eventos S-1200/S-1210 nem envио SOAP. Bloco eSocial é **obrigatório inteiro** em editais (Carbonita 549-565). **EFD-Reinf/DCTFWeb** idem ausentes.
3. **🔴 Ponto eletrônico (Port. MTP 671/2021) ausente.** "Ponto" no código = **ponto de pedido de estoque** (Patrimônio), não marcação/AFD/AEJ. (Mitigação: o Playbook marca este item como `[INCERTO]` em PoC — pode não ser cobrado.)
4. **🔴 Portal do Cidadão / autoatendimento ausente.** Nenhuma camada pública (2ª via de guia, CND, contracheque online ao servidor, consulta de protocolo). **[RECORRENTE]** em Mata/RS. A `Transparência` tem só geração de MSC/remessa/declaração — **não há portal LAI público em tempo real nem e-SIC** (`[a confirmar]` se há andaime mínimo; não localizei portal público).
5. **🟡 Arrecadação sem PIX/QR/código de barras.** `Dam.cs` gera parcelas/vencimentos mas **sem PIX via API nem código de barras/boleto registrado** (Playbook §2.4, [RECORRENTE]). Carnê imprimível existe; **meio de pagamento bancário não**.
6. **🟡 PNCP sem cliente HTTP real.** Grava número + emite evento de integração; `[a confirmar]` se há gateway HTTP que efetivamente publica no PNCP ou se é stub. **Mitigável** na PoC (Riqueza §14.6.6 dispensa integração de terceiros) → vira obrigação contratual.
7. **🟡 NFS-e por ingestão passiva (ADN), não emissão/escrituração ABRASF ativa.** Decisão de arquitetura (CLAUDE.md §8). Editais MG/RS presumem escrituração ativa → **risco de aderência ALTO**; `[a confirmar]` no edital-alvo concreto.
8. **🟡 Bloqueio mensal escalonado** (contabilidade governa abertura dos demais módulos — Carbonita item 59): fechamento existe, travamento cruzado **não provado**.
9. **🟡 Conciliação bancária CNAB/OBN:** `[a confirmar]` — não localizei `Conciliacao`/CNAB no código (a autoavaliação dizia "✅ Temos M4"; não confirmei).
10. **Folha — funções de competência ainda não vistas:** **13º, férias, rescisão** = **0 referências** no código (`DecimoTerceiro`/`Rescisao` ausentes; `Ferias` só 2 arquivos). Editais cobram "férias, 13º, rescisão, extra-folha" [RECORRENTE]. **O motor base existe; o ciclo anual da folha não.**

---

## Veredito de prontidão por trilha

| Trilha | PoC "núcleo de processo" | PoC ERP COMPLETO eliminatório |
|---|---|---|
| **Legislativa** | **~78% — APROVA** (Matão/Salto: processo legislativo + transparência) | ~60% — reprova (folha/eSocial da Câmara) |
| **Municipal** | núcleo fiscal/TCE **forte (≈90%)** | **~66% — REPROVA** (PPA/LDO/LOA, eSocial, portal cidadão, PIX) |

---

## Veredito honesto (banca)

1. **A narrativa do board subestima a si mesma nos motores e SUPERESTIMA em PPA/LDO/LOA.** Os motores de folha e tributos **existem e são tecnicamente corretos** (parametrizados, fail-closed, testados) — isso é real e bom. Mas a `AUTOAVALIACAO-POC.md` afirma "PPA/LDO/LOA ✅ Temos" e o código tem **zero**: numa banca, alegar aderência a item inexistente é o pior erro (Acórdão 2611/2016-TCU: produto diferente da amostra). **Corrigir esse claim antes de qualquer demo.**
2. **Trilha legislativa é a porta de entrada.** Vença primeiro uma PoC de "software de processo legislativo" (Matão/Salto). O sistema está pronto para isso, faltando só **confirmar Ata Sintética automática** e **parlamentar impedido de votar** (`[a confirmar]`).
3. **Trilha municipal reprova hoje num ERP completo** — mas por **4 gaps nomeáveis e fecháveis**, não por fragilidade de núcleo: **(a) PPA/LDO/LOA**, **(b) eSocial+EFD-Reinf**, **(c) Portal do Cidadão**, **(d) PIX/QR na arrecadação** + ciclo anual da folha (13º/férias/rescisão). O núcleo PCASP/SIAPC/SICONFI/A1 — onde a concorrência legada sofre — é nossa liderança.
4. **Antes de demonstrar qualquer remessa:** confirmar o **leiaute SIAPC/PAD e regras MSC do exercício vigente** (`[OFICIAL]` — TODO MT-2026) e ter **massa de demonstração** nos dois tenants (Prefeitura + Câmara, CNPJs distintos, sem vazamento ABAC/UO).

---

## Caminho (tira de "reprova" para "aprova")

1. **Corrigir a `AUTOAVALIACAO-POC.md`:** rebaixar PPA/LDO/LOA de "✅ Temos" para "🔴 Falta". Honestidade documental protege a empresa na banca.
2. **Primeiro alvo = Legislativo (processo legislativo).** Validar Ata automática + impedimento de voto; ensaiar sessão cronometrada (~5-7 min/item). Vitória provável.
3. **Sprint municipal de desbloqueio (ordem de impacto eliminatório):**
   - **PPA/LDO/LOA** (núcleo do módulo mais cobrado; hoje é o maior buraco).
   - **eSocial** (eventos S-1200/S-1210 + lote + retorno; A1 já existe no Cofre) + EFD-Reinf.
   - **Portal do Cidadão mínimo** (2ª via de guia/DAM + CND + consulta de protocolo) — barato e [RECORRENTE].
   - **PIX/QR + código de barras** no DAM (arrecadação registrada).
   - **Ciclo anual da folha** (13º, férias, rescisão, extra-folha).
4. **Resolver o risco NFS-e** no edital-alvo concreto: se exigir emissão/escrituração ABRASF ativa, decidir escopo (hoje é ingestão ADN passiva por arquitetura).
5. **Usar o diferimento de 5%** dos editais (Carbonita 120d / Mata 30d) para o resíduo (PNCP cliente, EFD-Reinf, ponto).
6. **Explorar na ata da banca:** web nativo sem emulador (cláusula literal) + prestação TCE-RS/SICONFI provada.

> **Fechamento honesto:** o sistema avançou **além** da sua própria autoavaliação nos motores (folha e tributos rodam), o que **não** é capturado pelo `progresso.json` (que ainda marca M5/M6 `pending`). Mesmo assim, **não passaria hoje numa PoC municipal de ERP completo** — agora reprovado principalmente por **PPA/LDO/LOA, eSocial, Portal do Cidadão e arrecadação PIX**, não mais por "falta de motor". A **trilha legislativa de processo legislativo está pronta para PoC.**
