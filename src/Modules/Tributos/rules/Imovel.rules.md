---
modulo: Tributos
agregado: Imovel
contexto: Tributos (Cadastro Imobiliário Municipal — BCI)
poder: Executivo
schema: tributos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais:
  - "CTN art. 32 a 34 (IPTU — fato gerador, contribuinte, base de cálculo)"
  - "Decreto 11.208/2022 (SINTER e CIB — Cadastro Imobiliário Brasileiro)"
  - "IN RFB 2.275/2025 (regras de gestão de informações de imóveis; valor de referência — art. 256 LC 214/2025)"
---

# Imovel (Cadastro Imobiliário / BCI) — Regras Normativas

> **Fonte da verdade.** Este `*.rules.md` é normativo e versionado. O agregado `Imovel` é a base
> do cadastro imobiliário e a origem do lançamento anual de IPTU.

## 1. Linguagem Ubíqua

| Termo | Definição |
|---|---|
| **Imovel** | Imóvel urbano do cadastro municipal (BCI). Raiz de agregado rica. |
| **IdentificacaoImovel** | VO: `InscricaoMunicipal` (chave histórica, obrigatória), `CibCodigo` (opcional, formato AAAAAAA-D), `MatriculaRgi` (opcional). |
| **EnderecoImovel** | VO: logradouro, número, bairro, CEP, setor/quadra/lote, face de quadra, **zona fiscal** (chave da PGV). |
| **CaracteristicasImovel** | VO: área de terreno, área construída, tipo de uso, padrão construtivo, ano de construção, fração ideal. |
| **ProprietarioId** | `ContribuinteId` do sujeito passivo (proprietário/possuidor). |
| **TipoUsoImovel** | Enum: Residencial, Comercial, Industrial, Servicos, Misto, Territorial. |

## 2. Modelo

- **Identidade:** `ImovelId` — `readonly record struct ImovelId(Guid Value)`; fábrica `ImovelId.New()`.
- **Raiz:** `Imovel : AggregateRoot<ImovelId>, IMustHaveTenant` (`sealed`).
- **Construção:** construtor privado + **factory** `Cadastrar(...)`. Nasce válida e `Ativo`.
- **VOs persistidos como owned types** (`IdentificacaoImovel`, `EnderecoImovel`, `CaracteristicasImovel`).

## 3. Invariantes

- **I-1.** Todo `Imovel` pertence a exatamente um tenant (`IMustHaveTenant`); `TenantId` imutável.
- **I-2.** `InscricaoMunicipal` é obrigatória; `CibCodigo` e `MatriculaRgi` são opcionais (CIB — piloto não-capital obrigado só jan/2027).
- **I-3.** `ProprietarioId` é obrigatório (não `Guid.Empty`).
- **I-4.** `AreaTerreno` e `AreaConstruida` são não-negativas; `FracaoIdeal` ∈ (0, 1].
- **I-5.** `TipoUso` deve ser um valor definido do enum.
- **I-6.** Mutações (características, endereço, transferência, inativação) exigem imóvel `Ativo`.
- **I-7.** Ao cadastrar, emite `ImovelCadastrado`; mutações emitem `ImovelAtualizado`.

## 4. Comandos (escrita)

### 4.1 `CadastrarImovel` — `CadastrarImovelCommand : ICommand<Guid>`
- **Pré:** comando válido; existe `Contribuinte` (proprietário) no tenant.
- **Efeito:** cria `Imovel.Cadastrar(...)`; persiste; retorna `Guid`.
- **Handler:** `CadastrarImovelHandler(IImovelRepository, IContribuinteRepository, IUnitOfWork, ITenantContext)`.

## 5. Consultas (leitura)

### 5.1 `ListarImoveisDoContribuinte` — `ListarImoveisDoContribuinteQuery : IQuery<IReadOnlyList<ImovelResumo>>`
- Tenant-scoped (Global Query Filter); projeta para `ImovelResumo`.

## 6. Eventos

### 6.1 Domínio (in-process, MediatR)

| Evento | Emitido em |
|---|---|
| `ImovelCadastrado` | `Cadastrar`. |
| `ImovelAtualizado` | `AtualizarCaracteristicas`, `AtualizarEndereco`, `TransferirProprietario`, `Inativar`. |

Nenhum Integration Event publicado/consumido nesta versão. Envio ao SINTER **não** implementado.

## 7. Persistência

- **Tabela:** `tributos.Imoveis` + colunas owned (Identificacao*, Endereco/Logradouro/ZonaFiscal/..., Caracteristicas*).
- **Índices:** `(TenantId, ProprietarioId)`; `InscricaoMunicipal`.

## 8. Segurança, Tenant e Auditoria

- `IMustHaveTenant` + Global Query Filter + TenantInterceptor; RBAC `tributos.gerenciar` (escrita) / `tributos.ver` (leitura), negar por padrão; auditoria imutável.

## 9. Cenários BDD

- **C-1.** Dado um contribuinte, quando `CadastrarImovel`, então cria `Imovel` `Ativo` carimbado com o tenant e emite `ImovelCadastrado`.
- **C-2.** Dado proprietário inexistente, quando `CadastrarImovel`, então `InvalidOperationException`.
- **C-3.** Isolamento: tenant B não enxerga imóveis do tenant A.

## 10. Pendências `// TODO(validar-oficial)`

- Campos efetivos do BCI e zonas fiscais = lei/decreto de Maximiliano de Almeida/RS.
- Regra do dígito verificador do CIB e protocolo de envio ao SINTER (manual técnico ENAT/NT CTAT 05/2025).

## 11. Changelog

| versao | data | mudança |
|---|---|---|
| 1.0.0 | 2026-06-22 | Cadastro imobiliário (BCI) — fundação do IPTU (M6 parte 1). |

<!-- manifest
commands: CadastrarImovel
queries: ListarImoveisDoContribuinte
domainEvents: ImovelCadastrado, ImovelAtualizado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
