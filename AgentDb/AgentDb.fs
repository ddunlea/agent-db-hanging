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
let ctx = Sql.GetDataContext()

let agent = MailboxProcessor.Start(fun (inbox:MailboxProcessor<String>) ->  
    let mutable callbacks = []
    let mutable events = []
    let rec loop() =
        async {
            let! msg = inbox.Receive()
            match msg with
            | _ ->
              let row = ctx.Main.Events.Create()
              row.Timestamp <- DateTime.Now
              printfn "Submitting"
              ctx.SubmitUpdates()
              printfn "Submitted"
            return! loop() 
        }
    loop() 
)

agent.Post "Hello"
