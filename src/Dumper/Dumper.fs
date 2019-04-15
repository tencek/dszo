module Dszo.Dumper.Dumper

[<EntryPoint>]
let main argv =
    let logger str = printfn "%A: %s" System.DateTime.Now str
    logger <| "Hello World from dumper!"

    match argv with
    | [|filepath|] ->
        logger <| sprintf "Dumping to %s" filepath
        let AsyncSaveSnapshot = Snapshots.AsyncSaveSnapshot logger filepath
        let AsyncCreateSnapshot () = Snapshots.AsyncCreateSnapshot logger

        let latestTimestamp = 
            Snapshots.AsyncGetLatestTimestamp logger filepath |> Async.RunSynchronously
            |> function
                | Some dateTime -> dateTime
                | None -> System.DateTime.MinValue

        logger <| sprintf "Using %A as latest timestamp" latestTimestamp

        Seq.initInfinite ( fun _x -> ())
        |> Seq.fold (fun lastTimeStamp _elm -> 
            try
                let snapshot = AsyncCreateSnapshot () |> Async.RunSynchronously
                if snapshot.TimeStamp <> lastTimeStamp then
                    AsyncSaveSnapshot snapshot |> Async.RunSynchronously
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(30000.0))
                snapshot.TimeStamp
            with 
                exn -> 
                    logger <| sprintf "Some error occured: %s" exn.Message
                    System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(10000.0))
                    lastTimeStamp) ( latestTimestamp )
        |> ignore
        0
    | argv ->
        logger <| sprintf "Usage: %s path/to/output/file" System.AppDomain.CurrentDomain.FriendlyName
        1
