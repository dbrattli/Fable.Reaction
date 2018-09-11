// From: https://github.com/ncthbrt/Fable.Websockets/blob/master/src/Fable.Websockets.Client/src/Client.fs

module Fable.Reaction.WebSocket

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

open Fable.Reaction.WebSocket.Protocol

let private toObj () = obj()

let [<PassGenerics>] private receiveMessage<'clientProtocol> (receiveSubject: Subject<WebsocketEvent<'clientProtocol>>) (msgEvent:MessageEvent) =
    try
        let msg = ofJson<'clientProtocol> (string msgEvent.data)
        Msg msg
    with
        | e -> Exception e
    |> receiveSubject.Next
    |> toObj

let private receiveCloseEvent<'clientProtocol, 'serverProtocol> (receiveSubject:Subject<WebsocketEvent<'clientProtocol>>) (sendSubject:Subject<'serverProtocol>) (closeEvent:CloseEvent) =
    let closedCode = (toClosedCode<<uint16) closeEvent.code
    let payload  = { code = closedCode; reason= closeEvent.reason; wasClean=closeEvent.wasClean }

    do payload
    |> WebsocketEvent.Closed
    |> receiveSubject.Next

    do sendSubject.Completed()
    do receiveSubject.Completed()

    obj()

let private sendMessage (websocket:WebSocket) (receiveSubject:Subject<WebsocketEvent<'a>>) msg =
    try
        let jsonMsg = msg |> toJson
        do websocket.send jsonMsg
    with
        | e -> receiveSubject.Next (Exception e)


let [<PassGenerics>] public establishWebsocketConnection<'serverProtocol, 'clientProtocol> (uri:string) :
    (('serverProtocol->unit)*IObservable<WebsocketEvent<'clientProtocol>>*(ClosedCode->string->unit)) =


    let receiveSubject = Subject<WebsocketEvent<'clientProtocol>>()
    let sendSubject = Subject<'serverProtocol>()

    let websocket = WebSocket.Create(uri)

    let connection = (sendSubject.Subscribe (sendMessage websocket receiveSubject))

    let closeHandle (code:ClosedCode) (reason:string) =
        let state = websocket.readyState |> uint16 |> toReadyState
        if state=Connecting || state=Open then
            websocket.close((float<<fromClosedCode) code, reason)
            connection.Dispose()
        else ()

    websocket.onmessage <- fun msg -> (receiveMessage<'clientProtocol> receiveSubject msg)
    websocket.onclose <- fun msg -> (receiveCloseEvent receiveSubject sendSubject msg)
    websocket.onopen <- fun _ -> receiveSubject.Next Opened |> toObj
    websocket.onerror <- fun _ -> receiveSubject.Next Error |> toObj

    (sendSubject.Next, receiveSubject :> IObservable<_>, closeHandle)