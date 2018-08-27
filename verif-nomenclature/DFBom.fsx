#r "../packages/Deedle/lib/net40/Deedle.dll"
#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#load "./Bom.fsx"


open System
open FSharp.Data
open Deedle

open Bom

let [<Literal>] basePath = __SOURCE_DIRECTORY__ + @"../../data/"

module BomData =
    open Deedle.Frame
    let [<Literal>] codeVentePath = basePath + "nomenclatures_code_ventes_actifs.csv"

    type BomData = CsvProvider<codeVentePath, Schema= Bom.CsvFile.schema,HasHeaders=true,Separators=";",Culture="fr-FR">
    type BomRow = BomData.Row
    
    let csvBomCV = BomData.Load(codeVentePath)

    let toObs (row: BomRow) = 
        {
            CodeProduit = row.CodeProduit
            VersionVariante = row.VersionVariante
            Evolution = row.Evolution
            Libelle = row.Libelle
            CodeFamilleLog = row.CodeFamilleLog
            Nature = row.Nature
            Quantite = row.Quantite
            CodeComposant = row.CodeComposant
            VersionComposant = row.VersionComposant
            QuantiteComposant = row.QuantiteComposant
            SousEnsemble = row.SousEnsemble
        }

    let obs = csvBomCV.Rows |> Seq.map toObs

    let df =  Deedle.Frame.ofRecords obs
        

    module Transforms = 
        let fillMissingWithBlank (colname: string) (df: Frame<'R, string>) = 
            let newSeries = 
                df
                |> Frame.getCol colname
                |> Series.fillMissingWith ""
            df |> Frame.replaceCol colname newSeries                        

    open Transforms

    let cleanDF : Frame<int,string> = df


let dfBom = 
    let cleanDF = BomData.cleanDF
    let pathBomClean = basePath + "dfbom.csv"

    cleanDF.SaveCsv(path=pathBomClean, separator=';')
    cleanDF
