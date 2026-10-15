# Phase 2 — Pendências de modelagem e requisitos do `libpjsua2maui`

> Atualização de 15/10/2026: este relatório foi produzido contra a baseline histórica `2.17.0.4`. A baseline mínima atual para source inspection é `libpjsua2maui >= 2.17.0.5`; consulte `docs/package-knowledge.md` e `docs/libpjsua2maui-source.md` para o estado atualizado. As conclusões sobre ausência de fontes no pacote abaixo não se aplicam à `2.17.0.5`.

> Estado da implementação: a versão `2.17.0.5` foi inspecionada e o extrator agora indexa os fontes C# gerados pelo SWIG, wrappers C++ e headers publicados. O produtor está fixado localmente em `7dfdd229f944b5f236b27cc19cdc97c3cf717567`, com pjproject em `5a457451fa2712ba18e12b01738e8ff3af2b26fd`. As pendências remanescentes são relações nativas que continuam parciais ou desconhecidas quando o pacote não contém a implementação pjproject correspondente; elas não devem ser tratadas como ausência de artefato.

## 1. Objetivo e escopo

Este relatório registra o estado verificável da Phase 2 do MCP, as limitações de evidência que permanecem por elo e os artefatos necessários para reproduzir a investigação no repositório `remotecodehub/libpjsua2maui`.

O relatório distingue:

- o que já está representado no MCP;
- o que foi confirmado no pacote NuGet;
- o que foi encontrado no código gerado do submódulo;
- o que permanece apenas parcial ou desconhecido;
- o que precisa ser adicionado ao repositório da biblioteca e, quando aplicável, ao `.nupkg`.

Nenhuma relação entre um método C# e uma função pjproject é considerada confirmada apenas por semelhança de nomes.

## 2. Estado e fontes de evidência utilizados

### Repositório do MCP

Os commits recentes da branch `main` no momento da análise foram:

- `ccdaaa5 feat: update PackageKnowledgeExtractor and tests to handle null source references and add platform evidence validation`;
- `0d01511 feat: Enhance Phase 2 knowledge model with platform evidence and confidence metrics`;
- `a2bfe14 docs: add NuGet MCP skill and pin source submodule`.

### Submódulo da biblioteca — baseline histórica

O relatório histórico foi originalmente produzido quando o submódulo `/lib` apontava para:

- repositório: `https://github.com/remotecodehub/libpjsua2maui.git`;
- tag: `v2.17.0.4`;
- commit: `03e4cf1194b810085447d0b1add34fbb5a9c8d58`.

Essa revisão contém wrappers C# gerados para Android, iOS, MacCatalyst e Windows, arquivos `pjsua2PINVOKE.cs`, tipos `SWIGTYPE_*`, documentação de build e o `Makefile` do binding. Ela não contém, na árvore versionada examinada, os inputs `pjsua2.i` e `symbols.i` referenciados pelo Makefile, o `pjsua2_wrap.cpp`, uma árvore versionada de pjproject ou uma revisão pjproject identificada.

### Pacote publicado — baseline histórica

O pacote histórico inspecionado foi `libpjsua2maui` versão `2.17.0.4`. A documentação existente registra 60 entradas de pacote, 268 entradas de classes/records/structs e nenhum tipo interface identificado no inventário publicado.

Para a implementação atual, a fonte normativa é `libpjsua2maui` `2.17.0.5`, cujo pacote e fontes publicados foram re-inspecionados; os artefatos gerados agora sustentam os mappings diretos descritos em `docs/phase2-type-modeling-inventory.md`.

Os TFMs gerenciados encontrados são:

- `net10.0-android36.0`;
- `net10.0-ios26.0`;
- `net10.0-maccatalyst26.0`;
- `net10.0-windows10.0.19041`.

O `.nupkg` contém assemblies, documentação XML, `build/libpjsua2maui.targets`, ativos Android, bibliotecas estáticas de iOS/MacCatalyst, DLLs Windows, conteúdo Java Android e o `.pri` do Windows. Não contém os fontes C# gerados pelo SWIG, `pjsua2PINVOKE.cs`, `SWIGTYPE_*`, inputs SWIG ou fontes C++ do wrapper.

