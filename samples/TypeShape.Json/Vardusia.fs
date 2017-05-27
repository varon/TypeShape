﻿namespace Vardusia

open System
open System.Globalization

open TypeShape_Utils

open Vardusia.PrimitivePicklers
open Vardusia.PicklerGeneration

type PicklerCache() =
    let cache = new TypeCache()
    static let defaultCache = new PicklerCache()

    member __.Register(pickler : JsonPickler<'T>) = cache.TryAdd pickler
    member __.Register(generator : IPicklerGenerator<'T>) =
        let pickler = generator.Generate __
        __.Register pickler

    member __.Resolve<'T> () = resolve<'T> cache

    interface IPicklerResolver with
        member __.Resolve<'T> () = resolve<'T> cache

    static member internal DefaultCache = defaultCache


type JsonSerializer(?cache : PicklerCache, ?indent : Indent, ?format : IFormatProvider) =
    let cache = defaultArg cache PicklerCache.DefaultCache

    member val Indent = defaultArg indent Indent.None with get, set
    member val Format = defaultArg format (CultureInfo.InvariantCulture :> _) with get, set

    member __.Pickle<'T>(value : 'T, ?pickler : JsonPickler<'T>, ?indent : Indent, ?format : IFormatProvider) : string =
        let pickler = match pickler with None -> cache.Resolve<'T>() | Some p -> p
        let format = defaultArg format __.Format
        let indent = defaultArg indent __.Indent
        let writer = new JsonWriter(indent, format)
        pickler.Pickle writer value
        writer.ToJson()

    member __.UnPickle<'T>(json : string, ?pickler : JsonPickler<'T>, ?format : IFormatProvider) : 'T =
        let pickler = match pickler with None -> cache.Resolve<'T>() | Some p -> p
        let format = defaultArg format __.Format
        let reader = new JsonReader(json, format)
        pickler.UnPickle reader


[<RequireQualifiedAccess>]
module Pickler =
    
    let auto<'T> = PicklerCache.DefaultCache.Resolve<'T>()

    let private serializer = JsonSerializer()
    let pickle (pickler : JsonPickler<'T>) (value : 'T) : string = serializer.Pickle(value, pickler = pickler)
    let unpickle (pickler : JsonPickler<'T>) (json : string) : 'T = serializer.UnPickle(json, pickler = pickler)

    let int = Int32Pickler() :> JsonPickler<int>