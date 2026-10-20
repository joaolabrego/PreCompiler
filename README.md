# Harbour Preprocessor nativo para C#

Este pacote expõe o pré-processador oficial do Harbour por uma DLL nativa e uma classe C#. O texto-fonte, as regras `.ch` e o resultado ficam em memória: o adaptador não inicia `hbpp.exe` nem cria arquivos temporários. Ele apenas pré-processa; o SQL gerado não é executado nem validado por um SGBD.

## Uso no C#

Copie `ClipperPreCompiler.cs` e a biblioteca nativa correspondente ao sistema operacional para a saída do projeto. O texto das regras `.ch` é passado diretamente à chamada `Execute`; pode ser carregado de um arquivo ou montado em memória.

```csharp
string rules = File.ReadAllText("crudex_dsl.ch");
using var compiler = new ClipperPreCompiler(AppContext.BaseDirectory);

string expandedCode = compiler.Execute(dslCode, rules);
Console.WriteLine(expandedCode);
```

No Windows x64, a classe procura `HarbourPreprocessor.dll` junto ao executável. No Linux x64, procura `libHarbourPreprocessor.so`. Como `rules` é uma string, as mesmas regras podem ser reutilizadas em chamadas sucessivas ou substituídas sem recriar a instância.

## Executar o exemplo

Com o .NET 10 SDK instalado:

```sh
cd sample
dotnet run
```

O código exemplo traduz:

```text
SELECIONAR id, nome, email DA tb_usuarios WHERE ativo = 1
CRIAR TABELA produtos COLUNAS id int, preco decimal
```

para:

```sql
SELECT id, nome, email FROM tb_usuarios WHERE ativo = 1
CREATE TABLE produtos ( id int, preco decimal )
```

## Compilação da biblioteca nativa

O projeto Visual C++ `native/HarbourPreprocessor.vcxproj` gera a DLL Windows x64 em `sample/HarbourPreprocessor.dll`. Antes de cada build C# no Windows, o projeto de exemplo executa `Rebuild` para o alvo nativo `Release|x64`; em seguida copia a DLL recém-gerada para a pasta de saída onde o adaptador a carrega. O DLL usa a biblioteca C estática do Visual C++, evitando depender do runtime redistribuível. Também é possível abrir o projeto Visual C++ diretamente. As fontes Harbour estão incluídas em `vendor/harbour/`.

`build-native.sh` também recompila a biblioteca a partir desse subconjunto de fontes:

```sh
# Linux x64
./build-native.sh linux-x64

# Windows x64, opcionalmente no terminal MINGW64 do MSYS2
CC=gcc ./build-native.sh win-x64
```

O pacote inclui a biblioteca Linux x64 compilada e o projeto nativo Visual Studio para gerar a DLL Windows. A compilação da DLL Windows precisa ser feita no Visual Studio instalado no seu Windows; não consegui executar essa etapa neste ambiente Linux.

## API nativa e diagnósticos

O header `native/harbour_preprocessor.h` define a ABI C. `hbpp_process` recebe as regras e o fonte como UTF-8 na memória, retorna a saída e eventuais diagnósticos, e `hbpp_free_buffer` libera os buffers. A classe C# envolve essa ABI, serializa chamadas por instância e libera os buffers mesmo se houver erro. O núcleo também protege o estado interno do Harbour durante o processamento.

Erros de sintaxe nas regras ou no texto processado geram `InvalidOperationException`; os detalhes ficam em `LastDiagnostics`. Use `Dispose` (ou `using`) para liberar o módulo nativo quando terminar.

## Fontes e licença

O subconjunto do Harbour vem do projeto oficial, identificado em `vendor/harbour/ORIGIN.txt`, e mantém seus avisos de copyright e licença junto aos arquivos. O wrapper e o adaptador são código novo deste pacote. Consulte os avisos incluídos antes de redistribuir o conjunto.
# PreCompiler
