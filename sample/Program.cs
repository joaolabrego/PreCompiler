using HarbourCompiler;

var rules = """"
    // Apenas as regras da DSL: não é necessário carregar std.ch.
    #command SELECIONAR <cCampos,...> DA <cTabela> [WHERE <*cCondicao*>] => ;
        SELECT <cCampos> FROM <cTabela> [WHERE <cCondicao>]

    #command CRIAR TABELA <cTabela> COLUNAS <*cColunas*> => ;
        CREATE TABLE <cTabela> ( <cColunas> )

    #command ATUALIZAR <cTabela> COM <cAtribuicoes,...> WHERE <*cCondicao*> => ;
    UPDATE <cTabela> SET <cAtribuicoes> WHERE <cCondicao>

    #translate HOJE => CURRENT_DATE
    """";
using var compiler = new ClipperPreCompiler(AppContext.BaseDirectory);

var dslCode = """
    SELECIONAR id, nome, email DA tb_usuarios WHERE ativo = 1
    CRIAR TABELA produtos COLUNAS id int, preco decimal
    ATUALIZAR tb_utilizadores COM ultimo_acesso = HOJE, status = 1 WHERE id = 42
    """;

var compilado = compiler.Execute(dslCode, rules);

Console.WriteLine($"Pré-processador: {compiler.Version}");
Console.WriteLine();
Console.WriteLine("Regras: {0}", rules);
Console.WriteLine();
Console.WriteLine("Código a compilar: {0}", dslCode);
Console.WriteLine();
Console.WriteLine("Compilado: {0}", compilado);
Console.WriteLine();
Console.WriteLine("Pressione qualquer tecla para sair...");
Console.ReadKey();
