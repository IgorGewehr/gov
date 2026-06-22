# DIRECIONAMENTO — Síntese estratégica para o dono do produto

> **O que é este documento.** Não é um resumo dos outros docs — é uma **opinião de
> direcionamento**. Cruza os designs M5–M9, a auditoria de plano/arquitetura, a autoavaliação de
> PoC, o playbook de licitações, a validação real contra o TCE-RS e o diagnóstico-vs-real, e diz:
> **onde estamos de verdade, qual a sequência até o M10, qual trilha comercial atacar primeiro, o
> que depende de você (dono), quais os riscos que não são técnicos triviais, e os 3–5 próximos
> passos de maior alavancagem.** Tom direto. `[a confirmar]` = não verificável só pelo repositório.
> Data: 2026-06-22. Piloto: Maximiliano de Almeida/RS (TCE-RS).
>
> **Fontes cruzadas:** `AUDITORIA-PLANO-ARQUITETURA.md`, `DIAGNOSTICO-VS-REAL.md`,
> `VALIDACAO-REAL-TCE.md`, `prova-de-conceito/{AUTOAVALIACAO-POC,PLAYBOOK-PROVA-DE-CONCEITO}.md`,
> `architecture/m{5,6,7,8,9}-prep/M*-DESIGN.md`, `progresso/progresso.json`,
> `planejamento/PLANO-MESTRE.md`, `CLAUDE.md`, ADRs 0005/0009/0013.

---

## TL;DR (a tese, em 6 linhas)

1. A **espinha fiscal (M0–M4) está pronta e provada em runtime** — é o ativo mais difícil do mercado e está do nosso lado. Falta o **carimbo do oficial** (MT/PAD 2026, certificado real), que é insumo seu, não código.
2. Existem **duas trilhas comerciais**, e elas têm prontidões muito diferentes: **legislativa (~70–75%, a 1 sprint de uma PoC vencível)** e **municipal (~60–65%, precisa M5+M6 inteiros)**.
3. **Recomendação central: vença uma PoC legislativa primeiro** (prova de mercado barata e real), enquanto o motor pesado (M5 folha, M6 tributos) avança para destravar a trilha municipal.
4. O caminho até o M10 **não é o Plano-Mestre original** — é o Plano-Mestre **já reordenado pela auditoria** (encerramento de exercício, tesouraria cedo, PNCP antecipado, destinos fiscais federais, quebra de workflows gigantes).
5. **Cinco insumos dependem só de você** e bloqueiam "validação real": MT 2026 + PAD oficial, certificado A1 homologado, RPPS-vs-RGPS do piloto, infra/tenant Azure, e credenciais de homologação (eSocial Produção Restrita, gov.br login).
6. O maior risco não é técnico — é **dispersão de escopo** (11 módulos, ~48 workflows) sem uma venda que financie o foco. Trate a PoC legislativa como o evento que disciplina o roadmap.

---

## 1. Onde estamos — provado em runtime (não "no papel")

O que mudou desde a foto pré-M0 (`ESTADO-ATUAL.md`, que ainda diz "PCASP = 0 linhas") é grande, e a régua honesta está em `DIAGNOSTICO-VS-REAL.md`:

**Espinha fiscal M0–M4 — entregue e provada:**
- **M0:** build verde, `EmpenhoRepository` implementado, segurança de borda fechada.
- **M1:** autorização organizacional real (UO + escopo ABAC + regra I4 "não delega o que não tem") com frontend — ADR-0007. É exigência de SoD do TCE e base de todo enforcement; estar pronta cedo evita retrabalho em Finanças/RH/Tributos.
- **M2:** contabilidade PCASP **síncrona e transacional** — partida dobrada com invariante de domínio **ΣD=ΣC**, roteiros MCASP, lançamento automático por evento via Outbox, balancete que fecha. A auditoria **refutou o mito** de "contabilidade eventualmente consistente": o lançamento PCASP ocorre na mesma UoW do fato (ADR-0013). Cofre A1 (envelope encryption, `SignedXml`+`X509`) provado seguro (ADR-0008).
- **M3:** 7 demonstrações DCASP + **MSC derivada dos lançamentos com publisher real** — a ponte Finanças→Transparência (`MSCGeradaIntegrationEvent`) fecha em runtime (ADR-0011).
- **M4 — meta nº1 do dono, ATINGIDA:** remessa SIAPC/PAD **posicional de verdade** (Latin-1, CR/LF, largura fixa via `Span<char>`), e-Validador local fiel ao MT, empacotamento ZIP+hash, reconciliação SICONFI via API de **consulta** (Dados Abertos, HTTP+Polly). **A transmissão é ato humano por design** — ADR-0009 provou que **não existe API de upload** no TCE-RS (PAD desktop + ICP pessoal) nem no SICONFI (upload manual com e-CPF A3). Isso não é uma lacuna: é a realidade do canal, corretamente modelada.

