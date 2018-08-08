#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"

open FSharp.Data

let [<Literal>] NomPath = "../data/Nomenclatures.csv"
let [<Literal>] NomSchema =
    "Code produit(string), Version de la variante(string), Evolution(string), \
    Libellé (string), Code Famille Logistique(int option), Nature du produit(string), \
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

let codeProduit (row: NomRow) = row.``Code produit``
let codeComposant (row: NomRow) = row.``Code Composant``


let consOption list opt = 
    Option.fold(fun acc value -> value::acc) list opt 
let removeOption l = List.fold(fun acc x -> consOption acc x) [] l
    
let getComponentSet: seq<NomRow> -> Set<string> =  
    Seq.map codeComposant
    >> Seq.toList
    >> removeOption
    >> Set.ofList

type BomIdentifier = 
    {
        Code : string
        Version : string
        Evolution : string
    }

let getComponents (rows: seq<NomRow>) = 
    rows
    |> Seq.groupBy (fun row -> 
        {
            Code = row.``Code produit``;
            Version = row.``Version de la variante``
            Evolution = row.Evolution} )
    |> Seq.map(fun (code, compos) -> code, getComponentSet compos)

let nomComponents = getComponents nom.Rows

open System.Text.RegularExpressions

let [<Literal>] LibellePath = "../data/codes_vente_actifs_libelle_technique.csv"
let [<Literal>] LibSchema = 
    "Code de la nature produit (string), Code niveau 4(string), \
    Libellé niveau 4(string), Code de la famille logistique(int option), Code du produit(string), \
    Libelle technique(string)"


type Libelle = CsvProvider<LibellePath, ";", Schema = LibSchema>
type LibRow = Libelle.Row

let codeDuProduit (row: LibRow) = row.``Code du produit``
let libelletechnique (row: LibRow) = row.``Libelle technique``

let lib = Libelle.Load(LibellePath)
let extractCodes libelle = 
    let pattern = @"([0-9]{5}[0-9]{0,1})"  
    let ms = Regex.Matches(libelle,pattern)
    [ for m in ms -> m.Value ]

let getComponentsLib = 
    libelletechnique 
    >> extractCodes
    >> Set.ofList

type LibStatus = 
    | OK
    | SansCodes
    | Vide
    override x.ToString() = 
        match x with
        | Vide -> "Vide"
        | SansCodes -> "Sans code"
        | OK -> "OK"

type LibAnalysis = {
    LibStatus : LibStatus
    Compos : Set<string>
}

let getLibComponents (rows: seq<LibRow>) = 
    rows 
    |> Seq.map(fun row -> 
        let code = codeDuProduit row
        let lib = libelletechnique row
        if System.String.IsNullOrEmpty(lib) then
            code, { LibStatus = Vide; Compos = Set.empty }
        else 
            let compos = getComponentsLib row
            if compos.IsEmpty then
                code, { LibStatus = SansCodes; Compos = Set.empty }
            else
                code, { LibStatus = OK; Compos = compos }
    )

let libComponents = 
    getLibComponents lib.Rows
    |> Map.ofSeq

libComponents.["10222"]

type CompareResults =
    {
        LibStatus : LibStatus
        MissingCompos : Set<string>
    }

//Returns the missing components in libComponents for every code in nomComponents
let compareComponents =
    nomComponents
    |> Seq.choose (fun (bomId, compos) -> 
        let missing = 
            Map.tryFind bomId.Code libComponents
            |> Option.map(fun libCompos-> 
                //Compare the components in nomenclature and those in libelle
                {
                    LibStatus = libCompos.LibStatus;
                    MissingCompos = Set.difference compos libCompos.Compos
                } )
        match missing with
        | None -> None
        | Some set -> Some (bomId, set)
        )

let collectMissing (results: seq<BomIdentifier * CompareResults>) =
        Seq.collect (fun (code, result) -> 
            Set.map(fun c-> code, result.LibStatus.ToString(), c) result.MissingCompos ) results

let formatMissing (missing: seq<BomIdentifier * string * string>) = 
    Seq.map (fun (bomId, b, c) -> sprintf "%s;%s;%s;%s;%s" bomId.Code bomId.Version bomId.Evolution b c) missing

    
open System.IO

let outputPath = __SOURCE_DIRECTORY__ + "../../data/output.csv"

let writeFile path (content: seq<string>)= 
    use wr = new StreamWriter(path, true)
    
    content
    |> Seq.iter wr.WriteLine


compareComponents
|> collectMissing
|> formatMissing
|> writeFile outputPath