## 3. Situação dos checks da Phase 2

### 3.1 Inspect generated SWIG artifacts available from the package/repository

**Estado: parcialmente atendido, com o inventário de presença concluído.**

O MCP consegue distinguir artefatos do pacote, tipos identificados nos assemblies, fontes geradas presentes no submódulo e ativos nativos publicados. O inventário atual registra:

- 315 arquivos C# gerados por plataforma no submódulo;
- um `pjsua2PINVOKE.cs` por plataforma, com aproximadamente 6.809 linhas;
- 16 tipos `SWIGTYPE_*` no conjunto inspecionado;
- os ativos nativos publicados por TFM/RID;
- a ausência de fontes gerados no `.nupkg`.

A pendência é de completude e reprodutibilidade: não é possível relacionar todos os arquivos gerados à entrada SWIG, ao comando de geração, ao commit pjproject e aos hashes dos artefatos porque esses elementos não estão disponíveis na revisão fixada.

### 3.2 Model managed-to-SWIG relationships

**Estado atual: concluído.**

Há evidência suficiente para modelar relações representativas entre tipos gerenciados e wrappers C# gerados. Os wrappers mostram, entre outros elementos:

- classes públicas geradas pelo SWIG;
- `HandleRef` para handles nativos;
- campo `swigCMemOwn`;
- métodos de destruição e `Dispose`;
- finalizadores;
- chamadas `DllImport` para a camada `pjsua2PINVOKE`;
- entradas auxiliares para propriedades, construtores e métodos.

O `SourceInspection` percorre os fontes publicados e relaciona chamadas diretas de membros gerados ao símbolo `pjsua2PINVOKE`, à declaração `DllImport`/`EntryPoint` e à definição do wrapper C++ quando ela existe. O matching por nome de tipo permanece apenas como observação auxiliar; não é usado como prova da cadeia. Chamadas sem elo demonstrável permanecem explicitamente `Partial` ou `Unknown`.

### 3.3 Model native pjproject mappings where evidence exists

**Estado atual: concluído para os elos demonstráveis.**

O produtor e o submódulo agora são inspecionados de modo versionado e rastreável. O inspector resolve definições `pjsua2_wrap.cpp`, headers pjproject em `pjsip/include/pjsua2`, implementações em `pjsip/src/pjsua2` e o commit/tag da revisão nativa utilizada.

As relações carregam separadamente o wrapper publicado, a declaração em `pjsip/include/pjsua2` e a implementação em `pjsip/src/pjsua2`, além do commit/tag da revisão pjproject utilizada.

Quando o corpo do wrapper não contém uma chamada resolvível ou não há declaração/implementação correspondente, a relação continua parcial ou desconhecida. Isso é agora uma limitação específica daquele elo, não uma limitação estrutural do pipeline.

### 3.4 Model ownership, callbacks, disposal, handles, and lifecycle semantics

**Estado atual: concluído para os fatos demonstráveis.**

Os wrappers examinados comprovam padrões de lifecycle em classes geradas, incluindo `HandleRef`, `swigCMemOwn`, finalizadores, `Dispose`, chamadas de destruição e a infraestrutura de exceção/callback do SWIG. Também há referências a mecanismos de `director` e `SWIGPendingException` nos arquivos gerados.

Ainda faltam, por tipo e plataforma:

- tabela completa de criação, transferência e destruição de ownership;
- identificação de referências proprietárias e não proprietárias;
- todos os delegates e métodos de registro/remoção de callbacks;
- exigências de retenção de delegates e objetos gerenciados;
- ordem de encerramento entre `Endpoint`, contas, chamadas, mídia e transportes;
- comportamento demonstrado após `Dispose`;
- restrições de thread e callbacks tardios comprovadas pela implementação nativa.

Riscos genéricos do padrão SWIG não são registrados como fatos específicos de uma classe sem evidência correspondente; comportamentos ainda não observados permanecem fora do inventário.

### 3.5 Add platform-specific evidence and confidence

**Estado atual: concluído.**