**Legislativo — demonstrável (demo-enablers provados):** vereadores nomeados, painel ao vivo com apuração/quórum, ata, seed. O `verify` pegou e corrigiu um bug de `IsInEnum` que travava a votação por HTTP. Em execução: parte 2 (Normas/Diário/cronômetro/comissão) + frontend do painel.

**Baseline no GitHub:** `github.com/IgorGewehr/gov`, `main 1bbf345` ("Baseline: espinha fiscal (M0-M4) + autorizacao + legislativo demonstravel"), repo limpo (0 segredos/dbs versionados). A **regressão do Outbox** (a guarda da auditoria H5 expôs uma fragilidade latente) foi **corrigida e provada** — isolamento por mensagem, contabilização de volta, balancete fecha.

**Build/testes (estado declarado):**
- Build: **0 erros / 0 avisos** (warnings-as-errors, NRT).
- Backend: **694** testes verdes (progresso.json) — `DIAGNOSTICO-VS-REAL.md` mediu **705** em sessão (16 assemblies). A diferença é de versão da foto; ambos = **0 falhas**. `[a confirmar]` o número exato no CI da branch atual.
- Frontend: **181** testes verdes. Módulos backend: **13**. Marcos concluídos: **5/11** (espinha fiscal).
- Nota de ambiente: a máquina só tem runtime .NET 10; o alvo é net8.0, então roda com `DOTNET_ROLL_FORWARD=Major` — limitação do host, não do código.

**Veredito honesto:** o núcleo mais escrutinado pelo TCE existe, é rico e roda fim-a-fim. O que falta no núcleo é o **carimbo do oficial** (leiaute exato 2026, validação no PAD real com certificado) — e isso é **insumo do dono**, não engenharia. O que falta no produto são os **módulos que M5/M6/M8 entregam** (folha, tributos, cidadão), que ainda não existem como motores.

---

## 2. Caminho até o M10 — sequência recomendada (já com as reordenações da auditoria)

> Esta é a sequência que eu seguiria, **incorporando as correções de `AUDITORIA-PLANO-ARQUITETURA.md`**
> (que o Plano-Mestre já absorveu em parte). A regra é: **robustez barata primeiro → trilha comercial
> mais próxima → motores pesados → compliance que cresce com o tempo → capstone**. "Depende de" é
> explícito; o que é `[OFICIAL]` espera o insumo do exercício antes de virar requisito.

### Fase 0 — Robustez P0 e fechamento do plano (dias, sem bloquear nada)
São correções de baixo esforço e alto valor que a auditoria marcou como P0/P1; muitas já foram feitas (a guarda H5/Outbox), mas confirme as três:
- **K6** — invalidação do cache de connection string (TTL + `Invalidar(tenantId)` no provisionamento/rotação). Crítico: apontar para banco obsoleto que move verba é incidente sério.
- **H1** — Outbox `AttemptCount`+`NextAttemptUtc`+dead-letter (poison message não pode reprocessar eterno).
- **H5** — `ModuleUnitOfWork` falha-alto no 2º `Definir` divergente (a regressão recente nasceu daqui).
- **K1/K2** — Plano-Mestre re-sincronizado (M0–M4 = ENTREGUE) e W4.2/W4.3 reescritos conforme ADR-0009 (gerar+validar+empacotar+reconciliar, **não** "transmitir"). **Já refletido** no Plano-Mestre atual.

