# Validação REAL contra o TCE-RS — plano para sair da SIMULAÇÃO

> Hoje a Tensorroot.Gov gera a remessa SIAPC/PAD (`.TXT` posicional + ZIP) e roda o pré-validador
> `EValidadorLocalSiapc`, que é uma **SIMULAÇÃO** das regras. Este documento é o PLANO para validar
> contra o **OFICIAL**, ser honesto sobre simulação × validação real, dizer o que dá p/ automatizar em
> CI, traçar o paralelo eSocial e listar EXATAMENTE o que obter e de quem.
> Regra: toda afirmação tem **[FONTE]** ou **[a confirmar]**. Data: 2026-06-22.
> Base: `docs/estudo/partes/evalidador-{ferramenta,mt-leiaute,automacao}.md`.

---

## PLANO RESUMIDO (≈15 linhas)

1. **Verdade nº 1:** o validador oficial SIAPC/PAD é o **PAD — Programa Autenticador de Dados**, app **Java desktop com GUI**, versão **vinculada ao exercício**, **sem CLI/headless documentado**, sem XSD nem web service público. **[FONTE: TCE-RS FAQ SIAPC; notícias do PAD]** — logo, **NÃO** há validação oficial 100% automatizável em CI.
2. **Verdade nº 2:** nossa `EValidadorLocalSiapc` é SIMULAÇÃO e seguirá sendo — a estratégia certa não é "chamar o oficial no CI", é **manter a simulação FIEL ao Manual Técnico versionado** e usar o PAD como **gate manual** de homologação humana.
3. **Loop de validação real (manual, por exercício):** (a) obter PAD vigente + MTs oficiais → (b) codificar o leiaute posicional EXATO (mapa de campos JSON) → (c) gerar remessa de teste → (d) rodar no **PAD oficial** (GUI) → ler o **RVE/RDI** → (e) corrigir gerador + simulação → **iterar até 0 inconsistências** no relatório oficial → (f) congelar/versionar o mapa e o "golden file" da remessa.
4. **CI automatizável:** validar a remessa contra o **mapa de campos versionado** (posições/tipos/somatórios) + **regression test contra golden files** validados no PAD. **Ato humano:** rodar o PAD oficial e ler o RVE a cada nova versão anual (diff de regras).
5. **Paralelo eSocial:** aqui **dá validação real automatizada** — há **XSD oficial (S-1.3, NT 06/2026)** + **Produção Restrita (web service SOAP, sem efeito legal)**; validamos XML contra XSD e transmitimos à homologação no CI. **[FONTE: eSocial NT S-1.3 06/2026; Produção Restrita]**
6. **Obter:** (i) **PAD do exercício** + **MT Vol I, II e V** no portal SIAPC; (ii) acesso ao **portal TCE-RS/SISCAD** + **certificado ICP-Brasil/gov.br** do município (via contador/gestor de Maximiliano de Almeida/RS) para rodar o PAD e, depois, transmitir de verdade.
7. **Honestidade:** sem o PAD oficial rodando sobre nossa remessa, **tudo é simulação**; o "0 erros" só é real quando vem do **RVE/RDI do PAD**, não do nosso validador.

---

## 1. O LOOP — de "simulação" a "0 erros no oficial"

Loop por **exercício** (a versão do PAD e o leiaute mudam todo ano):

```
[1] OBTER          PAD vigente (.MSI) + MT Vol I/II/V (PDF) do portal SIAPC
       │           → versionar PDFs em docs/estudo/fontes/ (host bloqueia fetch CI: usar UA navegador)
       ▼
[2] CODIFICAR      Extrair tabela posicional (Descrição|Tipo|Bytes|Colunas|Obs) de cada .TXT
       │           → gerar MAPA DE CAMPOS (JSON) que alimenta gerador E EValidadorLocalSiapc
       │           → trocar os TODO(MT-2026) por posição/tamanho/tipo/regra reais
       ▼
[3] GERAR          Produzir remessa de TESTE (.TXT + ZIP) com dados de Maximiliano de Almeida/RS
       ▼
[4] RODAR NO       Abrir o PAD oficial (GUI, Windows) → importar a remessa → "Iniciar"
    OFICIAL        → PAD valida o leiaute e emite o RVE/RDI com inconsistências  ← ATO HUMANO
       ▼
[5] ITERAR         Cada inconsistência do RVE/RDI → corrigir gerador + mapa + simulação
       │           → repetir [3]→[4] até o RVE/RDI acusar ZERO inconsistências
       ▼
[6] CONGELAR       Versionar mapa de campos + "golden file" da remessa aprovada
                   → vira fixture de regressão no CI (passo 2 da automação)
```

