﻿namespace FSCL.Compiler.FunctionCodegen

open FSCL
open FSCL.Compiler
open FSCL.Language
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Reflection
open FSCL.Compiler.Util.ReflectionUtil

[<StepProcessor("FSCL_DECLARATION_CODEGEN_PROCESSOR", "FSCL_FUNCTION_CODEGEN_STEP",
                Dependencies =[| "FSCL_FOR_RANGE_CODEGEN_PROCESSOR" |])>]
type DeclarationCodegen() =   
    inherit FunctionBodyCodegenProcessor()

    member private this.PrintArray(engine: FunctionCodegenStep,
                                   methodInfo: MethodInfo, 
                                   v: Var, 
                                   args: Expr list, 
                                   isLocal: bool) =
        let prefix = if isLocal then "local " else ""
        if (methodInfo.DeclaringType <> null && methodInfo.DeclaringType.Name = "ArrayModule" && methodInfo.Name = "ZeroCreate") then
            let mutable code = prefix + engine.TypeManager.Print(v.Type.GetElementType()) + " " + v.Name + "[" + engine.Continue(args.[0]) + "];\n"
            Some(code)
        else if (methodInfo.DeclaringType <> null && methodInfo.DeclaringType.Name = "Array2DModule" && methodInfo.Name = "ZeroCreate") then
            let mutable code = prefix + engine.TypeManager.Print(v.Type.GetElementType()) + " " + v.Name + "[" + engine.Continue(args.[0]) + "][" + engine.Continue(args.[1]) + "];\n"
            Some(code)
        else if (methodInfo.DeclaringType <> null && methodInfo.DeclaringType.Name = "Array3DModule" && methodInfo.Name = "ZeroCreate") then
            let mutable code = prefix + engine.TypeManager.Print(v.Type.GetElementType()) + " " + v.Name + "[" + engine.Continue(args.[0]) + "][" + engine.Continue(args.[1]) + "][" + engine.Continue(args.[2]) + "];\n"
            Some(code)
        else
            None

    member private this.TryPrintStructOrOptionOrRecordDecl(v: Var, value: Expr, body: Expr, step: FunctionCodegenStep) =
        // Check this is not a vector type (it's handled differently)
        if v.Type.GetCustomAttribute<VectorTypeAttribute>() = null then
            match value with
            // Default struct init
            | Patterns.DefaultValue(t) ->
                if t.IsStruct() then
                    Some(step.TypeManager.Print(v.Type) + " " + v.Name + ";\n" + step.Continue(body))
                else
                    None   
            // Check if tuple type                        
            | Patterns.NewTuple(args) ->
                let mutable gencode = step.TypeManager.Print(v.Type) + " " + v.Name + " = { ";
                // Gen fields init
                let fields = args
                for i = 0 to fields.Length - 1 do
                    gencode <- gencode + ".Item" + i.ToString() + " = " + step.Continue(args.[i])
                    if i < fields.Length - 1 then
                        gencode <- gencode + ", "
                Some(gencode + " };\n" + step.Continue(body))                  
            | Patterns.NewUnionCase(ui, args) ->
                // Check if option type
                if ui.DeclaringType.IsOption then
                    // Struct initialisation
                    if args.Length > 0 then
                        let gencode = 
                            Some(
                                step.TypeManager.Print(ui.DeclaringType) + " " + 
                                v.Name + " = { .Value = " + step.Continue(args.[0]) + ", .IsSome = 1 };\n" + 
                                step.Continue(body))
                        gencode                          
                    else
                        let gencode = 
                            Some(
                                step.TypeManager.Print(ui.DeclaringType) + " " + 
                                v.Name + " = { .IsSome = 0 };\n" + 
                                step.Continue(body))
                        gencode  
                else
                    None   
            // Non-Default struct init
            | Patterns.NewObject(constr, args) ->
                if v.Type.IsStruct() then
                    let mutable gencode = step.TypeManager.Print(v.Type) + " " + v.Name + " = { ";
                    // Gen fields init
                    let fields = v.Type.GetFields()
                    for i = 0 to fields.Length - 1 do
                        gencode <- gencode + "." + fields.[i].Name + " = " + step.Continue(args.[i])
                        if i < fields.Length - 1 then
                            gencode <- gencode + ", "
                    Some(gencode + " };\n" + step.Continue(body))                   
                else
                    None
            // Record init
            | Patterns.NewRecord(t, args) ->
                let mutable gencode = step.TypeManager.Print(v.Type) + " " + v.Name + " = { ";
                // Gen fields init
                let fields = FSharpType.GetRecordFields(t)
                for i = 0 to fields.Length - 1 do
                    gencode <- gencode + "." + fields.[i].Name + " = " + step.Continue(args.[i])
                    if i < fields.Length - 1 then
                        gencode <- gencode + ", "
                Some(gencode + " };\n" + step.Continue(body))  
            | _ ->
                None
        else
            None

    override this.Run(expr, s, opts) =
        let step = s :?> FunctionCodegenStep
        match expr with
        | Patterns.Let(v, value, body) ->
            // Try print struct
            let structCode = this.TryPrintStructOrOptionOrRecordDecl(v, value, body, step)
            if structCode.IsSome then
                structCode
            else
                match value with            
                // Local declaration
                | DerivedPatterns.SpecificCall <@ local @> (o, tl, a) ->
                    if a.[0].Type.IsArray then
                        // Local array
                        match a.[0] with 
                        | Patterns.Call(_, methodInfo, args) ->
                            Some(this.PrintArray(step, methodInfo, v, args, true).Value + step.Continue(body))                        
                        | _ ->
                            None
                    else
                        let structCode = this.TryPrintStructOrOptionOrRecordDecl(v, a.[0], body, step)
                        if structCode.IsSome then
                            Some("local " + structCode.Value)
                        else
                            // Local primitive type
                            Some("local " + step.TypeManager.Print(v.Type) + " " + v.Name + ";\n" + step.Continue(body))
                | _ ->
                    // Private array maybe
                    match value with
                    | Patterns.Call(_, methodInfo, args) ->
                        match this.PrintArray(step, methodInfo, v, args, false) with
                        | Some(c) ->
                            // Private array alloc, return
                            Some(c + step.Continue(body))
                        | None ->
                            // Not array alloc call
                            Some(step.TypeManager.Print(v.Type) + " " + v.Name + " = " + step.Continue(value) + ";\n" + step.Continue(body))
                    | _ ->
                        // Normal declaration
                        Some(step.TypeManager.Print(v.Type) + " " + v.Name + " = " + step.Continue(value) + ";\n" + step.Continue(body))
        | _ ->
            None