### Fase 1 — Trilha legislativa até PoC-vencível (1 sprint) — **fazer primeiro**
Origem: `AUTOAVALIACAO-POC.md` (legislativo ~70–75%) + `PLAYBOOK` §3. Fecha 3–4 gaps de baixo custo que hoje reprovam um edital tipo Matão/Salto:
- **Normas Jurídicas** consultáveis (recorrente E1/E3/SAPL).
- **Diário Oficial eletrônico** (recorrente E1).
- **Cronômetro de tribuna + inscrição de oradores** (recorrente E2/E3).
- **Validar**: geração **automática** de Ata Sintética ao fim da sessão + cadastro de **membros de comissão** (efetivos/suplentes + pauta/ata).
- **Não** entrar agora: deliberação remota e terminais físicos (são diferencial/condicional ao edital, não mínimo universal); LexML (não aparece em texto de edital — fica para M9/W9.5).
- *Depende de:* núcleo legislativo já entregue (existe). *Não depende de:* M5/M6.

### Fase 2 — M5 (RH/folha) — destrava a maior reprovação municipal
Origem: `M5-DESIGN.md`. Ordem interna de risco do próprio design:
- **Fase 0 do M5 (bloqueia tudo):** congelar fontes oficiais (MOS S-1.3 + XSD, Portaria 671, MT SIAPC Vol. V, tabelas INSS/IRRF 2026).
- **W5.1 — motor de folha (Rules-as-Code):** fundação; eSocial e TCE-RS consomem o resultado. Fórmulas no código, **números versionados por competência+tenant** (fail-closed sem tabela municipal).
- **W5.2 — eSocial:** **quebrar** (auditoria H7) em (a) tabelas S-1000/1005/1010/1020/1070, (b) periódicos S-1200/1202/1299, (c) admissão/desligamento S-2200/2299. Reusa o `AssinadorXmlDsig` do Cofre (não reescrever assinatura). É o **único destino com homologação real automatizável** (XSD + Produção Restrita).
- **W5.3 — Ponto AFD/AEJ:** pode correr em paralelo após Fase 0. **Atenção crítica:** a Portaria 671 regula a **CLT**; servidor estatutário segue lei municipal/RJU — **confirmar se 671 se aplica ao piloto** antes de priorizar (pode sair do MVP).
- **W5.4 — remessa folha TCE-RS:** reusa o pipeline local de empacotamento/validação do W4.2 (gerar+validar, não enviar).
- **W5.6 — EFD-Reinf (R-2010/R-4020) + DCTFWeb** (auditoria K4): **lacuna que gera recolhimento irregular** — o ente é tomador/retentor sobre as NFS-e já ingeridas via ADN; EFD-Reinf substituiu a DIRF (extinta 2024). Mensal (dia 15), multa R$ 500–1.500/mês. *Depende de:* `[a confirmar]` regime de órgão público.
- **W5.7 — CADPREV/RPPS:** **só se o piloto tiver RPPS** (`[a confirmar]`). Se for RGPS, **sai do escopo** (vira excesso).
- *Depende de:* W2.4 (contrapartida contábil), W1.3 (escopo), W2.1 (A1).

### Fase 3 — M6 (Tributos) — destrava a segunda reprovação municipal
Origem: `M6-DESIGN.md`. Ordem interna:
- **§0 — parâmetros fiscais versionados por tenant+vigência (bloqueia tudo):** nenhum número fiscal no código; o motor lê a regra vigente no fato gerador.
- **Cadastro Imobiliário (BCI)** → **motor IPTU/PGV** → **arrecadação `Dam`/`Parcela` + código de barras (44 dados / 48 linha digitável) + PIX** → **CNAB 240 conciliação** (publica `ReceitaArrecadadaIntegrationEvent`→Finanças) → **ISS** (evoluir ingestor ADN para cursor NSU/DF-e + mTLS; dedup pela chave inteira; `ApuradorIss`) → **ITBI/Taxas/Alvará/COSIP/Melhoria** → **Dívida Ativa: completar CDA + `SubstituirCda` versionada + relógio de prescrição real** (substitui o `EstaPrescrita` ingênuo; errar = perda de crédito + responsabilização TCE).
- **W6.2 (IPTU)** também deve ser **quebrado** (auditoria H7): apuração/lançamento separado de emissão/arrecadação.
- **CIB→SINTER fica para depois** (piloto não-capital só obrigado jan/2027); modelar campo opcional, **não** implementar o envio.
- *Depende de:* W1.3/W1.6 (escopo + sigilo fiscal). *Risco de aderência:* **NFS-e ativa vs. ingestão passiva** (ver §5).

