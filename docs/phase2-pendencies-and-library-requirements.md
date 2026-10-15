# Phase 2 — Pendências de modelagem e requisitos do `libpjsua2maui`

> Atualização de 15/10/2026: a baseline normativa para source inspection e modelagem da Phase 2 é `libpjsua2maui >= 2.17.0.5`. A versão `2.17.0.4` é mantida neste documento apenas como baseline histórica para explicar as limitações que motivaram a mudança. As conclusões de ausência de fontes abaixo não se aplicam à `2.17.0.5`.

> Estado atual: os checks estruturais da Phase 2 foram implementados e validados contra a revisão `2.17.0.5` e seu código-fonte versionado. As relações que ainda aparecem como `Partial` ou `Unknown` representam lacunas reais de evidência; não devem ser promovidas para `Established` apenas por inferência de nomes ou pelo padrão genérico do SWIG.

## 1. Objetivo e escopo

Este relatório registra o estado verificável da Phase 2 do MCP, separando:

- fatos demonstrados pelos artefatos disponíveis;
- relações estruturais que o inspector consegue estabelecer;
- relações que permanecem parciais ou desconhecidas por falta de evidência;
- limitações do parser, classificadas separadamente como `Unparsed`;
- evidências que ainda dependem de artefatos adicionais ou validação em runtime.

Nenhuma relação entre um membro C# e uma função pjproject é considerada estabelecida apenas por semelhança de nomes.

## 2. Baselines e fontes de evidência

### 2.1 Baseline histórica — `2.17.0.4`

A versão `2.17.0.4` foi a primeira baseline usada para a documentação XML e para o inventário inicial do pacote. Ela continha assemblies, XML por TFM, wrappers C# gerados e artefatos nativos publicados, mas não disponibilizava no pacote os fontes C# gerados, `pjsua2PINVOKE.cs`, `SWIGTYPE_*.cs` ou os fontes C++ do wrapper necessários para provar toda a cadeia de binding.

Essa limitação histórica não deve mais ser descrita como uma limitação da implementação atual.

### 2.2 Baseline normativa — `2.17.0.5`

A versão `2.17.0.5` é a baseline mínima atual para source inspection. Ela disponibiliza os artefatos necessários para a investigação estrutural, incluindo:

- fontes C# gerados pelo SWIG;
- `pjsua2PINVOKE.cs`;
- tipos `SWIGTYPE_*`;
- fontes C++ dos wrappers;
- headers e fontes pjproject disponíveis na revisão do produtor;
- revisão versionada do pjproject por meio do submódulo do produtor.

A árvore do produtor atualmente fixa o submódulo `/lib` em `7dfdd229f944b5f236b27cc19cdc97c3cf717567`. A revisão pjproject associada deve ser obtida do submódulo/repositório no momento da inspeção; ela não deve ser tratada como fato normativo se a revisão do produtor mudar.

Os TFMs gerenciados publicados são:

- `net10.0-android36.0`;
- `net10.0-ios26.0`;
- `net10.0-maccatalyst26.0`;
- `net10.0-windows10.0.19041`.

A documentação XML continua sendo lida por TFM em `/lib/<tfm>/`. Os XML gerados não dependem de `<summary>`: comentários `///` podem aparecer diretamente como texto do membro, enquanto `<param>` é usado quando aplicável.

## 3. Estado dos checks da Phase 2

### 3.1 Inspect generated SWIG artifacts

**Estado: Implementado e validado para a baseline `2.17.0.5`.**

O pipeline identifica os fontes gerados, `pjsua2PINVOKE.cs`, `SWIGTYPE_*`, wrappers C++, headers e ativos nativos publicados. A presença dos artefatos não depende mais da inspeção exclusiva dos assemblies.

A pendência que permanece é **reprodutibilidade completa da geração**. A inspeção estrutural não prova, para todos os artefatos, a cadeia completa de geração desde inputs SWIG, comandos, versões de todas as ferramentas, hashes dos inputs/outputs e parâmetros de compilação.

### 3.2 Model managed-to-SWIG relationships

**Estado: Implementado e validado para os elos demonstráveis.**

O `SourceInspection` correlaciona, quando presentes e delimitáveis:

```text
Managed C# member
    -> direct pjsua2PINVOKE call
    -> P/Invoke declaration / EntryPoint
    -> generated C++ wrapper
```

O matching por nome de tipo é apenas auxiliar e não constitui prova. Overloads e entry points distintos permanecem separados. Quando o inspector não consegue demonstrar um elo, a relação é mantida como `Partial` ou `Unknown`.

### 3.3 Model native pjproject mappings

**Estado: Implementado e validado para os elos demonstráveis.**

Para wrappers cujo corpo pode ser resolvido, a inspeção procura separadamente:

```text
Generated C++ wrapper
    -> pjproject declaration
    -> pjproject implementation
```

A declaração é procurada na árvore apropriada de headers, incluindo `pjsip/include/pjsua2`, e a implementação na árvore de fontes correspondente, incluindo `pjsip/src/pjsua2` quando aplicável.

A ausência de uma declaração ou implementação resolvível não é convertida em uma conclusão positiva. O elo permanece `Partial` ou `Unknown`.

### 3.4 Lifecycle, ownership, callbacks, handles e disposal

**Estado: Implementado e validado para os fatos estruturais demonstráveis; sem evidência completa para todos os semânticos.**

O modelo agora associa lifecycle facts ao tipo/membro quando a evidência pode ser delimitada. Os wrappers SWIG fornecem evidência direta para construções como:

- `HandleRef`;
- `swigCMemOwn`;
- destruição e `Dispose`;
- finalizadores;
- infraestrutura de callbacks/directors;
- mecanismos de exceção gerados pelo SWIG.

Continuam genuinamente pendentes, quando não demonstrados por fonte suficiente:

- ownership completo por tipo, parâmetro e retorno;
- transferência de ownership entre C++ e C#;
- distinção completa entre referências proprietárias e emprestadas;
- retenção e liberação de delegates/callbacks;
- comportamento de callbacks tardios após `Dispose`;
- ordem completa de destruição entre `Endpoint`, contas, chamadas, mídia, transportes e demais recursos;
- thread affinity e contexto de execução de callbacks;
- semântica de memória nativa de buffers, parâmetros e retornos específicos.

Padrões genéricos conhecidos do SWIG não são usados como substitutos para evidência específica da biblioteca.

### 3.5 Platform-specific evidence and confidence

**Estado: Implementado e validado como evidência de distribuição/geração.**

O MCP representa evidência por plataforma, TFM, RID quando disponível, artefato, símbolo, confiança e proveniência. A implementação correlaciona a evidência de fonte com os quatro TFMs publicados e com os ativos nativos distribuídos.

Isso demonstra presença/distribuição e relação estrutural de geração. Não demonstra execução do binding em runtime. O estado de runtime permanece `NotValidated`.

Ainda podem permanecer pendentes:

- export/linkage nativo exato de cada entry point em cada binário/arquitetura;
- equivalência de implementação entre plataformas quando não demonstrada pela fonte;
- carregamento efetivo de cada biblioteca nativa em runtime.

## 4. Status semântico das relações

A implementação deve preservar a seguinte distinção:

| Status | Significado |
|---|---|
| `Established` | O elo é demonstrado pelos artefatos e pela análise atual. |
| `Partial` | Parte da cadeia foi demonstrada, mas um endpoint ou elo intermediário permanece sem evidência. |
| `Unknown` | Os artefatos disponíveis não permitem identificar o destino com segurança. |
| `Unparsed` | Existe construção de fonte relevante, mas o inspector não consegue interpretá-la; isso é uma limitação do inspector, não evidência de ausência no binding. |

`Unparsed` não deve ser contabilizado como `Unknown` e não deve ser usado para afirmar que uma relação não existe.

## 5. O que deixou de ser pendência estrutural

Não devem mais ser tratados como bloqueios estruturais da Phase 2:

- ausência de fontes C# gerados na baseline atual;
- ausência de `pjsua2PINVOKE.cs` e `SWIGTYPE_*` na baseline atual;
- ausência de fontes C++ dos wrappers na baseline atual;
- ausência de uma revisão versionada do pjproject no produtor;
- ausência de correlação managed → `pjsua2PINVOKE` → P/Invoke → wrapper para os elos que podem ser demonstrados;
- uso de matching exclusivamente por nome como mecanismo de prova;
- falta de associação de lifecycle facts ao símbolo/tipo quando a fonte fornece o fato;
- confusão entre limitação de parser e ausência de evidência;
- ausência de evidência de plataforma no modelo;
- ausência de proveniência para os artefatos inspecionados;
- baseline `2.17.0.4` como requisito mínimo atual.

A existência histórica dessas limitações deve permanecer documentada apenas para explicar a evolução da Phase 2.

## 6. Evidências que permanecem pendentes

As seguintes pendências são reais e devem continuar visíveis no roadmap/inventário:

1. **Ownership completo:** provar criação, empréstimo, transferência e destruição para todos os tipos, parâmetros e retornos relevantes.
2. **Callbacks:** provar registro, retenção, remoção, destruição e callbacks tardios para todos os delegates relevantes.
3. **Destruction ordering:** provar a ordem de encerramento entre objetos relacionados, especialmente `Endpoint`, contas, chamadas, mídia e transportes.
4. **Thread semantics:** provar thread affinity e contexto de callback quando a fonte disponível não for suficiente.
5. **Native memory:** provar ownership e lifetime de buffers, strings, estruturas e retornos nativos específicos.
6. **Native linkage:** provar o símbolo/export/linkage efetivo para cada entry point por plataforma e arquitetura.
7. **Cross-platform equivalence:** provar equivalência de implementação onde wrappers ou código nativo divergem entre plataformas.
8. **Generation reproducibility:** registrar inputs SWIG, comandos, versões de ferramentas, hashes e revisão pjproject suficientes para reproduzir os artefatos sem depender de estado implícito do ambiente.
9. **Runtime validation:** executar testes que demonstrem carregamento, invocação e lifecycle real das bibliotecas nativas nas plataformas suportadas.

Esses itens não devem ser marcados como resolvidos somente porque a cadeia está estruturalmente representada.

## 7. Requisitos de evidência para eliminar as pendências

Quando a Phase 2 for ampliada para eliminar as pendências acima, a evidência deve ser vinculada ao artefato específico que a sustenta. Exemplos:

- fonte C# gerado e membro exato;
- `pjsua2PINVOKE` e `EntryPoint` exatos;
- wrapper C++ e chamada nativa exata;
- declaração e implementação pjproject exatas;
- header/ABI e símbolo exportado;
- manifesto de geração contendo inputs, versão de SWIG, compilador, opções e hashes;
- teste de runtime reproduzível para lifecycle/callback/linkage.

Inferência por nome, convenção do SWIG ou comportamento esperado não substitui essas evidências.

## 8. Validação da implementação atual

A validação da Phase 2 deve continuar usando o Quality Gate existente do projeto, mantendo os limites mínimos de:

- **90% de linhas**;
- **90% de branches**;
- **90% de métodos**.

Os percentuais de cobertura do código de teste não devem ser confundidos com percentuais de cobertura dos mappings de source inspection. A cobertura de testes demonstra execução dos caminhos do MCP; os status `Established`/`Partial`/`Unknown` demonstram o nível de evidência de cada relação.

## 9. Conclusão

A Phase 2 possui atualmente o modelo estrutural necessário para inspecionar a cadeia demonstrável:

```text
Managed C#
    ↓
Generated SWIG C#
    ↓
pjsua2PINVOKE / P-Invoke
    ↓
Generated C++ wrapper
    ↓
pjproject declaration
    ↓
pjproject implementation
    ↓
Platform native artifact
```

Os elos que podem ser demonstrados pelos artefatos atuais são representados e validados. Os elos que não podem ser demonstrados permanecem explicitamente `Partial` ou `Unknown`, e construções que o inspector ainda não interpreta são classificadas separadamente como `Unparsed`.

Portanto, a Phase 2 **não deve ser considerada como tendo todas as evidências semânticas completas**. O que está concluído é a infraestrutura e a modelagem estrutural para produzir evidência rastreável; ownership completo, callbacks/lifetime, destruction ordering, thread semantics, linkage nativo exato, reprodutibilidade completa e runtime validation continuam pendentes onde a evidência ainda não existe.
