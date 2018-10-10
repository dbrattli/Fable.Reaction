# Fable Reaction WebSocket Example

This example shows how to stream messages between the server and the client.

## Build

```shell
> mono .paket/paket.exe install
> yarn
> fake build
```

## Run

```shell
> fake build --target run
```

## Usage

To run the demo, open two (2) or more browsers targeting the service. Then push buttons in one browser and
see the counter being modified in the other browsers.