### Fase 4 — Encerramento de exercício (W3.4) — inserir ANTES da prestação anual
Origem: auditoria **K3**. Hoje a apuração de superávit/déficit, transposição de saldos e abertura do exercício seguinte estão **pulverizadas**. Sem encerramento, **a MSC de dezembro e a DCA anual nascem incorretas** — falha que só aparece em jan/mar e **trava a parte mais escrutinada pelo TCE**. Tem verbo SoD próprio (`financas.exercicio.encerrar`). *Depende de:* W2.4, W3.3. *Alimenta:* W4.3 (DCA/MSC de Encerramento). **Tratar como pré-condição da virada de exercício, não como item de M9.**

### Fase 5 — M7 (Saúde/Educação/Assistência) — eixo fiscal primeiro
Origem: `M7-DESIGN.md`. A descoberta-chave: os 3 domínios têm **o mesmo esqueleto fiscal** com nomes diferentes — **fatorar, não triplicar**.
- **M7.0 núcleo compartilhado** (`CalendarioFederal`, `FonteRecursoVinculado` estendendo o PCASP, `ApuradorMinimo`, `ParecerConselho`, `EnvioGatewayBase`) → **M7.1 mínimos constitucionais** (15% ASPS, 25% MDE, 70% FUNDEB → Anexos 12/8 no **mesmo PAD mensal do M4**) → **M7.2 fundos por bloco/piso** → **M7.3 conselhos** → **M7.4 Censo/produção** → **M7.5 integrações em tempo real (RNDS mTLS, BNAFAR, CNES, SIAPS)**.
- **Eixo fiscal é o de maior valor e menor risco** (reusa M2/M3/M4); as **integrações de envio ficam por último e BLOQUEADAS-SEM-LEIAUTE**. Tudo (% mínimos, prazos, sistema-alvo, rótulos AVE/REV) é **parâmetro versionado** — a janela regulatória mexe em meses (SISAB→SIAPS, PNAE 30%→45%, HÓRUS→e-SUS AF).
- **Promover SIOPS e CADPREV a workflows próprios** (auditoria H9), não diluídos.

### Fase 6 — M8 (Cidadão + Gestor/BI + LAI) — valor de conformidade cedo, gov.br por último
Origem: `M8-DESIGN.md`. Ordem que entrega conformidade legal sem depender do bloqueio externo:
- **Transparência ATIVA + dados abertos + LGPD gate** → **e-SIC (SLA LAI 20+10, sem login obrigatório)** → **Painel do Gestor/BI** → **Carta de Serviços + Ouvidoria (SLA 30+30) + Avaliação art. 23** → **Identidade gov.br RP (A.1) por último** (é o bloqueador externo de cronograma).
- **Mínimo do Portal do Cidadão** (2ª via de guia + abrir/consultar protocolo) é **recorrente em editais municipais e barato** — adiantar um MVP dele pode valer pontos numa PoC municipal.
- *Risco de cronograma:* ser RP gov.br exige **adesão formal do ente** à Rede Nacional de Governo Digital (`[a confirmar]` se Maximiliano aderiu) — por isso fica isolado no fim.

