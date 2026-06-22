# e-Validador / Validador oficial TCE-RS — ferramenta de validação da remessa

> Estudo Tensorroot.Gov — substituir o pré-validador LOCAL `EValidadorLocalSiapc` (SIMULAÇÃO) pelo fluxo OFICIAL do TCE-RS.
> Convenção: toda afirmação traz **[FONTE]** ou **[a confirmar]**.

## Resposta direta (4 linhas)

1. Para **SIAPC/PAD** (contas municipais, Lei 4.320/64), a ferramenta oficial de validação NÃO se chama "e-Validador": é o **PAD — Programa Autenticador de Dados**, baixado no portal do TCE-RS na seção SIAPC. **[FONTE: tcers.tc.br/sistemas-de-controle-externo (seção SIAPC) e tcers.tc.br — notícias da versão de teste do PAD 2025]**
2. O "**e-Validador**" é a ferramenta de **outro** sistema — o **LICITACON** (licitações e contratos): aplicativo **Java Web Start** distribuído via arquivo `.jnlp` (`portal.tce.rs.gov.br/portal_tcers/e-validador/licitacon.jnlp`), que gera o "Relatório de Críticas de Validação". Não serve para a remessa SIAPC/PAD. **[FONTE: compras.rs.gov.br/noticias/1620; manual e-Validador LicitaCon — tcers.tc.br/repo/cex/licitacon/cpt/eValidador-licitacon-manual-op.pdf]**
3. O PAD valida o leiaute da remessa (.TXT posicional) e gera o **RDI — Relatório de Dados e Informações** (atenção: o brief chamou de "Relatório de Identificação"; a nomenclatura oficial é **Relatório de Dados e Informações**). Inconsistências de leiaute "geram inconsistências no RDI". **[FONTE: MT-ASCE-0105-06 MT Volume V; perguntas_frequentes.pdf — tce.rs.gov.br/sistemas_controle/SIAPC]** — [a confirmar: existência de versão "Relatório de Identificação" como nome alternativo]
4. Para o nosso CI: o PAD é programa **desktop Windows** instalável (distribuído como **.MSI "Instalador Completo"**), com **GUI**, integrado ao SISCAD e assinatura eletrônica (ICP-Brasil / gov.br) para envio; **modo CLI/batch oficial NÃO foi confirmado** nas fontes. **[FONTE: tcers.tc.br/sistemas-de-controle-externo; perguntas_frequentes.pdf]** — [a confirmar: flag de linha de comando/headless]

## GUI × CLI (crucial para automação em CI)

| Aspecto | PAD (SIAPC/PAD — nossa remessa) | e-Validador (LICITACON) |
|---|---|---|
| Tipo | Desktop instalado no Windows (.MSI "Instalador Completo") **[FONTE: tcers.tc.br; busca portal SIAPC]** | Java Web Start (`.jnlp`) **[FONTE: licitacon.jnlp]** |
| Interface | **GUI** (operação manual: importa TXT → valida → gera RDI → assina → envia) **[FONTE: FAQ SIAPC]** | **GUI** (tela "Validar" → botão Imprimir → Relatório de Críticas) **[FONTE: manual e-Validador]** |
| Modo CLI / batch / headless | **NÃO confirmado** nas fontes oficiais. Sem flag de linha de comando documentada. → bloqueio para CI sem interação. **[a confirmar]** | NÃO confirmado **[a confirmar]** |
| Runtime | Windows; integração SISCAD + assinatura eletrônica. Java **[a confirmar]** (LicitaCon usa Java/JWS; PAD provavelmente, mas não confirmado em fonte) | Java Runtime / Java Web Start **[FONTE: .jnlp]** |
| Saída | **RDI — Relatório de Dados e Informações** (relatório de validação/inconsistências) **[FONTE: MT Vol V; FAQ]** | Relatório de Críticas de Validação **[FONTE: manual e-Validador]** |

**Implicação para Tensorroot.Gov:** não há, nas fontes oficiais consultadas, um modo CLI/batch documentado do PAD para rodar em CI de forma headless. Estratégias prováveis (a validar): (a) automação de GUI no Windows (frágil); (b) reimplementar fielmente as regras de leiaute do **Manual Técnico SIAPC Vol. V** como validador próprio de alta fidelidade (substitui a SIMULAÇÃO atual) e manter o PAD oficial como gate manual de homologação; (c) checar se o `.MSI`/JAR do PAD expõe entrypoint Java invocável. **[a confirmar todas]**

## Onde baixar (URLs)

