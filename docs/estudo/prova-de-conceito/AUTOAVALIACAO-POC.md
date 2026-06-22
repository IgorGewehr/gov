# Autoavaliação de Prova de Conceito — Tensorroot.Gov

> **Pergunta:** com o que NÓS TEMOS hoje, passaríamos numa PoC/Teste de Conformidade **MUNICIPAL** (Executivo)?
> E numa **LEGISLATIVA** (Câmara)?
> **Método:** cruzar os requisitos recorrentes ("obrigatórios de facto") extraídos dos editais REAIS
> (`pesquisa-*` + `verificacao-*`, verificados campo a campo) contra o estado de engenharia **pós-M4**
> (board `docs/progresso/progresso.json`: M0–M4 `done`, M5–M9 `pending`; ADRs 0007–0013; código lido).
> **Regra de honestidade:** o `ESTADO-ATUAL.md` é **foto pré-M0** (diz "PCASP = 0 linhas"); o código atual
> **contradiz** isso — Contabilidade PCASP, A1/Key Vault (módulo Cofre), MSC, DCASP e remessa SIAPC/SICONFI
> **existem e estão entregues** (verificado: 58 arquivos de Contabilidade em Finanças, `LeiauteSiapc`/
> `MotorPreValidacaoSiapc`/`EmissorRegistroSiapc`/`ReconciliarSiconfi` em Transparência, `AssinadorXmlDsig`/
> `ProvedorKekKeyVault` em Cofre). A avaliação abaixo usa o **estado pós-M4**, não a foto pré-M0.
> Data: 2026-06-22.

---

## Regra do jogo (das fontes verificadas)

Os 3 editais do RS/SC/MG lidos campo a campo (Mata/RS, Riqueza/SC, Carbonita/MG) convergem:
**(a)** 100% dos "requisitos obrigatórios GERAIS" sob pena de desclassificação imediata (eliminatório);
**(b)** **90–95% por módulo** (RS=95%); **(c)** resíduo de 5% diferível (30–120 dias pós-contrato);
**(d)** **web nativo sem emulador** (cláusula literal e repetida); **(e)** integrações de terceiros podem ser
**dispensadas na PoC** (E2 §14.6.6) — viram obrigação contratual. Para Câmara: núcleo é sessão+votação+
proposições+transparência; ERP-de-Câmara (Salto/SP, Belacruz/CE) ainda exige contabilidade+folha+prestação.

---

## TRILHA MUNICIPAL (Executivo) — requisito → temos → marco

| Requisito recorrente (fonte) | Temos / Parcial / Falta | Marco que cobre |
|---|---|---|
| **Web nativo, n-camadas, sem emulador** (E2 §1.b, Mata §7.9) | ✅ **Temos** (SPA React + API .NET) — diferencial literal | base (entregue) |
| Sem limite de usuários simultâneos; multi-navegador | ✅ Temos | base |
| **Contábil PCASP/MCASP + partida dobrada** (E1/E2/Mata — núcleo) | ✅ **Temos** (plano de contas, `MotorContabil`, ΣD=ΣC, balancete) | M2 (entregue) |
| Ciclo Lei 4.320 Empenho→Liquidação→Pagamento + Restos a Pagar | ✅ Temos (API exposta no M2) | M2 |
| **PPA/LDO/LOA** + vedar despesa sem dotação | ✅ Temos | M3 |
| **7 DCASP + RREO/RGF/DCA + MSC** | ✅ Temos (derivados do balancete) | M3/M4 |
| **Bloqueio mensal escalonado** (contabilidade governa) (E1 item 59) | 🟡 **Parcial/[a confirmar]** — fechamento existe; travar abertura dos demais módulos não está provado | M3.4/M9 |
| **Remessa TCE-RS SIAPC/PAD largura-fixa + pré-validação RDI** | ✅ **Temos** (`LeiauteSiapc`, `MotorPreValidacaoSiapc`, empacota+hash) — `[OFICIAL]` leiaute do exercício a confirmar | M4 |
| **SICONFI (RREO/RGF/DCA/MSC) gerar+reconciliar** | ✅ Temos (geração + `ReconciliarSiconfi` via API de consulta) | M4 |
| **Compras/Licitações Lei 14.133 + Contratos/Aditivos** | ✅ Temos (módulo maduro, saga orçamento↔contrato) | base/Administração |
| **PNCP** (art. 176) | 🟡 Parcial (grava nº + evento; **sem cliente HTTP**) — dispensável na PoC (E2 §14.6.6) | M9 |
| **Patrimônio + Almoxarifado + Frota + depreciação MCASP** | ✅ Temos (módulo mais completo) | base |
| **Cadastro único de credores/fornecedores** | ✅ Temos | base/M1 |
| **Folha de pagamento** (estrutura + abate-teto) | 🟡 **Parcial — falta o MOTOR** (INSS/IRRF/RPPS) | **M5 (pendente)** |
| **eSocial** (eventos + transmissão) | 🔴 **Falta** (só valida S-1010 local) | **M5** |
| EFD-Reinf / DCTFWeb | 🔴 Falta | **M5** |
| Ponto eletrônico (Port. 671) | 🔴 Falta — incerto como item de PoC | **M5** |
| **Tributos: IPTU/ISS/ITBI/Taxas + carnê/DAM** | 🔴 **Falta o motor** (sem cadastro imobiliário/PGV); só Dívida Ativa/CDA madura | **M6 (pendente)** |
| **NFS-e** (ingestão ADN passiva) | 🟡 **Risco de aderência** — editais MG/RS presumem **escrituração/emissão ativa**; nós ingerimos | M6 / `[a confirmar]` edital-alvo |
| **Dívida Ativa, CDA, cobrança** | ✅ Temos | base |
| **Processo digital/protocolo + assinatura** | ✅ Temos (NUP/CONARQ); carimbo de tempo é relógio local | base / M9 |
| **Portal da Transparência LAI + e-SIC** | 🟡 Parcial (andaime; tempo real LC 131 e portal público no M8) | M8 |
| **Portal do Cidadão / autoatendimento / app** | 🔴 **Falta** (camada cidadão inexistente) — recorrente em Mata/RS | **M8** |
| LGPD operacional (RoPA, consentimento, DPO, termos) | 🟡 Parcial (sensibilidade+trilha de leitura no M1; **RoPA/consentimento/área do titular não provados**) | M1 / M8-M9 |
| RBAC + auditoria por usuário (I/A/E/C); HTTPS/SSL | ✅ Temos (auditoria imutável a endurecer; A1 entregue) | base/M1 |
| Tesouraria / conciliação bancária (CNAB) | ✅ Temos | M4 |