### Fase 7 — M9 (Suprimentos + compliance + robustez) — **PNCP antecipado**
Origem: `M9-DESIGN.md` + auditoria B-1.
- **PNCP é dívida de compliance ATIVA, não feature nova:** o sistema já empenha (move dinheiro), mas a publicação no PNCP é só um flag. **Art. 94 = condição de eficácia** do contrato; sem ela o contrato é ineficaz → glosa do TCE. **Antecipar para logo após o M4 / início do M9** (frente bloqueante M9-A) — não deixar no penúltimo marco. Inclui o relógio de prazo (dias úteis + feriados) reusado por Convênios/Protocolo. Correções legais confirmadas: eficácia/prazos = **art. 94** (não 174); gestão = **Decreto 10.764/2021** (não 11.462).
- **Demais frentes:** M9-B suprimentos (Almox/Frota reusam domínio pronto; Obras é agregado novo) → M9-C compliance documental (ACT ICP RFC 3161, CONARQ/e-ARQ v2, LexML, art. 29-A) → M9-D Convênios (**Transferegov** — auditoria H10: convênio sem PC → inadimplência → sem repasses) → **W9.9 SADIPEM/CDP** (auditoria K5: anual, passivo PCASP → STN; não atualizar → **CAUC negativo → bloqueia transferências e crédito**) → **W9.10 conformidade SIAFIC** (o nosso ERP **é** o SIAFIC do ente — autodeclaração + base única) → M9-E robustez/QA transversal.
- **QA/a11y transversal por workflow** (auditoria M-6), não concentrado só aqui.

### Fase 8 — M10 (capstone: prontidão para PoC + go-live)
Origem: `progresso.json` M10. Definição de pronto = **validado no OFICIAL + deployável no município**:
- **W10.1** validação oficial real (e-Validador/PAD MT2026 + eSocial homologação) — certifica simulado→oficial.
- **W10.2** avaliações isentas finais (PoC + engenharia + current-state) + correções. *(Memória do projeto: lançar 2 workflows de avaliação isenta no M5/M6 concluído — olhos frescos PoC + auditoria de engenharia. Antecipar essa prática, não esperar o M10.)*
- **W10.3** PoC demonstrável end-to-end (legislativa + municipal) + seed + roteiro.
- **W10.4** go-live: infra Azure, provisionamento de tenant, migração inicial, backup/DR/OTel, segurança/LGPD final.
- **W10.5** limpeza de produção + docs finais.

**Cadeia de dependências dura (o que trava o quê):**
`A1/Key Vault (W2.1)` é pré-requisito transversal de TCE, SICONFI, eSocial, RNDS, PNCP e Protocolo — por isso entrou cedo. `PCASP→balancete→MSC` alimenta **todas** as prestações (SIAPC, SICONFI, Anexos setoriais). `Encerramento (W3.4)` é pré-condição da DCA/MSC anual. `Motor de folha (W5.1)` alimenta eSocial, remessa TCE e o 70% FUNDEB (M7). `Parâmetros fiscais (M6 §0)` bloqueiam todos os motores tributários. `Calendário de dias úteis (M9-A)` é reusado por Convênios e Protocolo.

---

## 3. As duas trilhas comerciais — qual priorizar

| Dimensão | **Trilha Legislativa (Câmara)** | **Trilha Municipal (Executivo)** |
|---|---|---|
| Prontidão hoje | **~70–75%** (`AUTOAVALIACAO-POC.md`) | **~60–65%** |
| Veredito de PoC | **Provável aprovação** numa PoC de "processo legislativo" (Matão/Salto) | **Reprova hoje** numa PoC de ERP completo |
| O que falta | 3–4 gaps baratos (Normas, Diário, cronômetro, validar Ata/comissões) | **M5 inteiro** (folha+eSocial) **e M6 inteiro** (IPTU/ISS) — módulos sem motor |
| Por que reprova | (só se for ERP-de-Câmara: folha sem motor) | Corte é **95%/módulo eliminatório**; folha e tributos não rodam fim-a-fim; **Portal do Cidadão ausente** |
| Esforço até vencer | **1 sprint** | **2 marcos pesados (M5+M6) + MVP de cidadão** |
| Trunfo citável | núcleo legislativo de pé + **PCASP/TCE/SICONFI já provados** (vantagem sobre ERP-de-Câmara) | **web nativo sem emulador** (cláusula literal nos editais) + prestação TCE/SICONFI provada |

