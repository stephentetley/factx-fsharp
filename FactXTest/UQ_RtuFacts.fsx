﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause
#I @"..\packages\FParsec.1.0.4-RC3\lib\portable-net45+win8+wp8+wpa81"
#r "FParsec"
#r "FParsecCS"

#I @"..\packages\ExcelProvider.1.0.1\lib\net45"
#r "ExcelProvider.Runtime.dll"

#I @"..\packages\ExcelProvider.1.0.1\typeproviders\fsharp41\net45"
#r "ExcelDataReader.DataSet.dll"
#r "ExcelDataReader.dll"
#r "ExcelProvider.DesignTime.dll"
open FSharp.Interop.Excel

#I @"..\packages\FSharp.Data.3.0.0-beta3\lib\net45"
#r @"FSharp.Data.dll"
open FSharp.Data


open System.IO 

#load "..\FactX\FactX\Internal\FormatCombinators.fs"
#load "..\FactX\FactX\Internal\PrologSyntax.fs"
#load "..\FactX\FactX\FactOutput.fs"
#load "..\FactX\FactX\Extra\ExcelProviderHelper.fs"
open FactX
open FactX.Extra.ExcelProviderHelper

#load @"PropUtils.fs"
open PropUtils


let outputFile (filename:string) : string = 
    System.IO.Path.Combine(@"D:\coding\prolog\rts\facts", filename) 


// *************************************
// Mimic facts


type MimicTable = 
    ExcelFile< FileName = @"G:\work\Projects\uquart\site-data\RTS\rts-mimic-list.xlsx",
                SheetName = "Sheet1",
                ForceString = true >

type MimicRow = MimicTable.Row


let readMimicRows () : MimicRow list = 
    let helper = 
        { new IExcelProviderHelper<MimicTable,MimicRow>
          with member this.ReadTableRows table = table.Data 
               member this.IsBlankRow row = match row.GetValue(0) with null -> true | _ -> false }
         
    excelReadRowsAsList helper (new MimicTable())


let genMimicNameFacts (rows:MimicRow list) : unit = 
    let outFile = outputFile "rts_mimic_names.pl"

    let mimicNameClause (row:MimicRow) : option<Clause> =
        Clause.optionCons( signature = "rts_mimic_name(mimic_id, mimic_name)."
                         , body = [ optPrologSymbol     row.``Mimic ID``
                                  ; optPrologString     row.Name ]) 


    let facts : FactBase = 
        readMimicRows () |> List.map mimicNameClause |> FactBase.ofOptionList
    
    let pmodule : Module = 
        new Module ("rts_mimic_names", "rts_mimic_names.pl", facts)
    
    pmodule.Save(outFile)


    
// *************************************
// Points facts

    
type PointsTable = 
    CsvProvider< Sample = @"G:\work\Projects\uquart\site-data\RTS\points-sample.csv",
                 HasHeaders = true,
                 IgnoreErrors = true >

type PointsRow = PointsTable.Row

let readPoints (sourcePath:string) : PointsRow list = 
    let sheet = PointsTable.Load(uri=sourcePath)
    sheet.Rows |> Seq.toList



let genMimicPoints (rows:PointsRow list) : unit = 
    let outFile = outputFile "rts_mimic_points.pl"

    let mimicPointClause (row:PointsRow) : option<Clause> = 
        Clause.optionCons( signature = "rts_mimic_point(picture, os_name, point_name)."
                         , body = [ optPrologSymbol row.``Ctrl pic  Alarm pic``
                                  ; optPrologSymbol (getOsName row.``OS\Point name``)
                                  ; optPrologSymbol (getPointName row.``OS\Point name``)] )

    let facts : FactBase = 
        rows |> List.map mimicPointClause |> FactBase.ofOptionList

    let pmodule : Module = 
        let db = [facts]
        new Module ("rts_mimic_points", "rts_mimic_points.pl", facts) 

    pmodule.Save(outFile)





        
// *************************************
// asset_to_signal

// signal is a suffix, one of _P,_R,_F,_A

type AssetToSignal = 
    { OsName: string
      AssetName: string
      PointName: string
      SignalSuffix: string }

let optAssetToSignal (row:PointsRow) : option<AssetToSignal> = 
    if hasSuffixAFPR row.``OS\Point name`` then
        Some <| 
            let pointName = getPointName row.``OS\Point name``
            { OsName = getOsName row.``OS\Point name``;
              AssetName = uptoSuffix '_' pointName;
              PointName = pointName;
              SignalSuffix = suffixOf '_' pointName }
    else None

let getAssetToSignals (rows:PointsRow list) : AssetToSignal list = 
    List.choose id <| List.map optAssetToSignal rows


