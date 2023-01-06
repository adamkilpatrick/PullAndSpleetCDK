open Amazon.CDK
open PullAndSpleetCdk

type DeployStage(scope, id, pullAndSpleetCdkStackProps:PullAndSpleetCdkStackProps) as this =
    inherit Stage(scope, id, new StageProps())

    do new PullAndSpleetCdkStack(this, "PullAndSpleetCdkStack", pullAndSpleetCdkStackProps) |> ignore

[<EntryPoint>]
let main _ =
    let app = App(null)

    
    let pipelineStack = PipelineStack(app, "PipelineStack", StackProps())


    let deployStage = new DeployStage(app, "DeployStage", new PullAndSpleetCdkStackProps(pipelineStack.ecrRepoArn.ImportValue, pipelineStack.ecrRepoName.ImportValue))
    
    pipelineStack.pipeline.AddStage(deployStage) |> ignore
    app.Synth() |> ignore
    0
