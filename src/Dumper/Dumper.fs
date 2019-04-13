module Dszo.Dumper.Dumper
// Learn more about F# at http://fsharp.org

[<Literal>]
let OutputFile = @"C:\temp\vehicles2.csv"


[<EntryPoint>]
let main argv =
    let logger str = printfn "%A: %s" System.DateTime.Now str
    let saveSnapshot = Snapshots.SaveSnapshot logger OutputFile
    let createSnapshot () = Snapshots.CreateSnapShot logger

    logger <| "Hello World from dumper!"
    Seq.initInfinite ( fun _x -> ())
    |> Seq.fold (fun lastTimeStamp _elm -> 
        try
            let snapshot = createSnapshot ()
            if snapshot.TimeStamp <> lastTimeStamp then
                saveSnapshot snapshot
            System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(30000.0))
            snapshot.TimeStamp
        with 
            exn -> 
                logger <| sprintf "Some error occured: %s" exn.Message
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(10000.0))
                lastTimeStamp) ( System.DateTime.MinValue )
    |> ignore
    0
