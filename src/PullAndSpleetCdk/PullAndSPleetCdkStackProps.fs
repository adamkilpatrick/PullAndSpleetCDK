namespace PullAndSpleetCdk
open Amazon.CDK.AWS.ECR
open Amazon.CDK
open Amazon.JSII.Runtime.Deputy

type PullAndSpleetCdkStackProps(ecrRepoArn: string, ecrRepoName: string) =
    inherit DeputyBase()
    interface IStackProps
    member this.ecrRepoArn: string = ecrRepoArn
    member this.ecrRepoName: string = ecrRepoName