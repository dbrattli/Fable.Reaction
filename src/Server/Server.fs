open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open FSharp.Control.Tasks.V2
open Giraffe
open Reaction
open Reaction.Giraffe.Middleware
open Shared

open Giraffe.Serialization

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

let getInitCounter () : Task<Counter> = task { return 42 }

let webApp =
    route "/api/init" >=>
        fun next ctx ->
            task {
                let! counter = getInitCounter()
                return! Successful.OK counter next ctx
            }

let query (connectionId: ConnectionId) (msgs: IAsyncObservable<Msg*ConnectionId>) : IAsyncObservable<Msg*ConnectionId> =
    msgs
    |> AsyncObservable.filter (fun (msg, ci) -> ci = connectionId)

let configureApp (app : IApplicationBuilder) =
    app.UseWebSockets()
       .UseReaction<Msg>(fun options ->
       { options with
           Query = query
           Encode = Msg.Encode
           Decode = Msg.Decode
       })
       .UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings) |> ignore

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()