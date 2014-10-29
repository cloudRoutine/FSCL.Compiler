﻿module FSCL.Compiler.DynamicDefineTest

open NUnit
open NUnit.Framework
open System.IO
open FSCL.Compiler
open FSCL.Language
open Microsoft.FSharp.Linq.RuntimeHelpers
open System.Runtime.InteropServices

module KernelModule =     
    [<ReflectedDefinition>] 
    let DataFieldModule =
        10.0f
    
    [<ReflectedDefinition>] 
    let VectorAddModule (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * DataFieldModule
        
    let Compile(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ VectorAddModule(size, a, b, c) @>) :?> IKernelModule
            
type KernelWrapper(data: float32) =        
    [<ReflectedDefinition>] 
    let DataField =
        10.0f
             
    [<ReflectedDefinition>] 
    let mutable DataFieldMutable =
        10.0f
        
    [<ReflectedDefinition>] 
    static let mutable DataFieldMutableStatic =
        10.0f
            
    [<ReflectedDefinition>] 
    let DataFieldFromConstructor =
        data
                        
    [<ReflectedDefinition>] 
    member this.DataPropertyWithGet 
        with get() =
            10.0f    
               
    [<ReflectedDefinition>] 
    member this.DataPropertyWithGetFromConstructor
        with get() =
            data

    [<ReflectedDefinition>] 
    member this.DataPropertyWithGetSet
        with get() =
            DataFieldMutable
        and set v =
            DataFieldMutable <- v 
            
    [<ReflectedDefinition>] 
    static member StaticDataField = 10.0f   
    
    static member StaticDataPropertyWithGetSet
        with get() =
            DataFieldMutableStatic
        and set v =
            DataFieldMutableStatic <- v               
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingField (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * DataField
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingFieldFromConstructor (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * DataFieldFromConstructor
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingMutableField (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * DataFieldMutable
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingPropertyWithGet (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * this.DataPropertyWithGet
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingPropertyWithGetSet (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * this.DataPropertyWithGetSet
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingPropertyWithGetFromConstructor (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * this.DataPropertyWithGetFromConstructor
        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingStaticField (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * KernelWrapper.StaticDataField
                        
    [<ReflectedDefinition>] 
    member this.VectorAddUsingModuleField (wi:WorkItemInfo, a: float32[], b:float32[], c:float32[]) =    
        let gid = wi.GlobalID(0)
        c.[gid] <- a.[gid] + b.[gid] * KernelModule.DataFieldModule
                        
    member this.CompileVectorAddUsingField(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingField(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingMutableField(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingMutableField(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingFieldFromConstructor(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingFieldFromConstructor(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingPropertyWithGet(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingPropertyWithGet(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingPropertyWithGetSet(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingPropertyWithGetSet(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingPropertyWithGetFromConstructor(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingPropertyWithGetFromConstructor(size, a, b, c) @>) :?> IKernelModule
        
    member this.CompileVectorAddUsingStaticField(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingStaticField(size, a, b, c) @>) :?> IKernelModule
                        
    member this.CompileVectorAddUsingModuleField(compiler: Compiler, size, a, b, c) =        
        compiler.Compile(<@ this.VectorAddUsingModuleField(size, a, b, c) @>) :?> IKernelModule
        
let GetData() =
    let compiler = new Compiler()
    let a = Array.create 64 1.0f
    let b = Array.create 64 2.0f
    let c = Array.create 64 0.0f
    let size = new WorkSize(64L, 64L) :> WorkItemInfo
    let wrapper = new KernelWrapper(10.0f)
    compiler, a, b, c, size, wrapper    

let FirstConstDefineValue(m: IKernelModule, inst:KernelWrapper option) =
    let thisVar, _, f = m.DynamicConstantDefines.Values |> List.ofSeq |> List.head
    if thisVar.IsSome then
        f.GetType().GetMethod("Invoke").Invoke(f, [| inst.Value |]) :?> float32
    else
        f.GetType().GetMethod("Invoke").Invoke(f, [|()|]) :?> float32
        
[<Test>]
let ``Can compile module kernel using module field from inside and outside module`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = KernelModule.Compile(compiler, size, a, b, c)
    Assert.NotNull(insideResult)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, None), 10.0f)
    let outsideResult = compiler.Compile(<@ KernelModule.VectorAddModule(size, a, b, c) @>) :?> IKernelModule
    Assert.NotNull(outsideResult)
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, None), 10.0f)
    
[<Test>]
let ``Can compile kernel using instance field from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = wrapper.CompileVectorAddUsingField(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingField(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    
[<Test>]
let ``Can compile kernel using mutable instance field from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = wrapper.CompileVectorAddUsingMutableField(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    wrapper.DataPropertyWithGetSet <- 5.0f    
    let insideResult2 = wrapper.CompileVectorAddUsingMutableField(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult2, Some(wrapper)), 5.0f)
    
    wrapper.DataPropertyWithGetSet <- 10.0f    
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingMutableField(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    wrapper.DataPropertyWithGetSet <- 5.0f    
    let outsideResult2 = compiler.Compile(<@ wrapper.VectorAddUsingMutableField(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult2, Some(wrapper)), 5.0f)
    
[<Test>]
let ``Can compile kernel using field set from constructor from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()
    let wrapper2 = new KernelWrapper(2.0f)    

    let insideResult = wrapper.CompileVectorAddUsingFieldFromConstructor(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    let insideResult2 = wrapper2.CompileVectorAddUsingFieldFromConstructor(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult2, Some(wrapper2)), 2.0f)
        
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingFieldFromConstructor(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    let outsideResult2 = compiler.Compile(<@ wrapper2.VectorAddUsingFieldFromConstructor(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult2, Some(wrapper2)), 2.0f)
    
[<Test>]
let ``Can compile kernel using getter property from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = wrapper.CompileVectorAddUsingPropertyWithGet(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)        
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingPropertyWithGet(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    
[<Test>]
let ``Can compile kernel using getter property from constructor from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()
    let wrapper2 = new KernelWrapper(2.0f)    

    let insideResult = wrapper.CompileVectorAddUsingPropertyWithGetFromConstructor(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    let insideResult2 = wrapper2.CompileVectorAddUsingPropertyWithGetFromConstructor(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult2, Some(wrapper2)), 2.0f)
        
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingPropertyWithGetFromConstructor(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    let outsideResult2 = compiler.Compile(<@ wrapper2.VectorAddUsingPropertyWithGetFromConstructor(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult2, Some(wrapper2)), 2.0f)
    
[<Test>]
let ``Can compile kernel using getter-setter property from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = wrapper.CompileVectorAddUsingPropertyWithGetSet(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    wrapper.DataPropertyWithGetSet <- 3.0f
    let insideResult2 = wrapper.CompileVectorAddUsingPropertyWithGetSet(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult2, Some(wrapper)), 3.0f)
        
    wrapper.DataPropertyWithGetSet <- 10.0f
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingPropertyWithGetSet(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    wrapper.DataPropertyWithGetSet <- 3.0f
    let outsideResult2 = compiler.Compile(<@ wrapper.VectorAddUsingPropertyWithGetSet(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult2.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult2, Some(wrapper)), 3.0f)
    
[<Test>]
let ``Can compile kernel using static field from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()

    let insideResult = wrapper.CompileVectorAddUsingStaticField(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)        
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingStaticField(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
                
[<Test>]
let ``Can compile instance kernel using module field from inside and outside instance`` () =
    let compiler, a, b, c, size, wrapper = GetData()
    
    let insideResult = wrapper.CompileVectorAddUsingModuleField(compiler, size, a, b, c)
    Assert.IsNotEmpty(insideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(insideResult, Some(wrapper)), 10.0f)
    let outsideResult = compiler.Compile(<@ wrapper.VectorAddUsingModuleField(size, a, b, c) @>) :?> IKernelModule
    Assert.IsNotEmpty(outsideResult.DynamicConstantDefines)
    Assert.AreEqual(FirstConstDefineValue(outsideResult, Some(wrapper)), 10.0f)
    