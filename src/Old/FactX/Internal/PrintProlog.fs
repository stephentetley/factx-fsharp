﻿// Copyright (c) Stephen Tetley 2018,2019
// License: BSD 3 Clause

namespace Old.FactX.Internal.PrintProlog




[<AutoOpen>]
module PrintProlog = 
    
    // Indent-level of 4 seems good in Prolog.
    open SLFormat.Pretty

    let commaSep (docs:Doc list) = foldDocs (fun ac e -> ac ^^ comma ^/^ e) docs
    // let commaSepV (docs:Doc list) = foldDocs (fun ac e -> ac ^@@^ comma ^/^ e) docs

    /// Print vertically
    let prologList (docs:Doc list) : Doc = 
        enclose lbracket rbracket  <| foldDocs (fun x y -> x ^^ comma ^@@^ y) docs

    // TODO Not sure this is right.
    let private escapeSpecial (source:string) : string = 
        let s1 = source.Replace("\\" , "\\\\")
        let s2 = s1.Replace("'", "\\'")
        s2

    let simpleAtom (value:string) : Doc = text value

    // This must escape.
    let quotedAtom (value:string) : Doc = 
        text <| sprintf "'%s'" (escapeSpecial value)

    let prologString (value:string) : Doc = 
        text <| sprintf "\"%s\"" (escapeSpecial value)

    let prologChar (value:char) : Doc =  text <| sprintf "0'%c" value

    let prologBool (value:bool) : Doc = 
        text <| if value then "true" else "false"

    let prologInt (i:int64) : Doc = 
       text <| i.ToString()


    let prologFloat (d:float) : Doc = 
        text <| d.ToString()

    let prologDouble (d:double) : Doc = 
        text <| d.ToString()
    
    let prologDecimal (d:decimal) : Doc = 
        // Ensure Prolog printing renders to a decimal string.
        text <| let d1 = 0.0M + d in d1.ToString()



    let prologComment (comment:string) : Doc = 
        let lines = comment.Split [|'\n'|] |> Array.toList
        vcat <| List.map (fun s -> text (sprintf "%c %s" '%' s)) lines

    let prologFunctor (head:string) (body:Doc list) : Doc =
        nest 4 (text (escapeSpecial head) ^^ lparen ^//^ commaSep body ^^ rparen)

    /// Must be no space between head and open-paren            
    let prologFact (head:string) (body:Doc list) : Doc =
        prologFunctor head body ^^ dot




    /// E.g:
    ///     :- module(installation,
    ///               [installation/3]).
    ///
    let moduleDirective (moduleName:string) (exports: (string * int) list) : Doc = 
        let exportList : Doc = 
            let factNames = List.map (fun (s,i) -> text (sprintf "%s/%i" s i)) exports
            prologList factNames                
        nest 8 (text ":-" ^+^ text "module" ^^ parens (text moduleName ^^ comma ^/^ exportList) ^^ dot)

