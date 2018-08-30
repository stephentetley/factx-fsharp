﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause

namespace FactX

open System
open System.IO

open FactX.Internal.FormatCombinators


[<AutoOpen>]
module FactOutput = 

    type Identifier = string

    /// Note - Sequences/tuples not represented (should they be?)
    type Value = 
        | PString of string
        | PInt of int
        | PDouble of double
        | PQuotedAtom of string
        | PList of Value list
        member v.Format = 
            match v with
            | PString s -> prologString s
            | PInt i -> formatInt i
            | PDouble d -> formatDouble d
            | PQuotedAtom s -> quotedAtom s
            | PList vs -> prologList (List.map (fun (x:Value) -> x.Format) vs)

    type Clause = 
        { FactName: Identifier
          Values : Value list }
        member v.Format = 
            prologFact (simpleAtom v.FactName) 
                        (List.map (fun (x:Value) -> x.Format) v.Values)

    type IFactHelper<'a> = 
        abstract FactName : string
        abstract FactSignature : string
        abstract Arity : int
        abstract ClauseBody : 'a -> Value list

    type FactSet = 
        { FactName: Identifier 
          Arity: int
          Signature: string
          Comment: string
          Clauses: Clause list }
        member v.Format = 
            let d1 = prologComment v.Signature
            let d2 = prologComment v.Comment
            let ds = List.map (fun (clause:Clause) -> clause.Format) v.Clauses
            vcat <| (d1 :: d2 :: ds)

        member v.ExportSignature = (v.FactName, v.Arity)
    
    let makeFactSet (helper:IFactHelper<'a>) (items:seq<'a>) : FactSet = 
        let makeClause (item:'a) : Clause  = 
            { FactName = helper.FactName
              Values = helper.ClauseBody item }
        { FactName  = helper.FactName
          Arity     = helper.Arity 
          Signature = helper.FactSignature 
          Comment   = "" 
          Clauses   = Seq.toList items |> List.map makeClause
        }
        


    /// Potentially this should be an object, not a record.
    type Module = 
        { ModuleName: Identifier
          GlobalComment: string
          Exports: (Identifier * int) list
          Database: FactSet list }
        member v.Format = 
            let d1 = prologComment v.GlobalComment
            let d2 = moduleDirective v.ModuleName v.Exports
            let ds = List.map (fun (col:FactSet) -> col.Format) v.Database
            vcat <| (d1 :: empty :: d2 :: empty :: ds)

        member v.SaveToString () : string = 
            render v.Format
        
        member v.Save(filePath:string) = 
            use sw = new System.IO.StreamWriter(filePath)
            sw.Write (render v.Format)

        member v.AddFacts(facts:FactSet) = 
            { v with 
                Exports = (facts.ExportSignature :: v.Exports) ; 
                Database = (facts :: v.Database) }

    let makeModule (name:string) (comment:string) : Module = 
        { ModuleName = name
          GlobalComment = comment
          Exports = []
          Database = [] }