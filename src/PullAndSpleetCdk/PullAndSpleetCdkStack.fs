namespace PullAndSpleetCdk

open Amazon.CDK
open Amazon.CDK.AWS.Lambda
open Amazon.CDK.AWS.S3
open Amazon.CDK.AWS.ECR

type PullAndSpleetCdkStack(scope, id, props: PullAndSpleetCdkStackProps) as this =
    inherit Stack(scope, id, props)
    let audioBucket = new Bucket(this, "AudioBucket")
    
    let functionProps = 
        let initFunctionProps = new FunctionProps()
        initFunctionProps.Runtime <- Runtime.FROM_IMAGE
        let repoAttributes = new RepositoryAttributes()
        repoAttributes.RepositoryArn <- props.ecrRepoArn
        repoAttributes.RepositoryName <- props.ecrRepoName
        initFunctionProps.Code <- Code.FromEcrImage(Repository.FromRepositoryAttributes(this, "EcrRepo", repoAttributes))
        initFunctionProps.Handler <- Handler.FROM_IMAGE
        initFunctionProps.Timeout <- Duration.Minutes(10.0)
        initFunctionProps
    let pullAndSpleetFunction = 
        let lambdaFunction = new Function(this, "PullAndSpleetFunction", functionProps)
        audioBucket.GrantWrite(lambdaFunction.Role) |> ignore
        lambdaFunction
    
    
    // The code that defines your stack goes here
