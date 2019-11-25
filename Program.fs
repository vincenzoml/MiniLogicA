open System.Collections.Generic

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
    let m = Map (Seq.concat <| Seq.map (fun (elems,n) -> Seq.map (fun elem -> (elem,n)) elems) k)
    (fun x -> m.Item x) : Quotient<'a>

type Transition<'a,'label when 'a : comparison and 'label : comparison> = Set<'label> * Set<'a>
let map (fn : 'a -> 'b) (tset : Transition<'a,'label>) : Transition<'b,'label> = 
    (fst tset,Set.map fn (snd tset))

type Coalg<'a,'label when 'a : comparison and 'label : comparison> = { carrier : Set<'a> ; fn : 'a -> Transition<'a,'label> }

let next (coalg : Coalg<'a,'label>) (k : Kernel<'a>) (fin : Quotient<'a>) = 
    let ftmp = map fin << coalg.fn
    let k1 = kernel coalg.carrier ftmp
    if k = k1 
    then None
    else Some ((k1 : Kernel<'a>), quotient k1)

let reduce coalg =
    let rec reduce coalg k fin = 
        match next coalg k fin with
        | None -> (k,fin)
        | Some (k1,fin1) -> reduce coalg k1 fin1
    let quotient = fun x -> 0 
    reduce coalg (kernel coalg.carrier quotient) quotient


open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open System


[<EntryPoint>]
let main argv =
    let x : Image<Rgb24> = Image.Load argv.[0]
    let s = seq { for i in 0..x.Width-1 do
                    for j in 0..x.Height-1 do
                        yield (i,j) }

    let inbounds (x : Image<_>) (i,j) = i >= 0 && j >= 0 && i < x.Width && j < x.Height
    let conn9 (i,j) = seq { for a in -1..1 do for b in -1..1 do yield (i+a,j+b) }

    let neighbours x (i,j) = 
        set (Seq.filter (inbounds x) <| conn9 (i,j))
    
    let fn (x : Image<Rgb24>) (i,j) = 
        let t = x.[i,j]
        let t1 = (t.R,t.G,t.B)
        let ap = [t1]
        let n = neighbours x (i,j)
        (set ap,n)
        
    let coalg = { carrier = Set s; fn = fn x }

    let (k,q) = reduce coalg

    let n = Set.count k
    let r = Random()
    let v = Array.init n (fun _ -> [|byte <| r.Next(0,255);byte <| r.Next(0,255);byte <| r.Next(0,255)|])
    let colouring = fun (i,j) -> v.[q (i,j)]
    let doit (i,j) = x.[i,j] <- (let c = colouring (i,j) in Rgb24(c.[0],c.[1],c.[2]))
    Seq.iter doit coalg.carrier
    x.Save(argv.[1])
    0
