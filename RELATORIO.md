# RELATÓRIO DE MIGRAÇÃO CELESTE PARA ANDROID

## ETAPA 0.2 - Identificação de csproj e referências net45/x86

### Arquivo Identificado
- **Celeste.csproj**
  - Framework: `net45`
  - Plataforma: `x86`

### Referências Relevantes
- **XNA**: Referências antigas presentes no projeto.

### Próximos Passos
- Planejar migração para framework mais recente.
- Substituir dependências obsoletas.

## ETAPA 0.3 - Mapeamento de IO

### Engine.cs
- **ContentDirectory**: Combina `AssemblyDirectory` com `Instance.Content.RootDirectory` para determinar o diretório de conteúdo.
- **Localização**: Linha 87 no arquivo `Monocle/Engine.cs`.

### Próximos Passos
- Verificar dependências de `AssemblyDirectory` e `Instance.Content.RootDirectory` para garantir compatibilidade com Android.

## ETAPA 0.5 - Mapeamento de Áudio

### Audio.cs
- **Classe**: `Audio`
- **Localização**: Linha 11 no arquivo `Celeste/Audio.cs`.
- **Detalhes**:
  - Utiliza FMOD para gerenciamento de áudio.
  - Contém subclasse `Banks` para organizar bancos de áudio (`Master`, `Music`, `Sfxs`).

### Próximos Passos
- Verificar compatibilidade do FMOD com Android.
- Planejar migração de bancos de áudio para o formato suportado.

## ETAPA 0.7 - Mapeamento de Reflexão

### Overworld.cs e Outros Pontos
- **Referência**: Reflexão utilizada em `Overworld.cs` e outros locais como `Commands`, `Tracker`, `Pooler`, e `SpawnManager`.
- **Exemplo**: Linha 412 em `Celeste.Pico8/Emulator.cs`:
  - `Engine.Scene = new OverworldLoader(Overworld.StartMode.Titlescreen);`

### Próximos Passos
- Garantir que o trimming/linker no Android não quebre o uso de reflexão.
- Planejar ajustes necessários para compatibilidade.

## ETAPA 0.8 - Mapeamento de Saves

### UserIO.cs
- **Classe**: `UserIO`
- **Localização**: Linha 12 no arquivo `Celeste/UserIO.cs`.
- **Detalhes**:
  - Gerencia operações de leitura e escrita de saves.
  - Utiliza caminhos relativos, como "Saves".

### Próximos Passos
- Redirecionar operações de I/O para armazenamento específico do aplicativo no Android.
- Substituir caminhos relativos por serviços de plataforma.

## ETAPA 0.9 - Diagnóstico, Riscos e Decisões Iniciais

### Diagnóstico do Repositório
1. **Arquitetura**: Projeto .NET Framework 4.5 com alvo x86, baseado em MonoGame e dependente de XNA.
2. **Componentes Críticos Identificados**:
   - **Engine.cs**: Gerencia ContentDirectory com dependência de Assembly.Location.
   - **Audio.cs**: Utiliza FMOD com estrutura de bancos (Master, Music, Sfxs).
   - **UserIO.cs**: Gerencia saves com caminhos relativos ("Saves").
   - **Overworld.cs**: Utiliza reflexão para descoberta dinâmica de tipos.

### Riscos Identificados
1. **IO e Paths**: Dependência de `AssemblyDirectory` e caminhos absolutos necessitará redirecionamento para Storage do Android via SAF/app-specific.
2. **Content.zip**: Não pode estar embutido no APK; precisa de estratégia de download/streaming.
3. **XNB Format**: Arquivos XNB não são suportados nativamente em Android; converter ou implementar parser.
4. **FMOD**: Bindings C# existem, mas requerem testes de compatibilidade com .NET 8/Android.
5. **Trimming/Linker**: Uso de reflexão quebra builds otimizados; exige RootDescriptor.xml ou AttributePreservation.
6. **Input e Plataforma**: Código atualmente assume teclado/mouse/gamepad desktop; adaptar para touch.

### Decisões Iniciais
1. **Framework Alvo**: .NET 8 + MonoGame 3.8.1.
2. **Estratégia de Content**: Extrair Content.zip em runtime para cache persistente do app.
3. **Paths**: Serviço `IPlatformPaths` com implementação Android usando `Context.FilesDir` e SAF.
4. **Audio**: Implementar `IAudioService` com FMOD nativo (bindings JNI).
5. **Trimming**: Usar `RootDescriptor.xml` para preservar tipos reflexivos.

### Plano de Execução
- **ETAPA 1**: Organizar projeto base (gradle + estrutura .NET Core).
- **ETAPA 2**: Migrar código para `Celeste.Core` (.NET 8).
- **ETAPA 3**: Implementar serviços de plataforma (Paths, Log, Assets).
- **ETAPA 4**: Adaptar Content (XNB → PNG + JSON ou binário).
- **ETAPA 5**: Integrar FMOD nativo.
- **ETAPA 6**: Implementar Input touch.
- **ETAPA 7**: Compilar, testar e otimizar.

## ETAPA 1 - Criar Solution e Projetos

