﻿// Copyright (c) Stephen Tetley 2018
// License: BSD 3 Clause

namespace FactX


open FactX.Internal.FormatCombinators
open FactX.Internal


[<AutoOpen>]
module FactOutput = 

    type ClauseBody = PrologSyntax.Value list

    type Clause = 
        { Signature: FactSignature.Signature
          Body : ClauseBody }
        member v.ToProlog() : PrologSyntax.Clause = 
            { FactName = v.Signature.Name
            ; Values = v.Body }

    
    let private makeFactSet (signature:FactSignature.Signature) 
                            (clauses: ClauseBody list) : PrologSyntax.FactSet =
        let makeClause1 (body:ClauseBody)  = 
            { Signature = signature; Body = body }
        { FactName = signature.Name
          Arity = signature.Arity
          Signature = signature.ToString()
          Comment = ""
          Clauses = List.map (fun (v:ClauseBody) -> (makeClause1 v).ToProlog()) clauses
          }


    /// Extending FactBase to include e.g comments on clauses, would be 
    /// nice but we lose the simplicity (and potentially the efficiency) 
    /// of just wrapping Map<>.
    /// Also we want FactBase to be immutable so we can have e.g backtracking 
    /// fact extractors.
    [<Struct>]
    type FactBase = 
        | FactBase of Map<FactSignature.Signature, ClauseBody list>
     
        static member empty : FactBase = 
            FactBase Map.empty

        member v.Add (clause:Clause) : FactBase = 
            let (FactBase db) = v
            let db1 = 
                match Map.tryFind clause.Signature db with
                | None -> db.Add(clause.Signature, [clause.Body])
                | Some xs -> db.Add(clause.Signature, clause.Body :: xs)
            FactBase db1

        member v.Add(opt:option<Clause>) : FactBase = 
            match opt with
            | None -> v
            | Some clause -> v.Add(clause)

        member v.Concat (facts:FactBase) : FactBase = 
            let (FactBase db0) = v 
            let (FactBase db1) = facts
            FactBase <| List.foldBack (fun (key,value) ac -> ac) (Map.toList db1) db0

        static member ofList(clauses:Clause list) : FactBase =
            List.foldBack (fun (clz:Clause) ac -> ac.Add(clz)) clauses FactBase.empty

        static member ofOptionList(optClauses:option<Clause> list) : FactBase =
            List.foldBack (fun (opt:option<Clause>) ac -> 
                                match opt with
                                | None -> ac
                                | Some clz -> ac.Add(clz) ) 
                          optClauses 
                          FactBase.empty

        member v.ToProlog() : PrologSyntax.FactSet list = 
            let (FactBase db) = v in 
            Map.toList db |> List.map (fun (k,x) -> makeFactSet k x)
    
    let mergeFactBases (dbs:FactBase list) : FactBase = 
        match dbs with
        | [] -> FactBase.empty
        | x :: xs -> List.foldBack (fun e ac -> ac.Concat(e)) xs x

    type Module = 
        val ModuleName : string
        val GlobalComment : string
        val Database : FactBase
        new (name:string, db:FactBase) = 
            { ModuleName = name
            ; GlobalComment = ""
            ; Database = db }

        new (name:string, dbs:FactBase list) = 
            { ModuleName = name
            ; GlobalComment = ""
            ; Database = mergeFactBases dbs }

        new (name:string, comment:string, db:FactBase) = 
            { ModuleName = name
            ; GlobalComment = comment
            ; Database = db }

        new (name:string, comment:string, dbs:FactBase list) = 
            { ModuleName = name
            ; GlobalComment = comment
            ; Database = mergeFactBases dbs }

        member v.ToProlog() : PrologSyntax.Module = 
            let prologFacts = v.Database.ToProlog () 
            new PrologSyntax.Module (name = v.ModuleName, comment = v.GlobalComment, db = prologFacts)

        member v.Save(filePath:string) = 
            let prologModule = v.ToProlog()
            use sw = new System.IO.StreamWriter(filePath)
            sw.Write (render <| prologModule.Format ())