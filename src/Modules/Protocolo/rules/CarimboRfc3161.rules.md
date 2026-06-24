# Spec BDD — Carimbo de Tempo RFC 3161 / ACT ICP-Brasil (Peca 1, W9.4)

> Autoridade: `docs/architecture/m9-prep/PROTOCOLO-ACT-CONARQ-DESIGN.md` (Peca 1).
> Porta `ICarimbadorDeTempo` (recebe o HASH, devolve o TST validado) atras de ACL + Polly.
> Substitui `ICarimboDeTempoService`; `CarimbadorDeTempoLocalService` vira fallback/dev.
> M9 = stub/simulado; ACT credenciada real = M10 (// TODO(M10)).

## Contexto

- RFC 3161 (TSP): o cliente monta o TSQ (`MessageImprint` = SHA-256 do conteudo + nonce + certReq),
  POST ao endpoint da ACT (`application/timestamp-query` -> `application/timestamp-reply`); a ACT
  responde com o TST (CMS SignedData cujo eContent e o TSTInfo: genTime, serial, policy, imprint, nonce).
- A ACT NUNCA ve o conteudo — apenas o hash (privacidade).
- O carimbo so e aceito apos validacao; falha => NAO persiste e propaga erro de dominio.

## Cenarios

### C-1 — granted: TST valido vinculado ao hash
```
Dado um documento juntado com Hash H
Quando o carimbador solicita carimbo para H e a ACT responde granted com TST valido
Entao o CarimboDeTempo retornado tem Origem = Act, TokenBase64, Serial, Politica e HashCarimbado = H
E o documento e assinado com esse carimbo
```

### C-2 — PKIFailureInfo: rejeicao mapeada para erro de dominio explicito
```
Dado um documento com Hash H
Quando a ACT responde rejection com PKIFailureInfo = badAlg (ou badRequest, unacceptedPolicy, ...)
Entao CarimboDeTempoException e lancada carregando o PKIFailureInfo mapeado
E nenhum carimbo e persistido
```

### C-3 — MessageImprint divergente: rejeitado
```
Dado um documento com Hash H
Quando o TST retornado tem MessageImprint != H
Entao CarimboDeTempoException e lancada (imprint divergente) e nada persiste
```

### C-4 — nonce divergente (anti-replay): rejeitado
```
Dado um TSQ enviado com nonce N
Quando o TST ecoa um nonce != N
Entao CarimboDeTempoException e lancada (nonce divergente) e nada persiste
```

### C-5 — cadeia nao-confiavel / EKU ausente: rejeitado
```
Dado um TST cuja AC do tempo nao consta em EmissoresConfiaveis (ou sem id-kp-timeStamping)
Quando o carimbo e validado
Entao CarimboDeTempoException e lancada e nada persiste
```

### C-6 — genTime fora da janela tolerada: rejeitado
```
Dado ToleranciaGenTime configurada
Quando genTime do TST esta fora da janela [agora - tol, agora + tol]
Entao CarimboDeTempoException e lancada e nada persiste
```

### C-7 — timeout / circuit breaker (Polly): resiliencia
```
Dado a ACT indisponivel/lenta
Quando o HttpClient resiliente esgota retry/abre o circuito
Entao a operacao falha de forma controlada (sem corromper estado) e nada persiste
```

### C-8 — Invariante "Alta exige Act": carimbo local recusado em ato qualificado
```
Dado um documento de criticidade Alta (assinatura qualificada)
Quando se tenta assinar com CarimboDeTempo de Origem = Local
Entao Documento.Assinar lanca (carimbo local nao satisfaz ato qualificado ICP-Brasil)
```

### C-9 — Vinculo hash <-> token revalidavel (oponibilidade ao TCE)
```
Dado um documento assinado com carimbo Act
Quando VerificarCarimbo e chamado
Entao confirma HashCarimbado == Hash do documento
```

## Invariantes
- I-CT1: carimbo so e aceito apos validacao completa (status/imprint/nonce/cadeia/EKU/genTime).
- I-CT2: HashCarimbado == Documento.Hash.Valor (vinculo verificado em Assinar e em VerificarCarimbo).
- I-CT3: criticidade Alta => Origem == Act (carimbo local recusado).
- I-CT4: I/O idempotente (nonce), resiliente (Polly), atras de ACL; segredo da ACT em Key Vault.
