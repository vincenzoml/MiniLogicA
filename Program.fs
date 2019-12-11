open System
open System.Collections.Generic

// Begin algorithm

type Kernel<'a when 'a : comparison> = Set<Set<'a> * int>
type Quotient<'a when 'a : comparison> = 'a -> int

let kernel = 
    fun x fn ->
        let graph = Set.map (fun x -> (x,fn x)) x
        let grouped = Seq.groupBy snd graph
        let kerntmp = Seq.map (fun (_,group) -> set <| Seq.map fst group) grouped 
        let ids = Seq.init (Seq.length kerntmp) id
        set (Seq.zip kerntmp ids) : Kernel<'a>

let quotient (k : Kernel<'a>) = 
    let m = Map (Seq.collect (fun (elems,n) -> Seq.map (fun elem -> (elem,n)) elems) k)
    (fun x -> m.Item x) : Quotient<'a>

type Transition<'a,'ap when 'a : comparison and 'ap : comparison> = Set<'ap> * Set<'a> * Set<'a>
let map (fn : 'a -> 'b) (tset : Transition<'a,'ap>) : Transition<'b,'ap> = 
    let (ap,fw,bw) = tset
    (ap,Set.map fn fw,Set.map fn bw)

type Coalg<'a,'ap when 'a : comparison and 'ap : comparison>(carrier : Set<'a>,fn : 'a -> Transition<'a,'ap>) =
    member __.Carrier = carrier
    member __.Fn = fn

let next (coalg : Coalg<'a,'ap>) (k : Kernel<'a>) (fin : Quotient<'a>) = 
    let ftmp = map fin << coalg.Fn
    let k1 = kernel coalg.Carrier ftmp
    if k = k1 
    then None
    else Some ((k1 : Kernel<'a>), quotient k1)

let reduce coalg =
    let rec reduce coalg k fin = 
        match next coalg k fin with
        | None -> (k,fin)
        | Some (k1,fin1) -> reduce coalg k1 fin1
    let quotient = fun x -> 0 
    reduce coalg (kernel coalg.Carrier quotient) quotient

open FSharp.Data

type 
    Graph = { nodes : Node list; arcs : Arc list} 
and
    Node = { id : string; atoms : string list }
and
    Arc = { source : string; target: string }

module List =
    let intersperse sep ls =
        List.foldBack (fun x -> function
            | [] -> [x]
            | xs -> x::sep::xs) ls []

let loadGraph filename = 
    FSharp.Json.Json.deserialize<Graph>(System.IO.File.ReadAllText(filename))

let coalgOfGraph graph =
    let statesObs = Seq.map (fun node -> (node.id,node.atoms)) graph.nodes
    let states = set (Seq.map fst statesObs)
    let mobs = Map statesObs
    let obs x = set mobs.[x] 
    let arcsS = Set.union (Set.map (fun arc -> (arc.source,arc.target)) (Set.ofList graph.arcs)) (Set.map (fun x -> (x,x)) states)
    let arcs = Set.toList arcsS
    let direct = List.map (fun (x,y) -> (x,List.map snd y)) (List.groupBy fst arcs)
    let inverse = List.map (fun (x,y) -> (x,List.map fst y)) (List.groupBy snd arcs)
    let mdirect = Map direct
    let minverse = Map inverse
    let fnDirect x = set mdirect.[x]    
    let fnInverse x = set minverse.[x]
    // TODO: add self-loops
    Coalg(states, fun state -> (obs state,fnInverse state,fnDirect state))

let graphOfCoalg<'a,'b when 'a : comparison and 'b : comparison> (coalg : Coalg<'a,'b>) = 
    let obs state = Seq.toList (let (o,_,_) = coalg.Fn state in Set.map (fun x -> x.ToString()) o)
    let arr state = Seq.toList (let (_,_,dst) = coalg.Fn state in dst)
    let nodes = Seq.map (fun state -> { id = state.ToString() ; atoms = List.map string <| obs state }) coalg.Carrier
    let arcsOf x = List.map (fun y -> {source = x.ToString(); target = y.ToString()}) (arr x)
    let arcs = List.collect arcsOf (Set.toList coalg.Carrier)
    { nodes = Seq.toList nodes; arcs = arcs }

let saveJson graph filename =    
    System.IO.File.WriteAllText(filename,FSharp.Json.Json.serialize(graph))

let graphViz graph =
    let mutable res = []
    let pushLine x = res <- (x::res)
    //let symmetric = match graph.symmetric with Some true -> true | _ -> false
    let symmetric = false // TODO: see above, implement
    let separator = if symmetric then "--" else "->"
    pushLine (if symmetric then "graph {" else "digraph {")    
    let atoms node = System.String.Concat(List.intersperse "," node.atoms)
    Seq.iter (fun node -> pushLine (sprintf "  %s [label=\"%s\"];" node.id (atoms node))) graph.nodes
    pushLine ""
    Seq.iter (fun arc -> pushLine (sprintf "  %s %s %s;" arc.source separator arc.target)) graph.arcs
    pushLine "}"
    System.String.Concat (List.intersperse "\n" (List.rev res))

let saveGraphViz graphviz filename =
    System.IO.File.WriteAllText(filename,graphviz)

open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

let swap seq = Seq.map (fun (a,b) -> (b,a)) seq

let loadImage (filename : string) = 
        Image.Load filename : Image<Rgb24>
    
let coalgOfImage (img : Image<Rgb24>) =
    let s = seq { for i in 0..img.Width-1 do
                    for j in 0..img.Height-1 do
                        yield (i,j) }
    let inbounds (img : Image<_>) (i,j) = i >= 0 && j >= 0 && i < img.Width && j < img.Height
    let conn9 (i,j) = seq { for a in -1..1 do for b in -1..1 do yield (i+a,j+b) }
    let neighbours x (i,j) = 
        set (Seq.filter (inbounds x) <| conn9 (i,j))        
    let fn (img : Image<Rgb24>) (i,j) = 
        let t = img.[i,j]
        let t1 = (t.R,t.G,t.B)
        let ap = [t1]
        let n = neighbours img (i,j)
        (set ap,n,n)            
    Coalg(Set s,fn img)

let finalGraph<'a,'b when 'a : comparison and 'b : comparison> (inputCoalg : Coalg<'a,'b>) (k,q) =
    let carrierS = Seq.map (string << snd) k
    let carrier = Set carrierS
    let m = Map (swap <| Seq.map (fun (c,i) -> (Seq.head (Set.toSeq c),i)) k)
    let fnM i = m.Item i
    let fn si = 
        let i = int si
        let (obs,bw,fw) = inputCoalg.Fn (fnM i)
        (obs,Set.map (string << q) bw,Set.map (string << q) fw)
    let outputCoalg = Coalg(Set.map string carrier,fn)
    graphOfCoalg outputCoalg    

let unsupportedArguments s1 s2 = 
    //printfn "Usage: MiniLogicA input.{png,dot} output.dot [quotient.{png,dot}]\nQuotient can be png only if input is also png\n" 
    printfn "Operating with input extension %s and output extension %s is not supported" s1 s2 


let usage () = 
    //printfn "Usage: MiniLogicA input.{png,dot} [output.dot] [quotient.{png,dot}]\nQuotient can be png only if input is also png\n" 
    printfn "Usage: MiniLogicA input.{png,json} [output.{dot}]"
    //printfn "Quotient can be png only if input is also png"
    printfn "" 
    exit 0

let minimize coalg =
    let (k,q) = reduce coalg 
    let outputGraph = finalGraph coalg (k,q) // todo: more efficient to return this from "reduce"    
    graphViz outputGraph 

[<EntryPoint>]
let main argv = 
    if argv.Length < 1 || argv.Length > 2 
    then usage ()
    let inFileName = argv.[0]
    let inExt = System.IO.Path.GetExtension inFileName
    let outExt = if argv.Length = 2 then System.IO.Path.GetExtension argv.[1] else ".dot"  
    let output = if argv.Length = 2 then Some argv.[1] else None
    let res = 
        match (inExt,outExt) with
        | ".json",".dot" -> minimize (coalgOfGraph (loadGraph inFileName))            
        | ".png",".dot" -> minimize (coalgOfImage (loadImage inFileName))            
        | s1,s2 -> 
            unsupportedArguments s1 s2
            usage ()
    match output with
        | None -> printfn "%s" res
        | Some fname -> saveGraphViz res fname
    0

    

    // QUOTIENT IMAGE:
    // let (k,q) = reduce coalg

    // let n = Set.count k
    // let r = Random()
    // let v = Array.init n (fun _ -> [|byte <| r.Next(0,255);byte <| r.Next(0,255);byte <| r.Next(0,255)|])
    // let colouring = fun (i,j) -> v.[q (i,j)]
    // let doit (i,j) = x.[i,j] <- (let c = colouring (i,j) in Rgb24(c.[0],c.[1],c.[2]))
    // Seq.iter doit coalg.Carrier
    // x.Save(argv.[1])
    // 0
