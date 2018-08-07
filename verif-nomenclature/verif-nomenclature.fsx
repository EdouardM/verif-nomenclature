#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"

open FSharp.Data

let [<Literal>] NomPath = "../data/Nomenclatures.csv"
let [<Literal>] NomSchema =
    "Code produit(string), Version de la variante(string), Evolution(string), \
    Libellé (string), Code Famille Logistique(int), Nature du produit(string), \
    Quantité(int), Code Composant(string option),Version(string option), \
    Quantité composant(int option), Sous ensemble(string)"

type Nomenclature = CsvProvider<NomPath, ";", Schema = NomSchema>

type NomRow = Nomenclature.Row

let nom = Nomenclature.Load(NomPath)

let codeWithCompo: seq<NomRow> = 
    query {
        for row in nom.Rows do
        where (row.``Code Composant`` <> None)
        select row
    }

let extractComposant (rows: seq<NomRow>) = 
    rows
    |> Seq.groupBy (fun row -> row.``Code produit``)
    |> Seq.map(fun (code, rows) ->  
        code, rows |> Seq.map (fun row -> row.``Code Composant``) )

let codecompo = extractComposant nom.Rows

let [<Literal>] LibellePath = "../data/codes_vente_actifs_libelle_technique.csv"
let [<Literal>] LibSchema = 
    "Code de la nature produit (string), Code niveau 4(string), \
    Libellé niveau 4(string), Code de la famille logistique(int), Code du produit(string), \
    Libelle technique(string)"


type LibelleTech = CsvProvider<LibellePath, ";", Schema = LibSchema>

let lib = LibelleTech.Load(LibellePath)

lib.Headers

lib.Rows |> Seq.head

open System.Text.RegularExpressions

let pattern = @"([0-9]{5})"

let extractCodes libelle = 
    let ms = Regex.Matches(libelle, pattern )
    Seq.cast ms.GetEnumerator()

lib.Rows 
|> Seq.item 4
|> (fun row -> row.``Libelle technique`` )
|> extractCodes

