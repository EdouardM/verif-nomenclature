#load "./Bom.fsx"
#load "./DFBom.fsx"
#load "./DFLibTech.fsx"

open FSharp.Data

open Bom
open DFBom

open LibTech
open DFLibTech

open System.Text.RegularExpressions

open Deedle

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
    Frame.getCol InfoComposants.codeComposant df
    |> Series.values
    |> Seq.toList
    |> removeOption
    |> Set.ofList
    
let byBomId: Series<BomId, Frame<int, string>> =
    dfBom
    |> Frame.groupRowsUsing (fun _ c -> 
        { 
            CodeProduit = c.GetAs<string>(InfoProduit.codeProduit) 
            Variante = c.GetAs<string>(InfoProduit.versionVariante)
            Evolution = c.GetAs<string>(InfoProduit.evolution)
        } )
    |> Frame.nest        

let nomComponents: Series<BomId, Set<string>> = 
    Series.mapValues components byBomId

let extractCodes libelle = 
    let pattern = @"([0-9]{5}[0-9]{0,1})"  
    let ms = Regex.Matches(libelle,pattern)
    [ for m in ms -> m.Value ]
    |> Set.ofList

type LibStatus = 
    | WithCodes
    | WithoutCodes
    | Empty
    override x.ToString() = 
        match x with
        | Empty -> "Vide"
        | WithoutCodes -> "Sans code"
        | WithCodes -> "Avec code"

type LibAnalysis = {
    LibStatus : LibStatus
    Compos : Set<string>
}

let libComponents: Series<string, LibAnalysis> = 
    dfLibTech
    |> Frame.indexRows LibTech.codeProduit
    |> Frame.getCol LibTech.libTech
    |> Series.mapValues(fun lib -> 
        if System.String.IsNullOrEmpty(lib) 
        then { LibStatus = Empty; Compos = Set.empty }
        else 
            let compos = extractCodes lib
            if compos.IsEmpty 
            then { LibStatus = WithoutCodes; Compos = Set.empty }
            else { LibStatus = WithCodes; Compos = compos }
    )

libComponents.["24358"]

type DiffInfo =
    {
        LibStatus : LibStatus
        MissingCompos : Set<string>
    }
type ProdCompareResult = 
    | LibNotFound
    | LibOK
    | Diff of DiffInfo
    override x.ToString() =
        match x with
        | LibNotFound -> "Libelle non trouvé"
        | LibOk -> "Libelle OK"
        | Diff _ -> "Manque Composant"

//Returns the missing components in libComponents for every code in nomComponents
let compareComponents =
    nomComponents
    |> Series.map(fun bomId compos -> 
        let missing = 
            Series.tryGet bomId.CodeProduit libComponents
            |> Option.map(fun libCompos-> 
            //Compare the components in nomenclature and those in libelle
            {
                LibStatus = libCompos.LibStatus;
                MissingCompos = Set.difference compos libCompos.Compos
            } )
        match missing with
        | None -> LibNotFound
        | Some diffInfo -> 
            if diffInfo.MissingCompos.IsEmpty
            then LibOK
            else Diff diffInfo
    )
    
let distinctComponents = 
    compareComponents
    |> Series.filterValues( function | Diff _ -> true | _ -> false)    

let sameComponents =
    compareComponents
    |> Series.filterValues( function | LibOK -> true | _ -> false)

let libNotFound = 
    compareComponents
    |> Series.filterValues( function | LibNotFound -> true | _ -> false)

compareComponents
|> Series.countKeys

distinctComponents
|> Series.countKeys

sameComponents
|> Series.countKeys

libNotFound
|> Series.countKeys

type CompareOutput = {
    CodeProduit:string
    Version: string
    Evolution: string
    StatutLibelle: string
    Ecart : string
    CodeComposant : string
}
let dfCompareResults =
        compareComponents
        |> Series.observations
        |> Seq.collect(fun (bomId, result) -> 
            match result with
            | Diff diffInfo -> 
                diffInfo.MissingCompos
                |> Set.map(fun compo -> 
                    {
                        CodeProduit = bomId.CodeProduit
                        Version = bomId.Variante
                        Evolution = bomId.Evolution
                        Ecart = "Compo manquant"
                        StatutLibelle = diffInfo.LibStatus.ToString()
                        CodeComposant = compo
                    } )
                |> Set.toList                                
            | res ->
                [ {
                        CodeProduit = bomId.CodeProduit
                        Version = bomId.Variante
                        Evolution = bomId.Evolution
                        Ecart = res.ToString()
                        StatutLibelle = ""
                        CodeComposant = ""
                } ]          
        )
        |> Seq.mapi (fun i v -> i, v)
        |> series
        |> Frame.ofRecords
let outputPath = __SOURCE_DIRECTORY__ + "../../data/output.csv"

dfCompareResults.SaveCsv(path=outputPath, separator=';')
