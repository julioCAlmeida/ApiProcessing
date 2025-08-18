namespace Processing

open System
open System.Text.Json
open System.Text.Json.Serialization

// Definição dos tipos usados no processamento
type InputItem = {
    trimestre: string
    nomeBandeira: string
    nomeFuncao: string
    produto: string
    qtdCartoesEmitidos: int
    qtdCartoesAtivos: int
    qtdTransacoesNacionais: int
    valorTransacoesNacionais: decimal
    qtdTransacoesInternacionais: int
    valorTransacoesInternacionais: decimal
}

type OutputItem = {
    trimestre: string
    nomeBandeira: string
    qtdCartoesEmitidos: int
    qtdCartoesAtivos: int
    qtdTransacoesNacionais: int
    valorTransacoesNacionais: decimal
}

// Função principal de pré-processamento
module PreProcessor =

    let processJson (jsonData: string) =
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true

        let input =
            JsonSerializer.Deserialize<InputItem[]>(jsonData, options)

        // Garante que não é nulo
        let inputData =
            if isNull input then [||] else input

        // Filtra apenas produto empresarial
        let corporativeData =
            inputData
            |> Array.filter (fun item -> item.produto = "Empresarial")

        // Agrupa por trimestre e bandeira
        let grouped =
            corporativeData
            |> Array.groupBy (fun item -> item.trimestre, item.nomeBandeira)
            |> Array.map (fun ((tri, bandeira), items) ->
                {
                    trimestre = tri
                    nomeBandeira = bandeira
                    qtdCartoesEmitidos = items |> Array.sumBy (fun x -> x.qtdCartoesEmitidos)
                    qtdCartoesAtivos = items |> Array.sumBy (fun x -> x.qtdCartoesAtivos)
                    qtdTransacoesNacionais = items |> Array.sumBy (fun x -> x.qtdTransacoesNacionais)
                    valorTransacoesNacionais = items |> Array.sumBy (fun x -> x.valorTransacoesNacionais)
                }
            )

        JsonSerializer.Serialize(grouped, options)


