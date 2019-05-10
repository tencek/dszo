module Dszo.Dumper.Dumper

open Dszo.Dumper.Snapshots

[<EntryPoint>]
let main argv =
    let logger str = printfn "%A: %s" System.DateTime.Now str
    logger <| "Hello World from dumper!"

    match argv with
    | [|filepath|] ->
        logger <| sprintf "Dumping to %s" filepath
        let AsyncSaveSnapshot = Snapshots.AsyncSaveSnapshot logger filepath
        let AsyncTakeSnapshot () = Snapshots.AsyncTakeSnapshot logger

        let latestSnapshot = 
            Snapshots.AsyncLoadLatestSnapshot logger filepath |> Async.RunSynchronously
            |> function
                | Some snapshot -> snapshot
                | None -> { TimeStamp = System.DateTime.MinValue ; Vehicles = Seq.empty }

        logger <| sprintf "Using %A as latest snapshot" latestSnapshot

        Seq.initInfinite ( fun _x -> ())
        |> Seq.fold (fun previousSnapshot _elm -> 
            try
                let snapshot = AsyncTakeSnapshot () |> Async.RunSynchronously
                let change = Seq.compareWith Operators.compare previousSnapshot.Vehicles snapshot.Vehicles
                let diff = 
                    Seq.zip previousSnapshot.Vehicles snapshot.Vehicles
                    |> Seq.filter (fun (v1,v2) -> v1 <> v2)
                    |> Seq.map snd
                    |> Seq.map (fun vehicle -> vehicle.Number)
                if change <> 0 then
                    logger <| sprintf "Change! %A, diff? %A!" snapshot.TimeStamp diff
                    AsyncSaveSnapshot snapshot |> Async.RunSynchronously
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(20000.0))
                snapshot
            with 
                exn -> 
                    logger <| sprintf "Some error occured: %s" exn.Message
                    System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(10000.0))
                    previousSnapshot) ( latestSnapshot )
        |> ignore
        0
    | argv ->
        logger <| sprintf "Usage: %s path/to/output/file" System.AppDomain.CurrentDomain.FriendlyName
        1
