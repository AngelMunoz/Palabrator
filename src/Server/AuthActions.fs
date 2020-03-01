[<RequireQualifiedAccess>]
module AuthActions

open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Types
open BCrypt.Net
open Database

let private generateToken email =
    let claims =
        [| Claim(JwtRegisteredClaimNames.Sub, email)
           Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) |]
    claims
    |> Auth.generateJWT (Utils.secretKey, SecurityAlgorithms.HmacSha256) Utils.issuer (DateTime.UtcNow.AddHours(1.0))


let private login (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<LoginPayload>()
        let! result = AuthQueries.buscarUsuario (Correo payload.email)
        let usuario =
            match result with
            | Ok usuarios -> usuarios |> List.tryHead
            | Error ex ->
                printfn "%O" ex
                None
        match usuario with
        | Some usuario ->
            match BCrypt.EnhancedVerify(payload.password, usuario.contrasena) with
            | true ->
                let token = generateToken payload.email
                return! json
                            { accessToken = token
                              email = payload.email } next ctx
            | false -> return! setStatusCode 400 next ctx
        | None -> return! setStatusCode 404 next ctx
    }

let private signup (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<SignupPayload>()
        let newuser = { payload with contrasena = BCrypt.EnhancedHashPassword payload.contrasena }
        let! result = AuthQueries.crearUsuario newuser
        match result with
        | Ok _ ->
            let token = generateToken newuser.correo
            return! json
                        { accessToken = token
                          email = payload.correo } next ctx
        | Error err ->
            printfn "%O" err

            return! setStatusCode 400 next ctx
    }

let private forgot (next: HttpFunc) (ctx: HttpContext) = task { return! (text "Forgot!") next ctx }

let routes =
    router {
        post "/login" login
        post "/signup" signup
        post "/forgot" forgot
    }
