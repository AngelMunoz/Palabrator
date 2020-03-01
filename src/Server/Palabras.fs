[<RequireQualifiedAccess>]
module Palabras

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn

let private palabrasPerfil perfilId (next: HttpFunc) (ctx: HttpContext) =
    task { return! (text (sprintf "perfilid: %i" perfilId)) next ctx }

let rutas =
    router {
        pipe_through (Auth.requireAuthentication JWT)
        getf "/%i" palabrasPerfil
    }
