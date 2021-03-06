﻿// Copyright (c) Stephen Tetley 2018,2019
// License: BSD 3 Clause


#I @"C:\Users\stephen\.nuget\packages\slformat\1.0.2-alpha-20190721\lib\netstandard2.0"
#r "SLFormat"
open SLFormat.Pretty


#load "..\src\FactX\Internal\Common.fs"
#load "..\src\FactX\Syntax.fs"
#load "..\src\FactX\FactOutput.fs"
#load "..\src\FactX\Pretty.fs"
#load "..\src\FactX\FactWriter.fs"
open FactX
open FactX.FactWriter

let outputFileName (filename:string) : string = 
    System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../data/", filename) 


let demo01 () = 
    let outPath = outputFileName "dummy_writer.pl"
    runFactWriter 160 outPath 
        <|  factWriter {
            do! tellComment "dummy_writer.pl"
            do! newlines 3
            do! tellDirective (moduleDirective "directories" ["listing/1"])
            do! newline
            return ()
        }