---

## TRILHA LEGISLATIVA (Câmara) — requisito → temos → marco

| Requisito recorrente (fonte) | Temos / Parcial / Falta | Marco |
|---|---|---|
| **Proposições + tramitação + protocolo** (E1/E3/SAPL — núcleo) | ✅ **Temos** (Proposicao, Tramitacao, Autoria, Ementa, Substitutivo) | base |
| **Emendas / Pareceres** | ✅ Temos (`ApresentarEmenda`, `RegistrarParecer`) | base |
| **Comissões + membros efetivos/suplentes + reuniões/atas** | 🟡 **Parcial** (14 arquivos com "Comissao"; **validar cadastro membros + pauta/ata de reunião** exigidos por E2/E3) | base / `[validar]` |
| **Sessão: pauta + Ordem do Dia + presença/quórum** | ✅ Temos (Sessao, Presenca, ItemOrdemDoDia, quórum em 9 arquivos) | base |
| **Geração automática de Ata Sintética** (E2 literal) | 🟡 Parcial (26 arquivos com "Ata"; **confirmar geração automática ao fim da sessão**) | base / `[validar]` |
| **Votação nominal — abrir/fechar/cancelar + resultado em painel** | ✅ Temos (Votacao maioria/modalidade, Voto, Painel em 5 arquivos) | base |
| Parlamentar impedido de votar | 🟡 `[validar]` | base |
| **Cronômetro de tribuna / inscrição de oradores** | 🔴 **Falta** (0 arquivos "Cronometro"/"Orador") — recorrente E2/E3 | gap |
| **Deliberação remota** (voto/tribuna online) | 🔴 Falta (0 arquivos "Remot") — **rebaixado a diferencial**, não mínimo universal | gap (opcional) |
| **Base de Normas Jurídicas consultável** | 🔴 **Falta** (só 2 arquivos "Norma") — recorrente E1/E3/SAPL | gap |
| **Diário Oficial eletrônico** | 🔴 **Falta** (0 arquivos) — recorrente E1 | gap |
| Integração painel/terminais físicos (hot-swap) | 🟡 Condicional ao edital (Vitória/Curitiba sim; Matão/Salto por vídeo) | `[depende do edital]` |
| **Contabilidade própria + prestação TCE-RS + folha** (ERP-Câmara: Salto/SP, Belacruz/CE) | ✅/🟡 **PCASP/MSC/SICONFI/SIAPC temos** (M2-M4); **folha sem motor** (M5) | M2-M4 ✅ / M5 🔴 |
| Transparência + transmissão de sessões | 🟡 Parcial (transparência base; transmissão de vídeo não é nosso escopo) | M8 |
| Assinatura/certificado digital nas peças | ✅ Temos (Cofre A1/XMLDSig + Protocolo) | M2/base |

