using HarbourCompiler;

var rulesPath = Path.Combine(AppContext.BaseDirectory, "rules", "crudex_dsl.ch");
var rules = await File.ReadAllTextAsync(rulesPath);
var expected = "SELECT id, nome, email FROM tb_usuarios WHERE ativo = 1\n" +
               "CREATE TABLE produtos ( id int, preco decimal )";
var input = "SELECIONAR id, nome, email DA tb_usuarios WHERE ativo = 1\n" +
            "CRIAR TABELA produtos COLUNAS id int, preco decimal";

using (var compiler = new ClipperPreCompiler(AppContext.BaseDirectory))
{
    var actual = compiler.Execute(input, rules).Replace("\r\n", "\n").TrimEnd();
    if (actual != expected)
        throw new Exception($"Tradução inesperada:\n{actual}");

    Parallel.For(0, 24, _ =>
    {
        var repeated = compiler.Execute(input, rules).Replace("\r\n", "\n").TrimEnd();
        if (repeated != expected)
            throw new Exception("A chamada repetida retornou uma tradução diferente.");
    });
}

using (var compiler = new ClipperPreCompiler(AppContext.BaseDirectory))
{
    try
    {
        compiler.Execute("FALHA", "#command FALHA <=>\n");
        throw new Exception("Uma regra inválida foi aceita.");
    }
    catch (InvalidOperationException) when (!string.IsNullOrWhiteSpace(compiler.LastDiagnostics))
    {
        Console.WriteLine("Diagnóstico de sintaxe recebido.");
    }
}

Console.WriteLine("OK: tradução, chamadas concorrentes e diagnóstico de erro.");
