#r "../packages/Deedle/lib/net40/Deedle.dll"
#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#load "./LibTech.fsx"


open System
open FSharp.Data
open Deedle

open LibTech

let [<Literal>] basePath = __SOURCE_DIRECTORY__ + @"../../data/"

module LibTechData =
    open Deedle.Frame
    let [<Literal>] libTechPath = basePath + "codes_vente_actifs_libelle_technique.csv"

    type LibTechData = CsvProvider<libTechPath, Schema= LibTech.schema,HasHeaders=true,Separators=";",Culture="fr-FR">
    type BomRow = LibTechData.Row
    
    let csvBomCV = LibTechData.Load(libTechPath)

    let toObs (row: BomRow) = 
        {
            CodeProduit = row.``Code Produit``
            Nature = row.Nature
            Niveau4 = row.Niveau4
            LibNiveau4 = row.LibelleNiveau4
            CodeFamilleLog = row.``Code Famille Logistique``
            LibTech = row.``Libellé technique``
        }

    let obs = csvBomCV.Rows |> Seq.map toObs

    let df =  Deedle.Frame.ofRecords obs
    
    let cleanDF : Frame<int,string> = df


let dfLibTech = 
    let cleanDF = LibTechData.cleanDF
    let pathBomClean = basePath + "dfLibTech.csv"

    cleanDF.SaveCsv(path=pathBomClean, separator=';')
    cleanDF
