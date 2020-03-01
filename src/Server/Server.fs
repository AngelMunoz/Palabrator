open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Types
open Utils
open Microsoft.AspNetCore.Authentication.JwtBearer

let jsonpipeline = pipeline {
    plug acceptJson
}

let webApp = router {
    pipe_through jsonpipeline

    forward "/auth" AuthActions.routes
    forward "/api/perfiles" Perfiles.rutas
    forward "/api/palabras" Palabras.rutas
}

let app = application {
    url ("http://0.0.0.0:" + Utils.port.ToString() + "/")
    use_router webApp
    memory_cache
    use_gzip
    use_jwt_authentication Utils.secretKey Utils.issuer
}

run app