---

## Veredito de prontidão

**TRILHA MUNICIPAL: ~60–65% — REPROVA hoje numa PoC ERP-completo.**
O núcleo fiscal mais escrutinado (PCASP, ciclo da despesa, DCASP, MSC, **prestação TCE-RS/SICONFI**) está
**entregue e é nosso ponto forte** — a meta nº1 do dono. Mas a PoC municipal típica é **eliminatória** e cobra
**Folha+eSocial** e **motor de Tributos (IPTU/ISS)** como módulos próprios: ambos **sem motor hoje** (M5/M6
pendentes). Como o corte é **95% por módulo** e esses módulos não rodam fim-a-fim, reprovaríamos por
**não atingir o percentual nesses módulos**, não por falha no núcleo contábil. Some-se o **Portal do Cidadão
ausente** (recorrente em Mata/RS) e o **risco NFS-e ativa vs. ingestão passiva**.

**TRILHA LEGISLATIVA: ~70–75% — PROVÁVEL APROVAÇÃO numa PoC de "software de processo legislativo"; REPROVA num ERP-de-Câmara completo.**
O núcleo legislativo (proposições, tramitação, sessão, quórum, votação nominal, painel, emendas, pareceres)
**está de pé** e é a maior cobertura de casos de uso do sistema. Para editais tipo Matão/Salto (processo
legislativo + transparência) passaríamos, **fechando 3 gaps de baixo custo**: **Normas Jurídicas**, **Diário
Oficial** e **cronômetro/oradores** (e validar **Ata Sintética automática** + cadastro de **membros de
comissão**). Para ERP-de-Câmara (Belacruz/CE), a contabilidade+TCE já é vantagem nossa, mas a **folha sem
motor (M5)** reprovaria o módulo de pessoal.

**Gaps que REPROVARIAM HOJE (eliminatórios):**
- Municipal: **Folha sem motor de cálculo** (M5); **eSocial ausente** (M5); **motor de Tributos/IPTU ausente** (M6); **Portal do Cidadão ausente** (M8).
- Legislativo (só se for ERP-completo): **folha sem motor** (M5).
- Ambos — risco de banca: **NFS-e** (ingestão passiva pode não satisfazer requisito de emissão/escrituração); **PNCP sem cliente** (mitigável por E2 §14.6.6); **leiaute SIAPC/PAD `[OFICIAL]` do exercício** a confirmar antes de demonstrar.

---

## Caminho (o que tira de "reprova" para "aprova")

1. **Escolher a trilha do primeiro alvo.** Maior prontidão = **Legislativo "processo legislativo"** (Matão/Salto). Vença essa PoC primeiro.
2. **Sprint de gaps legislativos (baixo custo, alto retorno):** Normas Jurídicas + Diário Oficial + cronômetro/oradores; validar Ata Sintética automática e cadastro de membros de comissão. → fecha a trilha legislativa.
3. **Para a trilha municipal, executar M5 (Folha+motor+eSocial) e M6 (Tributos: cadastro imobiliário + IPTU/ISS)** — são os dois módulos que hoje reprovam por percentual. São pré-condição para qualquer PoC de ERP municipal completo.
4. **M8 mínimo do Portal do Cidadão** (2ª via de guia + abrir/consultar protocolo) — recorrente e barato de demonstrar.
5. **Resolver o risco NFS-e:** confirmar no **edital-alvo concreto** se exige emissão/escrituração ativa; se sim, ajustar escopo (hoje é ingestão ADN passiva por decisão de arquitetura).
6. **Antes de QUALQUER demo:** confirmar **leiaute SIAPC/PAD e Regras MSC do exercício vigente** (`[OFICIAL]`), e usar o **diferimento de 5%** dos editais para o resíduo (PNCP, EFD-Reinf, ponto).
7. **Trunfo a explorar na ata da banca:** **web nativo sem emulador** (cláusula literal E2/Mata) + **prestação TCE-RS/SICONFI provada** — onde a maioria dos concorrentes legados sofre, nós lideramos.

> Honestidade final: **não passaríamos hoje numa PoC municipal de ERP-completo** (Folha/Tributos/Cidadão
> reprovam por percentual), apesar do **núcleo contábil/TCE estar entregue**. **Passaríamos numa PoC
> legislativa de processo legislativo** com um sprint pequeno de 3–4 gaps. A foto `ESTADO-ATUAL.md`
> subestima o sistema (é pré-M0); a realidade pós-M4 é melhor no núcleo fiscal — e ainda assim insuficiente
> nos módulos que M5/M6/M8 entregam.
