namespace PullAndSpleetCdk

open Amazon.CDK
open Amazon.CDK.Pipelines
open Amazon.CDK.AWS.SSM
open Amazon.CDK.AWS.CodeBuild
open Amazon.CDK.AWS.IAM
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.ECR

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
        initPipelineProps.AssetPublishingCodeBuildDefaults <- codeBuildOptions
        initPipelineProps
    let ecrRepoProps = 
        let lifeCycleRule = new LifecycleRule()
        lifeCycleRule.MaxImageCount <- 5
        lifeCycleRule.RulePriority <- 1
        let initEcrRepoProps = new RepositoryProps()
        initEcrRepoProps.LifecycleRules <- [|lifeCycleRule|]
        initEcrRepoProps
    let ecrRepo = new Repository(this, "PullAndSpleetECR", ecrRepoProps);

    let imageTagParameter =
        let stringParameterProps = new StringParameterProps()
        stringParameterProps.ParameterName <- "PULLANDSPLEET_IMAGE_TAG"
        stringParameterProps.StringValue <- "EMPTY"
        let tagParameter = new StringParameter(this, "EcrTagParameter", stringParameterProps)
        tagParameter

    let codeBuildStepProps = 

        let initCodeBuildStepProps = new CodeBuildStepProps()
        let connectionSourceOptions = new ConnectionSourceOptions()
        connectionSourceOptions.ConnectionArn <- StringParameter.ValueForStringParameter(this, "GITHUB_CONNECTION_ARN")
        let buildCacheBucket = new Bucket(this, "DockerBuildCacheBucket")
        let buildEnvironment = new BuildEnvironment()
        buildEnvironment.BuildImage <- LinuxBuildImage.AMAZON_LINUX_2_4
        buildEnvironment.Privileged <- true
        initCodeBuildStepProps.BuildEnvironment <- buildEnvironment
        initCodeBuildStepProps.Cache <- Cache.Bucket(buildCacheBucket)
        initCodeBuildStepProps.Input <- CodePipelineSource.Connection("adamkilpatrick/PullAndSpleet","main",connectionSourceOptions)
        initCodeBuildStepProps.Commands <- [|
            "aws ecr get-login-password --region $AWS_DEFAULT_REGION | docker login --username AWS --password-stdin "+ecrRepo.RepositoryUri;
            "REPOSITORY_URI="+ecrRepo.RepositoryUri;
            "COMMIT_HASH=$(echo $CODEBUILD_RESOLVED_SOURCE_VERSION | cut -c 1-7)";
            "IMAGE_TAG=${COMMIT_HASH:=latest}";
            "ls -la";
            "cd ./PullAndSpleet";
            "ls -la";
            "docker pull $REPOSITORY_URI:latest";
            "docker build --cache-from $REPOSITORY_URI:latest -t $REPOSITORY_URI:latest .";
            "docker tag $REPOSITORY_URI:latest $REPOSITORY_URI:$IMAGE_TAG";
            "docker push $REPOSITORY_URI:latest";
            "docker push $REPOSITORY_URI:$IMAGE_TAG";
            "aws ssm put-parameter --name PULLANDSPLEET_IMAGE_TAG --value $IMAGE_TAG --overwrite"
        |]
        let parameterStatementProps = new PolicyStatementProps()
        parameterStatementProps.Actions <- [|
            "ssm:PutParameter";
        |]
        parameterStatementProps.Effect <- Effect.ALLOW
        parameterStatementProps.Resources <- [|imageTagParameter.ParameterArn|]
        let cacheBucketStatementProps = new PolicyStatementProps()
        cacheBucketStatementProps.Actions <- [|
            "s3:PutObject";
            "s3:GetObject";
            "s3:GetObjectVersion";
            "s3:GetBucketAcl";
            "s3:GetBucketLocation";
        |]
        cacheBucketStatementProps.Effect <- Effect.ALLOW
        cacheBucketStatementProps.Resources <- [|buildCacheBucket.BucketArn; buildCacheBucket.BucketArn+"/*"|]
        let authTokenStatementProps = new PolicyStatementProps()
        authTokenStatementProps.Actions <- [|"ecr:GetAuthorizationToken";|]
        authTokenStatementProps.Effect <- Effect.ALLOW
        authTokenStatementProps.Resources <- [|"*"|]
        let policyStatementProps = new PolicyStatementProps()
        policyStatementProps.Actions <- [|
            "ecr:BatchCheckLayerAvailability";
            "ecr:GetDownloadUrlForLayer";
            "ecr:BatchGetImage";
            "ecr:TagResource";
            "ecr:PutImage"
            "ecr:InitiateLayerUpload";
            "ecr:UploadLayerPart";
            "ecr:CompleteLayerUpload"
        |]
        policyStatementProps.Effect <- Effect.ALLOW
        policyStatementProps.Resources <- [|ecrRepo.RepositoryArn|]
        initCodeBuildStepProps.RolePolicyStatements <- [|
            new PolicyStatement(policyStatementProps);
            new PolicyStatement(authTokenStatementProps);
            new PolicyStatement(parameterStatementProps);
        |]
        initCodeBuildStepProps
    
    let codeBuildStep = 
        let initcodeBuildStep = new CodeBuildStep("DockerPushStep", codeBuildStepProps)
        initcodeBuildStep

    member this.imageTagParameterArn = 
        let cfnOutputProps = new CfnOutputProps()
        cfnOutputProps.ExportName <- "IMAGE-TAG-PARAM"
        cfnOutputProps.Value <- imageTagParameter.ParameterArn
        let cfnOutput = new CfnOutput(this, "ImageTagParam", cfnOutputProps)
        cfnOutput
    
    member this.ecrRepoArn = 
        let cfnOutputProps = new CfnOutputProps()
        cfnOutputProps.ExportName <- "ECR-REPO-ARN"
        cfnOutputProps.Value <- ecrRepo.RepositoryArn
        let cfnOutput = new CfnOutput(this, "EcrRepoArn", cfnOutputProps)
        cfnOutput
    member this.ecrRepoName = 
        let cfnOutputProps = new CfnOutputProps()
        cfnOutputProps.ExportName <- "ECR-REPO-NAME"
        cfnOutputProps.Value <- ecrRepo.RepositoryName
        let cfnOutput = new CfnOutput(this, "EcrRepoName", cfnOutputProps)
        cfnOutput
    member this.ecrRepository = ecrRepo
    member this.pipeline = 
        let initPipeline = new CodePipeline(this, "Pipeline", pipelineProps)
        let buildWave = initPipeline.AddWave("BuildWave")
        buildWave.AddPre(codeBuildStep)
        initPipeline
    