open Amazon.CDK
open PullAndSpleetCdk

[<EntryPoint>]
let main _ =
    let app = App(null)

    PullAndSpleetCdkStack(app, "PullAndSpleetCdkStack", StackProps()) |> ignore
    PipelineStack(app, "PipelineStack", StackProps()) |> ignore
    app.Synth() |> ignore
    0
