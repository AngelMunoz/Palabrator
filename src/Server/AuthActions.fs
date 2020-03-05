[<RequireQualifiedAccess>]
module AccionesAuth

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
open Npgsql.FSharp

let private crearUsuario (usuario: SignupPayload) =
    async {
        let insert = """
            INSERT INTO usuarios
                (nombre, apellidos, correo, usuario, contrasena)
            VALUES(@nombre, @apellidos, @correo, @usuario, @contrasena) RETURNING id, nombre, apellidos, correo, usuario;
            """

        let insertParams =
            [ "@nombre", Sql.string usuario.nombre
              "@apellidos", Sql.string usuario.apellidos
              "@correo", Sql.string usuario.correo
              "@usuario", Sql.string usuario.usuario
              "@contrasena", Sql.string usuario.contrasena ]

        return! Database.prepareDefault insert insertParams
                |> Sql.executeAsync (fun reader ->
                    {| id = reader.int "id"
                       nombre = reader.string "nombre"
                       apellidos = reader.string "apellidos"
                       correo = reader.string "correo"
                       usuario = reader.string "usuario" |})
    }

let private buscarUsuario (criterio: BusquedaUsuario): Result<Usuario list, exn> Async =
    let statement =
        match criterio with
        | Correo correo ->
            let select = "SELECT * FROM usuarios WHERE correo = @correo"
            let selectParams = [ "@correo", Sql.string correo ]
            Database.prepareDefault select selectParams
        | Usuario usuario ->
            let select = "SELECT * FROM usuarios WHERE usuario = @usuario"
            let selectParams = [ "@usuario", Sql.string usuario ]
            Database.prepareDefault select selectParams
    statement
    |> Sql.executeAsync (fun reader ->
        { id = reader.int "id"
          nombre = reader.string "nombre"
          apellidos = reader.string "apellidos"
          correo = reader.string "correo"
          usuario = reader.string "usuario"
          contrasena = reader.string "contrasena" })



let private generateToken email =
    let claims =
        [| Claim(JwtRegisteredClaimNames.Sub, email)
           Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) |]
    claims
    |> Auth.generateJWT (Utils.secretKey, SecurityAlgorithms.HmacSha256) Utils.issuer (DateTime.UtcNow.AddHours(1.0))


let private inicioSesion (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<LoginPayload>()
        let! result = buscarUsuario (Correo payload.email)
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

let private registrarse (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<SignupPayload>()
        let newuser = { payload with contrasena = BCrypt.EnhancedHashPassword payload.contrasena }
        let! result = crearUsuario newuser
        match result with
        | Ok resultado ->
            match resultado |> List.tryHead with
            | Some usuario ->
                let token = generateToken usuario.correo
                return! (json
                             {| accessToken = token
                                email = payload.correo
                                usuario = usuario |} >=> setStatusCode (201)) next ctx
            | None -> return! setStatusCode (400) next ctx
        | Error err ->
            printfn "%O" err

            return! setStatusCode 400 next ctx
    }

let private olvideContrasena (next: HttpFunc) (ctx: HttpContext) = task { return! (text "Forgot!") next ctx }

let rutas =
    router {
        post "/login" inicioSesion
        post "/signup" registrarse
        post "/forgot" olvideContrasena
    }
