namespace PullAndSpleetCdk

open Amazon.CDK
open Amazon.CDK.Pipelines
open Amazon.CDK.AWS.SSM
open Amazon.CDK.AWS.CodeBuild

type PipelineStack(scope, id, props) as this =
    inherit Stack(scope, id, props)

    let shellStepProps = 
        let connectionSourceOptions = new ConnectionSourceOptions()
        connectionSourceOptions.ConnectionArn <- StringParameter.ValueForStringParameter(this, "GITHUB_CONNECTION_ARN")
        let initShellStepProps = new ShellStepProps()
        initShellStepProps.Input <- CodePipelineSource.Connection("adamkilpatrick/PullAndSpleetCDK","main",connectionSourceOptions)
        initShellStepProps.InstallCommands <- [|"npm install -g aws-cdk"|]
        initShellStepProps.Commands <- [|"cdk synth"|]
        initShellStepProps
    let shellStep = new ShellStep("PipelineShellStep",shellStepProps)

    let codeBuildOptions = 
        let initCodeBuildOptions = new CodeBuildOptions()
        let buildEnvironment = new BuildEnvironment()
        buildEnvironment.BuildImage <- LinuxBuildImage.STANDARD_6_0
        initCodeBuildOptions.BuildEnvironment <- buildEnvironment
        initCodeBuildOptions
    let pipelineProps: CodePipelineProps = 
        let initPipelineProps = new CodePipelineProps()
        initPipelineProps.SelfMutation <- true
        initPipelineProps.DockerEnabledForSelfMutation <- true
        initPipelineProps.DockerEnabledForSynth <- true
        initPipelineProps.PipelineName <- "PullAndSpleetPipeline"
        initPipelineProps.Synth <- shellStep
        initPipelineProps.SynthCodeBuildDefaults <- codeBuildOptions
        initPipelineProps

    let pipeline = 
        let initPipeline = new CodePipeline(this, "Pipeline", pipelineProps)
        initPipeline
    