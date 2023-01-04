namespace PullAndSpleetCdk

open Amazon.CDK
open Amazon.CDK.Pipelines
open Amazon.CDK.AWS.SSM

type PipelineStack(scope, id, props) as this =
    inherit Stack(scope, id, props)

    let foo = CodePipelineSource.Connection
    let shellStepProps = 
        let connectionSourceOptions = new ConnectionSourceOptions()
        connectionSourceOptions.ConnectionArn <- StringParameter.ValueForStringParameter(this, "GITHUB_CONNECTION_ARN")
        let initShellStepProps = new ShellStepProps()
        initShellStepProps.Input <- CodePipelineSource.Connection("adamkilpatrick/PullAndSpleetCDK","main",connectionSourceOptions)
        initShellStepProps.Commands <- [|"npm ci"; "npm run build"; "npx cdk synth"|]
        initShellStepProps
    let shellStep = new ShellStep("PipelineShellStep",shellStepProps)

    let pipelineProps: CodePipelineProps = 
        let initPipelineProps = new CodePipelineProps()
        initPipelineProps.SelfMutation <- true
        initPipelineProps.DockerEnabledForSelfMutation <- true
        initPipelineProps.DockerEnabledForSynth <- true
        initPipelineProps.PipelineName <- "PullAndSpleetPipeline"
        initPipelineProps.Synth <- shellStep
        initPipelineProps

    let pipeline = 
        let initPipeline = new CodePipeline(this, "Pipeline", pipelineProps)
        initPipeline
    