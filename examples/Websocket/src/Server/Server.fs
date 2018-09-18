open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe
open Shared

open Giraffe.Serialization
open WebSocketApp.Middleware
open Reaction

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

let query (msgs: AsyncObservable<Msg>) : AsyncObservable<Msg> =
    msgs |> debounce 2000

let configureApp (app : IApplicationBuilder) =
    app.UseWebSockets()
       .UseReaction<Msg>(fun () ->
       {
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