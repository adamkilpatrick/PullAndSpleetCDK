namespace PullAndSpleetCdk
open Amazon.CDK.AWS.ECR
open Amazon.CDK
open Amazon.JSII.Runtime.Deputy

type PullAndSpleetCdkStackProps(ecrRepoArn: string, ecrRepoName: string, imageTagParameterName) =
    inherit DeputyBase()
    interface IStackProps
    member this.ecrRepoArn: string = ecrRepoArn
    member this.ecrRepoName: string = ecrRepoName
    member this.imageTagParameterName: string = imageTagParameterName