- O PAD valida leiaute e gera o **RVE — Relatório de Validação e Encaminhamento** / **RDI — Relatório de Dados e Informações**; inconsistências de leiaute "geram inconsistências no RDI". **[FONTE: TCE-RS FAQ SIAPC; MT Vol V]**
- O cadastro de responsáveis/período no **SISCAD** precisa estar correto, senão o PAD acusa avisos. **[FONTE: FAQ SIAPC]**
- **0 erros real = RVE/RDI limpo no PAD**, nunca o "verde" do nosso validador.

---

## 2. AUTOMATIZÁVEL em CI × ATO HUMANO (honesto)

| Etapa | Automatizável no CI? | Como / por quê |
|---|---|---|
| Validar posições/tipos/tamanhos da remessa | **SIM** | Contra **mapa de campos versionado** extraído do MT (não é o oficial, é fiel ao oficial). |
| Regras de somatório/coerência (balancete fecha, PCASP) | **SIM (parcial)** | Reimplementar as regras do MT na `EValidadorLocalSiapc`; cobrir com testes. **[a confirmar: lista completa de críticas do PAD]** |
| Regressão contra remessa "golden" aprovada no PAD | **SIM** | Fixture validada uma vez no oficial; CI falha se o gerador divergir do golden. |
| **Rodar o PAD oficial sobre a remessa** | **NÃO** | PAD é **Java desktop GUI, sem CLI/headless** documentado. **[FONTE: FAQ SIAPC]** → automação de GUI no Windows é frágil; **não recomendada como gate de CI**. |
| Obter XSD/schema oficial p/ validar `.TXT` | **NÃO** | **Não existe XSD nem web service público** do SIAPC/PAD. **[FONTE: estudo; dados.tce.rs.gov.br não valida remessa]** |
| Atualizar regras a cada versão anual do PAD | **NÃO (humano)** | Baixar PAD novo, ler MT novo, **diff de regras**, re-rodar o loop. |
| Assinar (ICP-Brasil/gov.br) e transmitir ao TCE | **NÃO (humano)** | Ato do gestor/contador com certificado; CI não assina nem envia. |

> **Conclusão:** para o SIAPC/PAD **não há gate oficial automatizável**. O CI valida contra uma
> **réplica fiel e versionada** do MT + golden files; o oficial entra como **homologação manual**.

---

## 3. PARALELO eSocial — aqui SIM há validação real automatizada

Diferente do TCE-RS, o eSocial tem **ambiente oficial de homologação ("Produção Restrita")** com web
service real, **sem efeito legal**, próprio para validar eventos de verdade ponta a ponta:

- **XSD oficiais versionados:** Esquemas XSD **v. S-1.3 (NT 06/2026)**, publicados com Leiautes, Anexo I (Tabelas) e Anexo II (Regras). **[FONTE: eSocial — NOTA TÉCNICA S-1.3 nº 06/2026; gov.br/esocial documentação técnica]**
- **Produção Restrita (web service SOAP):** envio/consulta de lote de eventos, sem valor jurídico, p/ teste funcional (cert. ICP-Brasil do empregador; até ~1.000 vínculos). **[FONTE: eSocial — Produção Restrita]**
- **No CI dá p/ automatizar de verdade:** (1) validar XML do evento contra o **XSD oficial**; (2) transmitir à **Produção Restrita** e **falhar o build** em retorno de erro. Esse é o nível de validação real que o SIAPC/PAD **não** oferece.

> **TCE folha:** os dados de folha que vão ao TCE-RS chegam via remessa **SIAPC/PAD (MT Vol V — Folha)** —
> e portanto caem no mesmo regime do PAD (gate manual, sem automação oficial). Já o **eSocial** (mesma folha,
> outro destino — MTE/RFB) tem homologação automatizável. Ou seja: a **mesma folha** tem validação real
> automatizável no **eSocial** e apenas **gate manual** no **TCE-RS**. **[FONTE: MT Vol V; eSocial Produção Restrita]**

---

## 4. O que EXATAMENTE precisamos OBTER — e de quem

**Ferramentas + manuais (públicos, baixar/versionar):**
- **PAD vigente do exercício (.MSI "Instalador Completo")** — portal SIAPC → "Download do PAD". A versão é **vinculada ao exercício** (versões de teste validam leiaute mas não habilitam envio). **[FONTE: TCE-RS notícias do PAD]** — [a confirmar: URL direta do `.MSI` 2026]
- **MT Vol V** (Informações Complementares — inclui Folha): `MT_Vol_V_Arq_DispTCE_4320.pdf` (espelho MPC: `MT-ASCE-0105-06-MT-Volume-V.pdf`, rev.06). **[FONTE]**
- **MT Vol II** (`MT_Vol_II_SiapcPAD_6404.pdf`) e **Vol I** (leiaute contábil/orçamentário do balancete — PCASP/MSC). **[FONTE]** — [a confirmar: revisão/data 2026]
- **FAQ SIAPC** (`perguntas_frequentes.pdf`) e **Resumo de Leiaute** (Vol V). **[FONTE]**
- ⚠️ Operacional: `tcers.tc.br` retorna **HTTP 403** a fetch automatizado — baixar com **User-Agent de navegador** ou manualmente e **versionar em `docs/estudo/fontes/`**. **[FONTE: tentativas WebFetch]**

