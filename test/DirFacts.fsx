﻿// Copyright (c) Stephen Tetley 2018,2019
// License: BSD 3 Clause

#r "netstandard"

#I @"C:\Users\stephen\.nuget\packages\FParsec\1.0.4-rc3\lib\netstandard1.6"
#r "FParsec"
#r "FParsecCS"

#I @"C:\Users\stephen\.nuget\packages\slformat\1.0.2-alpha-20190304\lib\netstandard2.0"
#r "SLFormat"


#load "..\src\FactX\Internal\Common.fs"
#load "..\src\FactX\Syntax.fs"
#load "..\src\FactX\FactOutput.fs"
#load "..\src\FactX\FactWriter.fs"
#load "..\src-extra\FactX\Extra\PathString.fs"
#load "..\src-extra\FactX\Extra\LabelledTree.fs"
#load "..\src-extra\FactX\Extra\DirectoryListing.fs"
open FactX
open FactX.FactWriter
open FactX.Extra.DirectoryListing
open System.IO

let getLocalDataFile (fileName:string) : string = 
    System.IO.Path.Combine(__SOURCE_DIRECTORY__,"../data", fileName)

let outputFile (fileName:string) : string = 
    System.IO.Path.Combine(__SOURCE_DIRECTORY__,"../data", fileName)


let test01 () = 
    let path1 = getLocalDataFile "dir.txt"
    match readDirRecurseOutput path1 with
    | Choice1Of2 err -> failwith err
    | Choice2Of2 ans -> printfn "%s" <| ans.ToString()




// Note - to make facts a "filestore" must have a name
// The obvious name is the <path-to-root>.

//let test02 () = 
//    let path1 = getLocalDataFile "dir.txt"
//    match readDirRecurseOutput path1 with
//    | Choice1Of2 err -> failwith err
//    | Choice2Of2 ans -> 
//        let fs:FactBase = fileStore ans in (fs.ToProlog()) |> printfn "%A" 
//        let fs:FactBase = drive ans in (fs.ToProlog()) |> printfn "%A" 

// SWI-Prolog has a pcre module which suggests representing paths
// as lists of strings might be useful.
let pathList (path:FilePath) : Term = 
    path.Split('\\') |> Array.toList |> List.map stringTerm |> listTerm


let writeListing (infile:string) (moduleName:string) (outPath:string) : unit =
    match listingToProlog infile with
    | None -> printfn "Could not interpret the directory listing: '%s'" infile
    | Some listing -> 
        let justfile = FileInfo(outPath).Name
        runFactWriter 160 outPath 
            <|  factWriter {
                do! tellComment justfile
                do! newlines 3
                do! tellDirective (moduleDirective moduleName ["listing/1"])
                do! newline
                do! tellPredicate (predicate "listing" [listing])
                do! newline
                return ()
            }


// We should consider generating SWI Prolog record accessors

let main (localFile:string) = 
    let infile = getLocalDataFile localFile
    let name1 = Path.GetFileName infile |> fun x -> Path.ChangeExtension(x,"pl")
    let outfile = outputFile name1
    writeListing infile "directories" outfile






