# Onda 2 — Passar no TCE-RS e nas obrigações (mapa de bloqueio honesto)

> Status: **majoritariamente bloqueada** — e não só por credenciais. Este documento mede, do código,
> exatamente o que falta e por quê, para a equipe atacar com a informação certa. Medido em 2026-06-29.
>
> **Princípio:** estes artefatos alimentam **remessas oficiais ao Tribunal de Contas / Receita**.
> Dado errado é pior que arquivo vazio. Por isso **não fabricamos** agregados de domínio nem contratos
> de integração que não temos — preferimos marcar honestamente o bloqueio (igual aos `TODO(prod:...)` /
> `[validar-oficial]` já espalhados no código).

## Por que a Onda 2 não fechou autonomamente

| Item | Bloqueio dominante | Credencial? |
|---|---|---|
| Folha-TCE 1099 | Confirmação no **e-Validador oficial** | Sim |
| SIAPC/PAD grade 2026 | **Leiaute oficial MT Vol. V 2026** (BP/BF/DFC) não está no repo | Não (mas é doc oficial que não temos) |
| LicitaCon 1.4 | **Agregados de domínio inexistentes** (Comissão, Itens, Dotação vinculada, …) | Transmissão sim |
| eSocial transporte | **Contrato SOAP/WSDL + XSD oficiais** não estão no repo | Cert mTLS + endpoint sim |
| NFS-e ADN | Endpoint + contrato XML nacional + certificado | Sim |
| PCASP completo 2026 | **Elenco oficial STN** (Portaria 3.133/2025) não está no repo | Não (mas é doc oficial extenso) |

## LicitaCon 1.4 — `GeradorRemessaLicitaCon.cs`

`src/Modules/Administracao/.../Application/LicitaCon/GeradorRemessaLicitaCon.cs`

Os **layouts (colunas) dos 14 arquivos já estão definidos no código**. O bloqueio é **falta de dado
no domínio**, não falta de layout.

| Arquivo | Preenchível agora? | Motivo |
|---|---|---|
| Licitacao | 🟡 parcial (≈37/65 campos) | resto: datas, regime, comissão, documentos — não modelados |
| Lote | ✅ | número/descrição/valor estimado existem nos lotes |
| Licitante | 🟡 parcial | situação de habilitação existe; **documento sai como UUID** |
| Proposta | 🟡 parcial | valor/situação existem; **documento sai como UUID** |
| Pessoas, MembroConsorcio | ❌ | agregado Pessoa/Consórcio não modelado |
| Comissao, MembroComissao | ❌ | **Comissão de licitação não existe no domínio** |
| DotacaoLicitacao | ❌ | sem vínculo Dotação↔Licitação no agregado |
| EventoLicitacao | ❌ | só há DomainEvents efêmeros; sem entidade de evento persistida |
| Item, ItemProposta | ❌ | propostas têm valor único; itens não decompostos dos lotes |
| LoteProposta | ❌ | sem entidade de agrupamento lote×proposta |
| DocumentoLicitacao | ❌ | sem entidade de documento persistido |

**Bloqueador transversal:** o agregado `Licitacao` guarda `FornecedorVencedorId`/`FornecedorId` como
**GUID interno** — não há agregado `Fornecedor` para resolver o **CNPJ/CPF real** (linhas 86, 145, 174
do gerador, `// TODO(M10) mapear ao CNPJ/CPF real`). O `LicitacaoHomologadaIntegrationEvent` também só
carrega IDs internos.

**Próximo passo (precisa de decisão de domínio, não de credencial):** modelar no contexto
`Administracao` os agregados/entidades **Comissão + MembroComissão**, **Item** (decomposição de lote),
**ItemProposta**, **vínculo Dotação→Licitação** e um agregado/leitura **Fornecedor** com documento real.
Isso deve ser dirigido pelo **fluxo real da licitação** (a comissão, os itens e as propostas precisam ser
*capturados na operação* para então serem remetidos) — fabricá-los só para preencher o CSV produziria
remessa oficial com dados inventados.

## eSocial — pipeline real, transporte bloqueado

Pipeline interno **é real**: `EscritorXmlEvento` (gera XML) → `AssinarEventoESocial` (XML-DSig via A1 do
Cofre) → `EmpacotadorLoteESocial` (envelope do lote) → `ESocialGatewaySimulado` (transporte fake).

O que **falta para um `ESocialGatewaySoap` real**, e por que não foi feito agora:
- **Contrato SOAP/WSDL e XSD (S-1.3) oficiais NÃO estão no repo** (`find -iname '*.xsd'` = vazio). Montar
  o envelope SOAP e o parsing da resposta **sem o WSDL oficial** seria *chutar o contrato de uma
  integração fiscal* — exatamente o que o repo proíbe. O risco não é "não compila", é "transmite
  estrutura errada e parece pronto".
- **Cred-gated de fato:** certificado **mTLS** (A1 real no Cofre), **endpoint** de Produção Restrita,
  códigos de tabela oficiais (MOS).

**O que dá para adiantar com segurança quando o WSDL/XSD estiverem disponíveis** (já mapeado): campo
`EndpointSoap` em `ParametrosESocial`, um `ValidadorXmlESocial` (valida o XML contra o XSD antes de
assinar) e o esqueleto `ESocialGatewaySoap` (SOAP 1.2 + Polly + A1 do Cofre). Tudo isso fica de fora
**até termos o XSD/WSDL oficial** para validar contra.

## SIAPC/PAD, Folha-1099, SICONFI/MSC, NFS-e ADN, PCASP

- **SIAPC/PAD:** emissor posicional já robusto; falta a **grade oficial MT Vol. V 2026** (JSON
  versionado) — documento do TCE-RS que não está no repo. `LeiauteSiapcSeed.cs` (`TODO(validar-leiaute-MT-2026)`).
- **Folha-TCE 1099:** o leiaute **mais pronto** (grade dos 3 arquivos + contrato RH→Transparência);
  só falta **confirmar vigência no e-Validador** (cred-gated).
- **PCASP completo 2026:** catálogo mínimo (~62/70 contas). Importar o **elenco oficial STN** (Portaria
  3.133/2025) — tabela oficial extensa que não está no repo.
- **NFS-e ADN / SICONFI:** endpoint + contrato + certificado (cred-gated).

## Conclusão

A Onda 2 avança quando chegarem **(a)** os **documentos oficiais de leiaute** (grade SIAPC MT 2026, XSD
eSocial S-1.3, elenco PCASP STN, WSDL eSocial) e **(b)** as **credenciais/certificados** do ente — e,
para o LicitaCon, **(c)** decisões de **modelagem de domínio** capturando comissão/itens/propostas e o
fornecedor real no fluxo da licitação. Nenhuma dessas três coisas é fabricável com responsabilidade
para uma remessa ao Tribunal de Contas.
