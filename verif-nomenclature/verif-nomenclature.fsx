#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"

open FSharp.Data

let [<Literal>] nomPath = "../data/Nomenclatures.csv"

type Nomenclature = CsvProvider<nomPath, ";">

let nom = Nomenclature.Load(nomPath)

nom.Headers


nom.Rows
|> Seq.take 5
|> Seq.map(fun row -> row.``Code produit``)