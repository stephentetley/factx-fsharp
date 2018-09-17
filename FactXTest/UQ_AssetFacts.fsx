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
#load "..\FactX\FactX\FactOutput.fs"
#load "..\FactX\FactX\Extra\ExcelProviderHelper.fs"
open FactX
open FactX.Extra.ExcelProviderHelper

#load @"PropUtils.fs"
open PropUtils


let outputFile (filename:string) : string = 
    System.IO.Path.Combine(@"D:\coding\prolog\asset\facts", filename) 


type AssetTable = 
    ExcelFile< @"G:\work\Projects\uquart\site-data\AssetDB\adb-site-sample.xlsx",
               ForceString = true >

type AssetRow = AssetTable.Row

let readAssetSpeadsheet (sourcePath:string) : AssetRow list = 
    let helper = 
        { new IExcelProviderHelper<AssetTable,AssetRow>
          with member this.ReadTableRows table = table.Data 
               member this.IsBlankRow row = match row.GetValue(0) with null -> true | _ -> false }
         
    excelReadRowsAsList helper (new AssetTable(sourcePath))


let equipmentBody (row:AssetRow) : Option<Value list> = 
    match row.``Common Name`` with
    | null -> None
    | cname ->
        Some [ PQuotedAtom      <| row.Reference
             ; PQuotedAtom      <| installationNameFromPath row.``Common Name`` 
             ; PQuotedAtom      <| row.``Common Name`` 
             ; PQuotedAtom      <| row.AssetStatus ]

let genUltrasonicInsts (allRows:AssetRow list) : unit = 
    let outFile = outputFile "adb_ultrasonic_insts.pl"
    
    let helper : IFactHelper<AssetRow> = 
        { new IFactHelper<AssetRow> with
            member this.Signature = "adb_ultrasonic_inst(uid, site_name, path, op_status)."
            member this.ClauseBody row = equipmentBody row }
              
    
    let ultrasonics = 
        List.filter (fun (row:AssetRow) -> isLevelControlAdb row.``Common Name``) allRows

    let facts : FactSet = ultrasonics |> makeFactSet helper

    let pmodule : Module = 
        new Module("adb_ultrasonic_insts", "adb_ultrasonic_insts.pl", facts)

    pmodule.Save(outFile)


let genFlowMeters (allRows:AssetRow list) : unit = 
    let outFile = outputFile "adb_flow_meters.pl"

    let helper : IFactHelper<AssetRow> = 
        { new IFactHelper<AssetRow> with
            member this.Signature = "adb_flow_meter(uid, site_name, path, op_status)."
            member this.ClauseBody row = equipmentBody row }
            
    let flowMeters = 
        List.filter (fun (row:AssetRow) -> isFlowMeterAdb row.``Common Name``) allRows

    let facts : FactSet = flowMeters |> makeFactSet helper

    let pmodule : Module = 
        new Module("adb_flow_meters", "adb_flow_meters.pl", facts)

    pmodule.Save(outFile)

let genPressureInsts (allRows:AssetRow list) : unit = 
    let outFile = outputFile "adb_pressure_insts.pl"

    let helper : IFactHelper<AssetRow> = 
        { new IFactHelper<AssetRow> with
            member this.Signature = "adb_pressure_inst(uid, site_name, path, op_status)."
            member this.ClauseBody row = equipmentBody row }
            
    let pressureInsts = 
        List.filter (fun (row:AssetRow) -> isPressureInstAdb row.``Common Name``) allRows

    let facts : FactSet = pressureInsts |> makeFactSet helper
    
    let pmodule : Module = 
        new Module ("adb_pressure_insts", "adb_pressure_insts.pl", facts)

    pmodule.Save(outFile)