- **PAD (oficial, SIAPC/PAD):** Portal TCE-RS → Sistemas de Controle Externo → **SIAPC** → "Download do PAD" → versão (ex.: 25.0.0.0 / 26.x). URL da seção: `https://tcers.tc.br/sistemas-de-controle-externo/` (filtro/section SIAPC). **[FONTE: tcers.tc.br/sistemas-de-controle-externo; notícia "TCE disponibiliza versão de teste do PAD para 2025"]** — [a confirmar: URL direta do .MSI da versão vigente do exercício]
- **e-Validador (LICITACON, NÃO é a nossa remessa):** `https://portal.tce.rs.gov.br/portal_tcers/e-validador/licitacon.jnlp` **[FONTE: resultado de busca + compras.rs.gov.br/noticias/1620]**
- **Manual Técnico SIAPC Vol. V (leiaute / arquivos à disposição):** `http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf` e espelho MPC `https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf` **[FONTE: ambos abertos na pesquisa]**
- **FAQ SIAPC/PAD:** `http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf` **[FONTE]**

## Como o município usa hoje (fluxo oficial)

1. Sistema contábil gera a remessa SIAPC/PAD (.TXT posicional + complementares: CADASTRO.TXT, LEIAUTE.TXT, LEIAME.TXT). **[FONTE: MT Vol V / FAQ]**
2. Operador importa a remessa no **PAD** (desktop), que **valida o leiaute** e gera o **RDI** com eventuais inconsistências. **[FONTE: FAQ SIAPC]**
3. Cadastro de responsáveis/período de efetivo exercício deve estar correto no **SISCAD**, senão o PAD acusa avisos. **[FONTE: FAQ SIAPC, perguntas 8 e seguintes]**
4. Assinatura eletrônica (certificado **ICP-Brasil** ou conta **gov.br** nível prata/ouro) e envio ao TCE pelo Processo Eletrônico. **[FONTE: FAQ SIAPC; portal TCE-RS]**

## Requisitos (Java / SO)

- **SO:** Windows (distribuição como **.MSI**). **[FONTE: busca portal SIAPC]** — [a confirmar: suporte Linux/macOS — improvável]
- **Java/JRE:** o **e-Validador LicitaCon** é Java Web Start (requer JRE). Para o **PAD**, o uso de Java é **[a confirmar]** (não há fonte direta lida confirmando versão de JRE do PAD). **[FONTE parcial: licitacon.jnlp para o e-Validador]**
- **Certificado digital ICP-Brasil A1/A3 ou gov.br prata/ouro** para assinatura/envio. **[FONTE: FAQ/portal]**
- Versão do PAD é **vinculada ao exercício** (ex.: 25.0.0.0 para 2025; versões de teste não habilitam envio, só validam leiaute). **[FONTE: notícias TCE-RS]**

## Lacunas a confirmar (próximos passos do estudo)

- [a confirmar] Existe modo **CLI/batch/headless** do PAD (flags, JAR invocável)? — decisivo para CI.
- [a confirmar] **URL direta** do instalador .MSI da versão vigente do exercício.
- [a confirmar] PAD depende de **Java** e qual versão de JRE.
- [a confirmar] Nome do relatório: oficial **RDI = Relatório de Dados e Informações**; brief usou "Relatório de Identificação" — verificar se há ambiguidade na documentação por exercício.
- [a confirmar] Formato/parse do **RDI** (texto/PDF/estrutura) para extrair PASS/erros automaticamente.

## Fontes

- [Portal TCE-RS — Sistemas de Controle Externo (seção SIAPC)](https://tcers.tc.br/sistemas-de-controle-externo/)
- [TCE disponibiliza versão de teste do PAD para 2025](https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/)
- [Versão do PAD para envio das informações de janeiro](https://tcers.tc.br/noticia/versao-do-pad-para-envio-das-informacoes-de-janeiro-ja-esta-disponivel/)
- [FAQ SIAPC/PAD — perguntas frequentes (PDF)](http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf)
- [Manual Técnico SIAPC Vol. V — Arquivos à disposição (PDF)](http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf)
- [Manual Técnico SIAPC Vol. V — espelho MPC (PDF)](https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf)
- [e-Validador LicitaCon — JNLP (Java Web Start)](https://portal.tce.rs.gov.br/portal_tcers/e-validador/licitacon.jnlp)
- [LICITACON e-Validador — notícia Compras RS](https://www.compras.rs.gov.br/noticias/1620)
- [Manual e-Validador LicitaCon (PDF)](https://tcers.tc.br/repo/cex/licitacon/cpt/eValidador-licitacon-manual-op.pdf)
- [Manual SISCAD (PDF)](https://tcers.tc.br/repo/cex/siscad/manual_do_SISCAD-dez.pdf)
