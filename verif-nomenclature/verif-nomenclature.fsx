#load "./Bom.fsx"
open Deedle
#load "./DFBom.fsx"
#load "./DFLibTech.fsx"

open FSharp.Data

open Bom
open DFBom

open LibTech
open DFLibTech

open System.Text.RegularExpressions

open Deedle

type DF = Frame

let dfBom = dfBom
let dfLibTech = dfLibTech

type BomId = 
    {
        CodeProduit : string
        Variante : string
        Evolution : string
    }

let consOption list opt = 
    Option.fold(fun acc value -> value::acc) list opt 

let removeOption l = 
    List.fold(fun acc x -> consOption acc x) [] l
    
let components (df: Frame<int, string>) = 
    df.Columns
    |> Series.get InfoComposants.codeComposant
    |> Series.values
    |> Seq.toList
    |> removeOption
    
    // Frame.getCol InfoComposants.codeComposant df
    // |> Series.mapValues removeOption
    // |> Series.values
    // |> Set.ofSeq
// Series<BomId, Set<string list>> =
let byBomId: Series<BomId, ObjectSeries<int>> =
    dfBom
    |> Frame.groupRowsUsing (fun _ c -> 
        { 
            CodeProduit = c.GetAs<string>(InfoProduit.codeProduit) 
            Variante = c.GetAs<string>(InfoProduit.versionVariante)
            Evolution = c.GetAs<string>(InfoProduit.evolution)
        } )
    |> Frame.nest        
    |> Series.mapValues components

byBomId.Get {CodeProduit = "10057"; Variante = "1"; Evolution = "1" }

let getComponentSet: seq<NomRow> -> Set<string> =  
    Seq.map codeComposant
    >> Seq.toList
    >> removeOption
    >> Set.ofList


let getComponents (rows: seq<NomRow>) = 
    rows
    |> Seq.groupBy (fun row -> 
        {
            Code = row.``Code produit``;
            Version = row.``Version de la variante``
            Evolution = row.Evolution} )
    |> Seq.map(fun (code, compos) -> code, getComponentSet compos)

let nomComponents = getComponents nom.Rows


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
        
        if System.String.IsNullOrEmpty(lib) 
        then code, { LibStatus = Vide; Compos = Set.empty }
        else 
            let compos = getComponentsLib row
            if compos.IsEmpty 
            then code, { LibStatus = SansCodes; Compos = Set.empty }
            else code, { LibStatus = OK; Compos = compos }
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


let distinctComponents = 
    compareComponents
    |> Seq.filter(fun (_, results) -> 
        not results.MissingCompos.IsEmpty
    )

let sameComponents =
    compareComponents
    |> Seq.filter(fun (_, results) -> 
        results.MissingCompos.IsEmpty
    )

nomComponents
|> Seq.length

compareComponents
|> Seq.length

distinctComponents
|> Seq.length

sameComponents
|> Seq.length

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