**Recomendação: priorizar a trilha LEGISLATIVA para a primeira venda/PoC.** Razões:
1. **Está a um sprint de ser vencível** — o núcleo (proposições, tramitação, sessão, quórum, votação nominal, painel, emendas, pareceres) já está de pé e é a maior cobertura de casos de uso do sistema.
2. **Prova de mercado barata e real** — uma PoC legislativa ganha referência, valida o produto em banca técnica e gera caixa/credibilidade sem esperar os dois marcos mais caros.
3. **O diferencial técnico aparece exatamente onde o legado sofre** — web nativo sem emulador + contabilidade/TCE provados (que um ERP-de-Câmara também precisa).
4. **A trilha municipal não fica parada** — M5 e M6 (que destravam o Executivo) avançam em paralelo; quando maduros, a mesma base atende editais municipais completos.

**O que NÃO fazer:** não tentar uma **PoC municipal de ERP completo agora** — reprovaria por percentual em folha/tributos/cidadão, queimando a referência. E não dispersar nos sub-módulos de paridade Betha (Cemitério, Meio Ambiente, portais de pais/alunos) antes de fechar uma venda.

---

## 4. Decisões / insumos em ABERTO que dependem do DONO

> Estes itens **não são engenharia** — são insumos que só você (ou o piloto) consegue obter, e
> vários **bloqueiam a "validação real"** (sair de simulação fiel para 0-erros no oficial). Liste-os
> como tarefas suas; sem eles, o código fica em `// TODO(validar-leiaute-oficial)` para sempre.

