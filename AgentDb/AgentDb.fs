open Newtonsoft.Json
open FSharp.Data.Sql
open System

let [<Literal>] ResolutionPath = __SOURCE_DIRECTORY__ + "/../build/" 
let [<Literal>] ConnectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"/test.db;Version=3"

// test.db is initialized as follows:
//
// BEGIN TRANSACTION;
//    CREATE TABLE "Events" (
//        `id`INTEGER PRIMARY KEY AUTOINCREMENT,
//        `timestamp` DATETIME NOT NULL
//    );
//    COMMIT;

type Sql = SqlDataProvider< 
            ConnectionString = ConnectionString,
            DatabaseVendor = Common.DatabaseProviderTypes.SQLITE,
            ResolutionPath = ResolutionPath,
            IndividualsAmount = 1000,
            UseOptionTypes = true >

let agent = MailboxProcessor.Start(fun (inbox:MailboxProcessor<String>) ->  
    let mutable callbacks = []
    let mutable events = []
    let rec loop() =
        async {
            let! msg = inbox.Receive()
            match msg with
            | _ ->
              let ctx = Sql.GetDataContext()
              let row = ctx.Main.Events.Create()
              row.Timestamp <- DateTime.Now
              printfn "Submitting"
              ctx.SubmitUpdates()
              printfn "Submitted"
            return! loop() 
        }
    loop() 
)

[<EntryPoint>]
let main argv =
    agent.Post "Hello"
    agent.Post "Hello again"
    let waitLoop = async {
        while agent.CurrentQueueLength > 0 do
            printfn "Sleeping"
            do! Async.Sleep 1000
        }
    Async.RunSynchronously waitLoop
    0