let genAssetToSignals (source:AssetToSignal list) : unit = 
    let outFile = outputFile "rts_asset_to_signal.pl"

    let assetSignalClause (row:AssetToSignal) : option<Clause> = 
        Clause.optionCons( signature = "asset_to_signal(os_name, asset_name, signal_name, suffix)." 
                         , body =  [ optPrologSymbol row.OsName
                                   ; optPrologString row.AssetName
                                   ; optPrologSymbol row.PointName
                                   ; optPrologSymbol row.SignalSuffix ] )
                

    let facts : FactBase = 
        source |> List.map assetSignalClause |> FactBase.ofOptionList
    
    let pmodule : Module = 
        new Module ("rts_asset_to_signal", "rts_asset_to_signal.pl", facts) 

    pmodule.Save(outFile)



/// Name include Outstation:
/// "THORNTON_DALE_STW   \INLET_BRUSH_SCREEN_R" => "THORNTON_DALE_STW   \INLET_BRUSH_SCREEN"

/// The map maps stem to its suffixes.
type StemPoints = Map<string,string list>

let getStemPoints (rowMatch:PointsRow -> bool) (rows:PointsRow list) : StemPoints = 
    let oper (ac:StemPoints) (row:PointsRow) : StemPoints = 
        if rowMatch row && hasSuffixAFPR row.``OS\Point name`` then
            let rootName    = uptoSuffix '_' row.``OS\Point name``
            let suffix      = suffixOf '_' row.``OS\Point name``
            match Map.tryFind rootName ac with
            | Some xs -> Map.add rootName (suffix::xs) ac
            | None -> Map.add rootName [suffix] ac
        else ac
    List.fold oper Map.empty rows

// *************************************
// Pump facts

let getPumpPoints (rows:PointsRow list) : StemPoints = 
    let matcher (row:PointsRow) : bool = isPumpRtu (getPointName row.``OS\Point name``)
    getStemPoints matcher rows
    

let genPumpFacts (pumpPoints:StemPoints) : unit = 
    let outFile = outputFile "rts_pump_facts.pl"
    
    let pumps = Map.toList pumpPoints
    
    let pumpPointsClause (qualName:string, pointCodes:string list) : option<Clause> = 
        Clause.optionCons( signature = "rts_pump(osname, pump_name, point_codes)."
                        , body = [ optPrologSymbol  <| getOsName qualName
                                 ; optPrologSymbol  <| getPointName qualName
                                 ; optPrologList    <| List.map optPrologSymbol pointCodes ] )
        
    let facts : FactBase = 
        pumps |> List.map pumpPointsClause |> FactBase.ofOptionList
    
    let pmodule : Module = 
        new Module ("rts_pump_facts", "rts_pump_facts.pl", facts) 

    pmodule.Save(outFile)





// *************************************
// Screen facts

let getScreenPoints (rows:PointsRow list) : StemPoints = 
    let matcher (row:PointsRow) : bool = isScreenRtu (getPointName row.``OS\Point name``)
    getStemPoints matcher rows


let genScreenFacts (screenPoints:StemPoints) : unit = 
    let outFile = outputFile "rts_screen_facts.pl"

    let screens = Map.toList screenPoints

    let screenPointsClause (qualName: string, pointCodes:string list) : option<Clause> = 
        Clause.optionCons( signature = "rts_screen(os_name, screen_name, point_codes)."
                         , body = [ optPrologSymbol <| getOsName qualName
                                  ; optPrologSymbol <| getPointName qualName
                                  ; optPrologList   <| List.map optPrologSymbol pointCodes ])

    let facts : FactBase = 
        screens |> List.map screenPointsClause |> FactBase.ofOptionList
    
    let pmodule : Module = 
        new Module ("rts_screen_facts", "rts_screen_facts.pl", facts)

    pmodule.Save(outFile)



// *************************************
// Outstation facts

let getOutstations (rows:PointsRow list) : string list = 
    let step (ac:Set<string>) (row:PointsRow) : Set<string> = 
        let osName = getOsName row.``OS\Point name``
        if not (ac.Contains osName) then 
            ac.Add osName
        else ac
    
    List.fold step Set.empty rows 
        |> Set.toList


let genOutstationFacts (allRows:PointsRow list) : unit = 
    let outFile = outputFile "rts_outstations.pl"

    let outstationClause (name:string) : option<Clause> = 
        Clause.optionCons( signature = "rts_outstation(os_name)."
                         , body = [ prologSymbol name ] )
        
    let facts : FactBase = 
        getOutstations allRows |> List.map outstationClause |> FactBase.ofOptionList

    let pmodule : Module = 
        new Module ("rts_outstations", "rts_outstations.pl", facts) 

    pmodule.Save(outFile)



// *************************************
// Main

let main () : unit = 
     readMimicRows () |> genMimicNameFacts

     let allPointsFiles = getFilesMatching @"G:\work\Projects\uquart\site-data\RTS" "*-rtu-points.csv"
     let allPoints = 
        List.map readPoints allPointsFiles |> List.concat

     allPoints |> genMimicPoints
     allPoints |> genOutstationFacts

     // Pumps pump/3
     allPoints |> getPumpPoints |> genPumpFacts
     allPoints |> getScreenPoints |> genScreenFacts
     allPoints |> getAssetToSignals |> genAssetToSignals