### Ações Realizadas
1. **1.1** - Criada solution `Celeste.sln` com `dotnet new sln -n Celeste`.
2. **1.2** - Criado projeto classlib `Celeste.Core` em `src/Celeste.Core` com framework `net9.0-android`.
3. **1.3** - Criado projeto MonoGame Android `Celeste.Android` em `src/Celeste.Android`.
4. **1.4** - Ambos os projetos adicionados à solution e referência `Celeste.Core` adicionada ao `Celeste.Android`.
5. **1.5** - Configurado `ApplicationId = celestemeown.app` e label `Celeste` no `Celeste.Android.csproj`.
6. **1.6** - Configurado `RuntimeIdentifiers = android-arm64` (64-bit only, arm64-v8a).
7. **1.7** - Build realizado com sucesso:
   - `dotnet build src/Celeste.Core -c Release` ✓
   - `dotnet build src/Celeste.Android -c Release` ✓

### Estrutura de Projetos
```
Celeste/
  ├── Celeste.sln
  ├── src/
      ├── Celeste.Core/
      │   ├── Celeste.Core.csproj (net9.0-android, classlib)
      │   └── bin/Release/net9.0-android/Celeste.Core.dll
      └── Celeste.Android/
          ├── Celeste.Android.csproj (net9.0-android, app)
          ├── GameActivity.cs (template base)
          └── bin/Release/net9.0-android/ (APK/bundle)
```

### Próximos Passos
- Mover código `Celeste/`, `Monocle/`, `SimplexNoise/`, `FMOD/`, `FMOD.Studio/` para `src/Celeste.Core/`.
- Remover referências XNA antigas e compatibilizar build.

## ETAPA 2 - Migração de Código para Core

### Ações Realizadas
1. **2.1** - Código `Celeste/`, `Monocle/`, `SimplexNoise/`, `FMOD/`, `FMOD.Studio/` copiados para `src/Celeste.Core/`.
2. **2.2** - Removidas referências a `Celeste.Editor` e `Celeste.Pico8` (não disponível em Android).
3. **2.3** - Adicionadas dependências MonoGame Framework Android ao Celeste.Core.csproj.
4. **2.4** - Habilitado unsafe code no Celeste.Core.csproj.
5. **2.5** - Desabilitado método `OnExiting` que não existe em MonoGame Android.

### Status de Compilação
- **Warnings**: 6174 (principalmente CS8625 nullability)
- **Erros**: 104 (principalmente em Credits.cs, BirdNPC.cs, etc - relacionados com métodos de extensão Vector2)
- **Status**: Parcialmente compilável - erros de compatibilidade com extensões MonoGame requerem investigação

### Próximos Passos
- ETAPA 3: Implementar serviços de plataforma (IPlatformPaths, ILogSystem, etc.)
- ETAPA 4: Adaptar estrutura de Content.zip
- Essas interfaces podem desbloquear os builds após implementação

## ETAPA 3 - Serviços de Plataforma + Paths + LogSystem

### Ações Realizadas
1. **3.1** - Criadas interfaces de serviço:
   - `IPlatformPaths`: Abstração de caminhos (Desktop vs Android)
   - `ILogSystem`: Sistema de logging centralizado
   - `IAssetLocator`: Localização e validação de assets
   
2. **3.2** - Implementações concretas:
   - `DesktopPlatformPaths`: Paths para desenvolvimento
   - `LogSystemImpl`: Sistema de logs com estrutura por data
   - `AssetLocatorImpl`: Validação CheckContent + carregamento

3. **3.3** - Service Locator:
   - `ServiceLocator`: Centraliza injeção de dependências
   - Acessível globalmente via métodos estáticos

4. **3.4** - Content Manager customizado:
   - `ExternalFileContentManager`: Carrega XNBs do filesystem
   - Mantém compatibilidade com `Engine.Instance.Content.Load<T>()`
   
5. **3.5** - Integração FMOD:
   - Copiadas libs nativas: `libfmod.so` e `libfmodstudio.so` (arm64-v8a)
   - Adicionadas ao projeto Android via `NativeLibrary` ItemGroup
   - DllImport no FMOD.Studio já aponta para "fmod" e "fmodstudio"

6. **3.6** - GameActivity + CelesteGame:
   - `GameActivity.cs`: Activity Android que hospeda MonoGame
   - `CelesteGame.cs`: Classe Game que inicializa serviços e valida content
   - Fullscreen imersivo configurado
   - Landscape/Fullscreen garantido

### Arquivos Criados
- `src/Celeste.Core/Services/IPlatformPaths.cs`
- `src/Celeste.Core/Services/ILogSystem.cs`
- `src/Celeste.Core/Services/IAssetLocator.cs`
- `src/Celeste.Core/Services/DesktopPlatformPaths.cs`
- `src/Celeste.Core/Services/LogSystemImpl.cs`
- `src/Celeste.Core/Services/AssetLocatorImpl.cs`
- `src/Celeste.Core/Services/ServiceLocator.cs`
- `src/Celeste.Core/Services/ExternalFileContentManager.cs`
- `src/Celeste.Android/GameActivity.cs`
- `src/Celeste.Android/CelesteGame.cs`
- `src/Celeste.Android/libs/arm64-v8a/libfmod.so`
- `src/Celeste.Android/libs/arm64-v8a/libfmodstudio.so`

### Próximos Passos
- ETAPA 4: Adaptar Engine.cs para usar IPlatformPaths
- ETAPA 5: Criar estrutura Android (MainActivity, Flutter UI placeholder)
- ETAPA 6: Resolver erros de compilação (Vector2.Floor etc)