O MCP já possui representação para plataforma, TFM, RID, estado do artefato, confiança e proveniência. O pacote comprova a distribuição de ativos para Android, iOS, MacCatalyst e Windows. A inspeção também encontrou quatro conjuntos de wrappers C# gerados; Android, iOS e MacCatalyst possuem arquivos representativos iguais em alguns casos, enquanto Windows apresenta diferenças observáveis.

O extrator emite evidência por assembly e por símbolo gerenciado para os quatro TFMs publicados, relacionando fontes C#, wrapper C++, headers, ativos nativos, TFM, RID quando disponível, confiança, provenance e limitações. Isso comprova distribuição/geração, não execução em runtime; o campo `Runtime` permanece `NotValidated`.

## 4. Modelos do MCP já reutilizados

Os seguintes contratos existentes são suficientes para preservar a maior parte da distinção necessária e devem continuar sendo a base da implementação:

- `KnowledgeNodeReference` para identidade e referências a tipos, membros e artefatos;
- `KnowledgeRelationship` para relações confirmadas, parciais e desconhecidas;
- `LifecycleFact` para ownership, callbacks, handles, disposal e ordenação;
- `SymbolRepresentation` para a representação de símbolos e seus tipos;
- `FactProvenance` para pacote, assembly, TFM, RID, arquivo, repositório, revisão e evidência específica;
- `PackageArtifact` para os arquivos efetivamente presentes no pacote;
- `PlatformEvidence` para estado de disponibilidade, plataforma e confiança.

As extensões de confiança, plataforma e proveniência devem permanecer separadas do status da relação. Uma relação pode ter elos conhecidos com evidência forte e, ainda assim, permanecer parcial porque o destino final não foi localizado.

## 5. Alterações a serem feitas no repositório `remotecodehub/libpjsua2maui`

Esta é a lista de artefatos necessários para eliminar as principais pendências da Phase 2. Os caminhos abaixo são o contrato recomendado; se a organização do repositório mudar, o manifesto de origem deverá registrar o caminho efetivo.

### 5.1 Inputs do SWIG

Versionar os arquivos de entrada que o Makefile já referencia:

- `res/pj/pjsua2.i`;
- `res/pj/symbols.i`.

Esses arquivos devem conter ou incluir as declarações que originam os tipos públicos e os símbolos mapeados. Cada release deve registrar SHA-256 desses inputs e a versão do SWIG usada.

### 5.2 Fontes C++ geradas e wrappers nativos

Disponibilizar o `pjsua2_wrap.cpp` efetivamente compilado ou uma fonte equivalente reproduzível para cada plataforma:

- `src/libpjsua2maui/Platforms/Android/pjsua2maui/pjsua2/pjsua2_wrap.cpp`;
- `src/libpjsua2maui/Platforms/iOS/pjsua2maui/pjsua2/pjsua2_wrap.cpp`;
- `src/libpjsua2maui/Platforms/MacCatalyst/pjsua2maui/pjsua2/pjsua2_wrap.cpp`;
- `src/libpjsua2maui/Platforms/Windows/pjsua2maui/pjsua2/pjsua2_wrap.cpp`.

Se esses fontes não forem versionados, o CI de release deve publicar um artefato de auditoria contendo o fonte exato, o comando SWIG, os hashes dos inputs, a versão do compilador e o commit pjproject. Um binário sozinho não é suficiente para comprovar o mapping.

### 5.3 Fontes e revisão do pjproject

Adicionar uma referência reproduzível à árvore nativa utilizada:

- `.gitmodules`, se pjproject for mantido como submódulo;
- `res/pj/pjproject/`, ou caminho equivalente, se a fonte for versionada no repositório;
- `res/pj/pjproject.commit`, se a fonte for obtida no build e não puder ser submódulo;
- `docs/pjproject-source.md`, documentando repositório, tag/commit, patches e opções de compilação.

A revisão deve permitir localizar, quando aplicável, os headers e implementações em:

- `pjlib/include/`;
- `pjlib-util/include/`;
- `pjmedia/include/`;
- `pjsip/include/pjsua2/`;
- `pjnath/include/`;
- os diretórios de implementação correspondentes.

### 5.4 Wrappers C# e configuração de geração

Manter ou publicar, por plataforma, os arquivos C# gerados e a configuração que os produziu:

