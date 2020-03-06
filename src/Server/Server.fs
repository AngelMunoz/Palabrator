open Saturn

let jsonpipeline = pipeline {
    plug acceptJson
}

let webApp = router {
    pipe_through jsonpipeline

    forward "/auth" AccionesAuth.rutas
    forward "/api/perfiles" Perfiles.rutas
    forward "/api/palabras" Palabras.rutas
}

let app = application {
    url ("http://0.0.0.0:" + Utils.port.ToString() + "/")
    use_router webApp
    memory_cache
    use_gzip
    use_cors "CORS_Policy" (fun builder -> 
                                if Utils.isProd then 
                                    builder.WithOrigins [| "palabrator-api.herokuapp.com" |]
                                else
                                    builder
                                        .AllowAnyOrigin()
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                |> ignore
                                ())
    use_jwt_authentication Utils.secretKey Utils.issuer
}

run app
