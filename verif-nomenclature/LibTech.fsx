namespace LibTech
open System

module LibTech = 

    module ColTypes = 
        let [<Literal>] natureT = "string"
        let [<Literal>] niveau4T = "string"
        let [<Literal>] libNiveau4T = "string"
        let [<Literal>] codeFamilleLogT = "string"
        let [<Literal>] codeProduitT = "string"
        let [<Literal>] libTechT = "string"

    let [<Literal>] nature = "Nature"
    let [<Literal>] niveau4 = "Niveau4"
    let [<Literal>] libNiveau4 = "LibelleNiveau4"
    let [<Literal>] codeFamilleLog = "Code Famille Logistique"
    let [<Literal>] codeProduit = "Code Produit"
    let [<Literal>] libTech = "Libellé technique"

    open ColTypes

    let [<Literal>] schema = 
        nature              + " (" + natureT            + "), "
        + niveau4           + " (" + niveau4T           + "), "
        + libNiveau4        + " (" + libNiveau4T        + "), "
        + codeFamilleLog    + " (" + codeFamilleLogT    + "), "
        + codeProduit       + " (" + codeProduitT       + "), "
        + libTech           + " (" + libTechT           + ")"
    
    let list = [
        nature; niveau4; 
        libNiveau4; codeFamilleLog; 
        codeProduit; libTech ] 
        

type Observation = {
    Nature : string
    Niveau4 : string
    LibNiveau4 : string
    CodeFamilleLog : string
    CodeProduit : string
    LibTech: string
}

    