- `pjsua2PINVOKE.cs`;
- todos os `SWIGTYPE_*.cs`;
- classes C# geradas públicas e auxiliares;
- configuração `.i`, `%include`, typemaps e opções de geração;
- versão exata do SWIG e do compilador.

Os caminhos efetivos devem ser incluídos no manifesto, mesmo que a árvore atual continue usando os diretórios específicos de cada plataforma.

### 5.5 Manifestos de mapping e lifecycle

Adicionar ao repositório, preferencialmente em `docs/`:

- `docs/swig-binding-inventory.json`;
- `docs/native-mapping-inventory.json`;
- `docs/lifecycle-inventory.json`;
- `docs/source-artifact-manifest.json`.

Cada entrada deve identificar, no mínimo:

- plataforma, TFM e RID;
- assembly, namespace, tipo, membro e identificador XML;
- arquivo C# gerado e símbolo SWIG;
- método `pjsua2PINVOKE` e entry point P/Invoke;
- arquivo/símbolo C++ do wrapper;
- declaração e implementação pjproject, quando confirmadas;
- handle, ownership, `Dispose`, finalizador e callback, quando aplicáveis;
- status (`confirmed`, `partial`, `unknown` ou equivalente);
- confiança, limitações e evidência específica;
- SHA-256 e revisão de cada artefato.

Esses manifestos são necessários para que o MCP não precise inferir uma cadeia completa a partir de nomes semelhantes.

### 5.6 Build, CI e documentação

Atualizar os arquivos existentes de geração/publicação:

- `res/pj/pjsua2maui/Makefile`;
- `.github/workflows/ci-cd.yml`;
- `docs/SWIG.md`;
- documentação equivalente atualmente usada pelo workflow de release.

O Makefile deve validar a existência dos inputs, registrar a versão SWIG, registrar o commit pjproject e produzir os wrappers por plataforma. O workflow deve publicar os manifestos, hashes e fontes de auditoria junto da release correspondente, sem misturar artefatos de revisões diferentes.

## 6. Arquivos que o `.nupkg` deve conter

### 6.1 Conteúdo de runtime já necessário

O pacote deve continuar contendo os assets atuais, associados aos TFMs/RIDs corretos:

- `build/libpjsua2maui.targets`;
- `lib/<tfm>/libpjsua2maui.dll` e XML correspondente para os quatro TFMs;
- `runtimes/android-arm/native/*.so`;
- `runtimes/android-arm64/native/*.so`;
- `runtimes/android-x86/native/*.so`;
- `runtimes/android-x86_64/native/*.so`;
- `runtimes/ios-arm64/native/libpjsua2.a`;
- `runtimes/iossimulator-arm64/native/libpjsua2.a`;
- `runtimes/iossimulator-x64/native/libpjsua2.a`;
- `runtimes/maccatalyst-arm64/native/libpjsua2.a`;
- `runtimes/maccatalyst-x64/native/libpjsua2.a`;
- `runtimes/win-arm64/native/pjsua2.dll`;
- `runtimes/win-x64/native/pjsua2.dll`;
- conteúdo Java Android e o `.pri` Windows quando exigidos pelo build atual.

Os nomes finais devem ser confirmados pelo `.nupkg` da release e registrados no manifesto, pois o padrão `*.so` acima representa o conjunto observado e não substitui a lista concreta de arquivos.

### 6.2 Metadados recomendados para tornar a Phase 2 reproduzível

Adicionar ao pacote, em `build/` ou numa pasta de metadados equivalente:

- `build/libpjsua2maui.bindings.json`;
- `build/libpjsua2maui.native-mappings.json`;
- `build/libpjsua2maui.lifecycle.json`;
- `build/libpjsua2maui.source-manifest.json`.

Esses arquivos devem ser específicos da versão do pacote e conter o mapping por TFM/RID/plataforma. O `source-manifest` deve apontar para a tag/commit da biblioteca, os hashes dos wrappers e a revisão pjproject.

Os fontes C# gerados não precisam ser distribuídos como conteúdo de consumo se os manifestos forem completos. Para auditoria independente, a publicação também pode incluir:

- `contentFiles/any/any/swig/Android/*.cs`;
- `contentFiles/any/any/swig/iOS/*.cs`;
- `contentFiles/any/any/swig/MacCatalyst/*.cs`;
- `contentFiles/any/any/swig/Windows/*.cs`;
- `contentFiles/any/any/swig/*/pjsua2PINVOKE.cs`;
- `contentFiles/any/any/swig/*/SWIGTYPE_*.cs`.

Essa alternativa deve ser avaliada quanto ao tamanho e à política de distribuição. Ela é útil para auditoria, mas não substitui os fontes C++ e a revisão pjproject.

O pacote não precisa carregar toda a árvore pjproject se o `source-manifest` apontar para uma revisão pública, imutável e reproduzível. Se essa revisão não estiver acessível aos consumidores ou ao processo de auditoria, os headers/patches necessários deverão ser publicados como artefato de origem da release.

## 7. Ajustes necessários no MCP após a publicação dos artefatos

Quando os artefatos acima estiverem disponíveis, o MCP deverá:

1. adquirir e validar os quatro manifestos usando o mesmo Package ID e versão do pacote;
2. rejeitar mistura de manifests, assemblies ou fontes de versões diferentes;
3. indexar o mapping por tipo, membro, TFM, RID e plataforma;
4. associar cada elo a uma proveniência específica, sem promover texto descritivo a símbolo confirmado;
5. preservar `partial` e `unknown` quando um elo continuar ausente;
6. detectar conflitos entre plataformas e hashes diferentes;
7. manter a distinção entre arquivo de pacote, fonte gerado, wrapper C++ e API pjproject;
8. garantir ordenação, deduplicação e round-trip JSON determinísticos;
9. acrescentar fixtures reduzidas dos manifestos para testes sem rede;
10. construir uma matriz verificável de cobertura por tipo/membro/TFM/RID.

O índice não deve baixar, carregar ou executar bibliotecas nativas durante a aquisição ordinária.

## 8. Critérios objetivos para concluir a Phase 2

A Phase 2 somente poderá ser marcada como concluída quando houver evidência para todos os pontos aplicáveis:

- os 268 tipos inventariados possuem estado de exame explícito;
- cada relação gerenciada-SWIG confirmada aponta para arquivo e símbolo verificáveis;
- cada mapping nativo confirmado possui declaração/wrapper e, quando alegado, destino pjproject identificável;
- ownership, callbacks, handles e disposal estão registrados por tipo quando demonstráveis;
- a matriz Android/iOS/MacCatalyst/Windows diferencia presença gerenciada de implementação nativa;
- os artefatos e manifestos são vinculados à versão `2.17.0.5` ou a uma versão posterior explicitamente identificada;
- limitações e relações desconhecidas permanecem visíveis;
- o pipeline `PackageAcquirer → PackageKnowledgeExtractor → KnowledgeIndexer → JsonKnowledgeStore` preserva os fatos;
- testes cobrem proveniência, status parcial/desconhecido, plataforma, lifecycle, deduplicação e persistência;
- o quality gate executa e comprova os limiares configurados.

Não é necessário que todos os tipos tenham um destino pjproject conhecido quando a fonte não permitir isso; é necessário que a ausência seja demonstrada e modelada, e não escondida como uma relação confirmada.

## 9. Conclusão

Na baseline atual, a limitação determinante deixou de ser a ausência dos fontes gerados no pacote. O MCP consegue registrar os artefatos publicados e as relações demonstráveis, mas ainda preserva como parciais ou desconhecidas as cadeias cujo destino pjproject não pode ser provado apenas pelo conteúdo distribuído. A investigação adicional deve usar o submódulo pjproject fixado e a implementação C++ correspondente, sem converter semelhança textual em mapeamento confirmado.

O MCP já possui contratos para proveniência, confiança, plataforma, relações e lifecycle. Com a baseline `2.17.0.5`, os três checks anteriormente parciais estão implementados e cobertos por fixtures sintéticas e por uma fixture que lê o produtor/pjproject fixados. A Phase 2 preserva desconhecidos reais, mas não depende mais da ausência de fontes gerados ou de wrappers para realizar os mappings demonstráveis.
