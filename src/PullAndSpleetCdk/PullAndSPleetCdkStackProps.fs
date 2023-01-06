namespace PullAndSpleetCdk
open Amazon.CDK.AWS.ECR
open Amazon.CDK
open Amazon.JSII.Runtime.Deputy

type PullAndSpleetCdkStackProps(ecrRepo: string) =
    inherit DeputyBase()
    interface IStackProps
    member this.ecrRepo: string= ecrRepo