let genDissolvedOxygenInsts (allRows:AssetRow list) : unit = 
    let outFile = outputFile "adb_dissolved_oxygen_insts.pl"
    
    let helper : IFactHelper<AssetRow> = 
        { new IFactHelper<AssetRow> with
            member this.Signature = "adb_dissolved_oxygen_inst(uid, site_name, path, op_status)."
            member this.ClauseBody row = equipmentBody row }
            
    let doxyInsts = 
        List.filter (fun (row:AssetRow) -> isDissolvedOxygenInstAdb row.``Common Name``) allRows

    let facts : FactSet = doxyInsts |> makeFactSet helper

    let pmodule : Module = 
        new Module("adb_dissolved_oxygen_insts", "adb_dissolved_oxygen_insts.pl", facts)


    pmodule.Save(outFile)

// *************************************
// Installation facts

let getInstallations (rows:AssetRow list) : string list = 
    let step (ac:Set<string>) (row:AssetRow) : Set<string> = 
        let instName = installationNameFromPath row.``Common Name``
        if not (ac.Contains instName) then 
            ac.Add instName
        else ac
    
    List.fold step Set.empty rows 
        |> Set.toList

let genInstallationFacts (allRows:AssetRow list) : unit = 
    let outFile = outputFile "adb_installations.pl"

    let helper : IFactHelper<string> = 
        { new IFactHelper<string> with
            member this.Signature = "adb_installation(installation_name)."
            member this.ClauseBody name = Some [ PQuotedAtom name ] }
     
    let facts : FactSet =  getInstallations allRows |> makeFactSet helper

    let pmodule : Module = 
        new Module("adb_installations", "adb_installations.pl", facts)

    pmodule.Save(outFile)

let main () = 
    let allAssetFiles = getFilesMatching @"G:\work\Projects\uquart\site-data\AssetDB" "AI*.xlsx"
    let allRows = 
        allAssetFiles |> List.map readAssetSpeadsheet |> List.concat
    
    genUltrasonicInsts allRows
    genFlowMeters allRows
    genPressureInsts allRows
    genDissolvedOxygenInsts allRows
    genInstallationFacts allRows


// ** TEMP ** 



    
type SimpleTable = 
    CsvProvider< Schema = "Site Name (string),Instrument Type(string),AI2 Asset Ref(string),Common Name(string)",
                 HasHeaders = false >

type SimpleRow = SimpleTable.Row

let makeSimpleRow (instType:string) (row:AssetRow) : SimpleRow = 
    SimpleTable.Row( siteName = installationNameFromPath row.``Common Name``
                   , instrumentType = instType
                   , ai2AssetRef = row.Reference
                   , commonName = row.``Common Name`` )

let genCsv (inputFile:string) : unit = 
    let outputFile : string = 
        let name1 = System.IO.Path.GetFileName(inputFile)
        let name2 = System.IO.Path.Combine(@"G:\work\Projects\uquart\output", name1) 
        System.IO.Path.ChangeExtension(name2, "csv")

    let xlsxRows = readAssetSpeadsheet inputFile

    let ultrasonics = 
        List.filter (fun (row:AssetRow) -> isLevelControlAdb row.``Common Name``) xlsxRows
            |> List.map (makeSimpleRow "ULTRASONIC")
    
    let flowInsts = 
        List.filter (fun (row:AssetRow) -> isFlowMeterAdb row.``Common Name``) xlsxRows
            |> List.map (makeSimpleRow "FLOW METER")

    let pressureInsts = 
        List.filter (fun (row:AssetRow) -> isPressureInstAdb row.``Common Name``) xlsxRows
            |> List.map (makeSimpleRow "PRESSURE INST")

    let doInsts = 
        List.filter (fun (row:AssetRow) -> isDissolvedOxygenInstAdb row.``Common Name``) xlsxRows
            |> List.map (makeSimpleRow "DISSOLVED OXYGEN")

    let table = new SimpleTable(ultrasonics @ flowInsts @ pressureInsts @ doInsts) 
    use sw = new System.IO.StreamWriter(outputFile)
    sw.WriteLine "Site Name,Instrument Type,AI2 Asset Ref,Common Name"
    table.Save(writer = sw, separator = ',', quote = '"' )

    

let temp01 () = 
    let allAssetFiles = getFilesMatching @"G:\work\Projects\uquart\site-data\AssetDB" "AI*.xlsx"

    List.iter genCsv allAssetFiles

