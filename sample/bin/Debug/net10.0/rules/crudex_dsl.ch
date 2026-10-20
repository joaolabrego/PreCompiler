// Apenas as regras da DSL: não é necessário carregar std.ch.
#command SELECIONAR <cCampos,...> DA <cTabela> [WHERE <*cCondicao*>] => ;
    SELECT <cCampos> FROM <cTabela> [WHERE <cCondicao>]

#command CRIAR TABELA <cTabela> COLUNAS <*cColunas*> => ;
    CREATE TABLE <cTabela> ( <cColunas> )