**Acessos / credenciais (de quem) — só o município tem:**
- **Acesso ao portal TCE-RS + SISCAD** (cadastro de responsáveis/período de efetivo exercício) — do **contador/gestor de Maximiliano de Almeida/RS** (piloto). **[FONTE: FAQ SIAPC]**
- **Certificado ICP-Brasil A1/A3 ou conta gov.br prata/ouro** do município — para o PAD assinar o RVE e transmitir. **No nosso fluxo, fica no Azure Key Vault por tenant.** **[FONTE: CLAUDE.md §6; FAQ SIAPC]**
- **Máquina Windows** (PAD é desktop) para rodar a etapa [4] do loop. **[FONTE: distribuição .MSI]**

**Chamado a abrir ao TCE-RS (pode mudar todo o quadro) — [a confirmar]:**
1. Existe **modo batch/CLI/headless** do PAD (flag, JAR invocável)?
2. Existe **XSD/schema** dos `.TXT` ou **validador programático/web service**?
3. Existe **ambiente de homologação** equivalente à Produção Restrita do eSocial?
> Se qualquer resposta for "sim", o SIAPC sobe ao nível de automação do eSocial.

---

## 5. Mudança 2026 a monitorar

Para o **encerramento de 2026**, o TCE-RS passará a **confeccionar o Balanço Financeiro e a Demonstração
dos Fluxos de Caixa (DFC)** (26ª edição do evento SIAPC). O leiaute detalhado desses demonstrativos **ainda
não foi localizado publicado** — verificar se exige **novos `.TXT`** ou apenas qualifica dados existentes,
e re-rodar o loop da §1 quando publicado. **[FONTE: TCE-RS — 26ª edição evento SIAPC]** — [campos: a confirmar]

---

## Simulação × Validação real — a régua honesta

- **Simulação (o que temos):** `EValidadorLocalSiapc` + mapa de campos do MT + golden files. Roda no CI, é rápido, mas **é nossa interpretação** do manual — pode divergir.
- **Validação real (a verdade):** **somente** o **RVE/RDI emitido pelo PAD oficial** sobre a nossa remessa. É ato **manual**, no Windows, com certificado.
- **Critério de pronto:** uma remessa só é "validada de verdade" quando o **PAD oficial acusa 0 inconsistências** — então essa remessa vira **golden file** e protege o CI até a próxima versão anual do PAD.

---

## Fontes

- [Portal TCE-RS — Sistemas de Controle Externo / SIAPC](https://tcers.tc.br/sistemas-de-controle-externo/)
- [Portal TCE-RS (legado) — SIAPC](http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc)
- [TCE-RS — versão de teste do PAD para 2025](https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/)
- [TCE-RS — versão do PAD para envio de janeiro](https://tcers.tc.br/noticia/versao-do-pad-para-envio-das-informacoes-de-janeiro-ja-esta-disponivel/)
- [TCE-RS — 26ª edição do evento SIAPC (mudanças 2026)](https://tcers.tc.br/noticia/tce-rs-realiza-26a-edicao-do-evento-do-siapc-com-orientacoes-tecnicas-para-gestores-municipais/)
- [TCE-RS — FAQ SIAPC (PDF)](http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf)
- [MT Volume V (rev.06) — TCE-RS (PDF)](http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf)
- [MT Volume V — espelho MPC-RS (PDF)](https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf)
- [MT Volume II — TCE-RS (PDF)](https://tcers.tc.br/repo/SIAPC/MANUAL/MT_Vol_II_SiapcPAD_6404.pdf)
- [Resumo de Leiaute (Vol V) — TCE-RS (PDF)](http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf)
- [e-Validador LicitaCon (JNLP, NÃO é a remessa SIAPC)](https://portal.tce.rs.gov.br/portal_tcers/e-validador/licitacon.jnlp)
- [eSocial — Home / documentação técnica](https://www.gov.br/esocial/pt-br)
- [eSocial — NOTA TÉCNICA S-1.3 nº 06/2026 (XSD/leiautes/regras)](https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/nota-tecnica-s-1-3-06-2026-rev.pdf)
- [eSocial — Ambiente de Produção Restrita (homologação)](https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita)