1. **MT 2026 + e-Validador/PAD oficial do TCE-RS** *(bloqueia validação real do M4 e da remessa de folha M5)*. Obter: PAD vigente (.MSI) + Manual Técnico Vol. I/II/V no portal SIAPC + acesso ao portal TCE-RS/SISCAD (via contador/gestor do piloto). Sem rodar o PAD oficial sobre a nossa remessa, **tudo é simulação** — o "0 erros" só é real no RVE/RDI do PAD (Java desktop, Windows, ato humano; **não há CLI/XSD/web service**). Considere abrir chamado ao TCE-RS perguntando se existe modo batch/headless ou homologação — se sim, muda o jogo. (`VALIDACAO-REAL-TCE.md`.)
2. **Certificado A1 real homologado por tenant** *(bloqueia eSocial em produção, assinatura de remessas, Protocolo)*. A criptografia (Cofre, XMLDSig, Key Vault) está pronta; falta o **certificado ICP-Brasil real do município** no Key Vault. Inclui definir o **EFR (Ente Federado Responsável)** para o eSocial S-1000.
3. **Piloto é RPPS ou RGPS?** *(afeta CADPREV, folha e eSocial)*. Decide se W5.7 (CADPREV/DRPPS) e os eventos S-1202/S-1207 entram ou **saem do escopo**, e se o motor de folha precisa de tabela previdenciária municipal (fail-closed) ou usa default RGPS. **Também precisa:** lei do RPPS (alíquota servidor/patronal + base), lei de consignações (% e base da margem), estatuto dos servidores. Sem isso, o motor de folha compila mas **recusa o cálculo previdenciário municipal**.
4. **Infra Azure + provisionamento de tenant** *(bloqueia go-live W10.4 e medições de carga)*. Provider real do banco (Postgres vs SQL Server) define o lock do Outbox relay e o grant INSERT-only da trilha (auditoria M9-E #2). Runbook de provisionamento/rotação de connection string + janela de migrations por banco dedicado (ADR-0005). Vários `[a confirmar]` de performance (plan-cache do filtro de UO, ponto de saturação do despacho do Outbox) **só se resolvem medindo em carga real** — DEV é SQLite.
5. **Credenciais de homologação** *(destravam a única validação real automatizável que temos)*:
   - **eSocial Produção Restrita** (certificado ICP do empregador; até ~1.000 vínculos) — permite validar XML contra XSD e transmitir de verdade no CI. Esse é o nível de validação real que o TCE-RS **não** oferece.
   - **gov.br login (Conecta/Login Único)** — **status de adesão do município à Rede Nacional de Governo Digital** (bloqueia o Portal do Cidadão autenticado no M8; obter AGORA, é ciclo de homologação→produção).
   - **PNCP** homologação (`treina.pncp.gov.br`) + Manual de Integração v2.5 — para o cliente real do M9-A.
   - **Banco arrecadador do piloto** (nº convênio + versão CNAB + credenciais PIX do PSP) — para a arrecadação do M6.
6. **Decisões de produto/escopo que você precisa cravar:**
   - **NFS-e: emissão ativa ou ingestão passiva?** Hoje é ingestão passiva via ADN (ADR-0003). Editais de MG e RS **presumem emissão/escrituração ativa** — **confirmar no edital-alvo concreto** antes de prometer aderência (risco alto, ver §5).
   - **Ponto eletrônico (Portaria 671)** se aplica ao estatutário do piloto? Se não, sai do MVP do M5.
   - **Obras**: agregado em Administracao ou Patrimonio? **Execução fiscal**: integra PJe/eproc-RS no escopo do M6?

---

## 5. Riscos estratégicos (não os técnicos triviais) e mitigação

| # | Risco estratégico | Por que importa | Mitigação |
|---|---|---|---|
| R1 | **Dispersão de escopo sem venda que financie o foco** | 11 módulos, ~48 workflows, paridade Betha — é fácil "avançar em tudo" e não fechar nada vendável. | **Ancorar o roadmap numa PoC legislativa próxima** (§3); congelar sub-módulos de paridade até a 1ª venda; usar o diferimento de 5% dos editais para o resíduo. |
| R2 | **"Pronto" que não é pronto sem o carimbo do oficial** | O núcleo fiscal é simulação **fiel** ao MT, mas "0 erros" só vem do PAD oficial. Demonstrar como "validado" sem o RVE/RDI é risco reputacional. | Disciplina de honestidade já existe nos docs; **obter MT 2026 + PAD (insumo do dono §4.1)** e congelar golden files; antes de qualquer demo, confirmar leiaute SIAPC/MSC do exercício. |
| R3 | **Risco de aderência NFS-e (passiva × ativa) numa PoC municipal** | Nosso modelo é ingestão ADN; editais presumem emissão/escrituração. Pode reprovar um módulo inteiro. | **Validar no edital concreto** antes de prometer; se exigir emissão ABRASF, reabrir escopo. Mitigado em parte por cláusulas que dispensam integração de terceiros na PoC (vira obrigação contratual). |
| R4 | **Janela regulatória móvel** (SISAB→SIAPS, PNAE 30%→45%, HÓRUS→e-SUS AF, EFD-Reinf, PNCP v2.5) | Número/sistema-alvo errado hardcoded = **falso "conforme"** → apontamento TCE / recolhimento irregular / bloqueio de repasse. | Princípio §16 já adotado: **tudo parametrizado por tenant+vigência** com fonte; integrações **BLOQUEADAS-SEM-LEIAUTE** atrás de ACL com versão de contrato; nunca hardcodar destino. |
| R5 | **Bloqueio de repasse/CAUC por lacuna fora do plano original** | EFD-Reinf, SADIPEM/CDP, Transferegov, encerramento de exercício só "aparecem" em jan/mar e travam a prestação anual ou o CAUC (>4.000 municípios já bloqueados por SADIPEM). | Já incorporados ao Plano-Mestre (W3.4, W5.6, W9.6, W9.9); **sequenciar antes da virada de exercício**, não no fim. |
| R6 | **Bloqueio externo de cronograma (adesão gov.br, certificado, banco)** | São processos de terceiros com ciclo próprio (homologação→produção); descobrir tarde atrasa o go-live. | Tratar os insumos do §4 como **tarefas do dono iniciadas AGORA, em paralelo ao dev** — especialmente adesão gov.br e certificado A1. |
| R7 | **Custo operacional do banco-por-tenant subestimado** | Decisão correta (ADR-0005), mas migrations/monitoração/fan-out por banco são gargalo prático real em escala. | Orçar pooling, janela de migration em lote com observabilidade por tenant, teto prático de tenants/instância; medir antes de prometer SLA. |

---

## 6. Recomendação do colega sênior — os próximos passos de maior alavancagem

### Faça AGORA (maior alavancagem, nesta ordem)
1. **Disparar os insumos do dono em paralelo (hoje, semana 1).** MT 2026 + PAD oficial, certificado A1 real, status de adesão gov.br, decisão RPPS-vs-RGPS. São processos lentos de terceiros — **não bloqueie o dev neles, mas inicie-os já**. Sem o item 1, "validação real" nunca acontece.
2. **Fechar a trilha legislativa até PoC-vencível (1 sprint).** Normas + Diário Oficial + cronômetro/oradores; validar Ata automática e membros de comissão. É a **prova de mercado mais barata e mais próxima** — e disciplina o roadmap (R1).
3. **Começar o M5 pela Fase 0 + W5.1 (motor de folha).** É o gargalo nº1 da trilha municipal e a fundação de eSocial/TCE/FUNDEB. Congelar MOS+XSD antes de uma linha de leiaute; **quebrar W5.2 (eSocial)** em sub-workflows desde o início.
4. **Confirmar as 3 correções P0 de robustez** (K6 cache, H1 Outbox dead-letter, H5 falha-alto) — a regressão recente do Outbox mostra que esse pano de fundo não é teórico.
5. **Antecipar o PNCP (M9-A) para logo após o núcleo** — é dívida de eficácia **já ativa** (contratos que empenham sem publicar são ineficazes), não uma feature do penúltimo marco.

### Faça DEPOIS (importante, mas não agora)
- M6 (Tributos) começando pelos parâmetros fiscais versionados → BCI → IPTU → arrecadação.
- W3.4 (encerramento de exercício) **antes da virada de 2026**.
- M7 pelo eixo fiscal compartilhado (mínimos constitucionais), integrações de envio por último.
- MVP do Portal do Cidadão (2ª via + protocolo) quando mirar editais municipais.
- Antecipar **uma avaliação isenta** (PoC + engenharia) ao concluir M5/M6, conforme a memória do projeto — não esperar o M10.

### NÃO faça agora (evitar dispersão)
- **Não** mirar PoC municipal de ERP completo antes de M5+M6 maduros (reprova por percentual; queima referência).
- **Não** construir cliente de transmissão para TCE-RS/SICONFI — **não existe API** (ADR-0009); o envio é ato humano. Investir aqui é trabalho jogado fora.
- **Não** implementar envio CIB→SINTER (piloto só obrigado jan/2027), nem SST/eSocial além do MVP, nem deliberação remota/terminais físicos no legislativo, nem os sub-módulos de paridade Betha (Cemitério, Meio Ambiente, portais de pais/alunos) — todos **antes da 1ª venda**.
- **Não** enxugar os excessos especulativos que a auditoria apontou (CNAB 400 + layouts por banco, XBRL-GL completo para a MSC, `IConsultaSiconfi` com 7 endpoints) — MVP = CNAB 240 + PIX + CSV + `/extrato_entregas`; o resto **sob demanda**.
- **Não** hardcodar nenhum número fiscal/prazo/sistema-alvo "para a demo" — é exatamente o que vira falso "conforme" e apontamento do TCE.

---

> **Uma frase para fechar:** o ativo mais caro e difícil — a espinha fiscal PCASP→MSC→TCE/SICONFI — **já é nosso e roda**; a tarefa agora não é provar que o sistema funciona, é **escolher uma venda legislativa próxima para financiar o foco** enquanto o motor pesado (folha, tributos) amadurece, e **obter do mundo real os carimbos** (MT 2026, certificado, homologações) que transformam "simulação fiel" em "0 erros no oficial".
>
> **Caminho deste arquivo:** `/Users/igorgewehr/Development/Tensorroot.Gov/docs/estudo/DIRECIONAMENTO.md`
