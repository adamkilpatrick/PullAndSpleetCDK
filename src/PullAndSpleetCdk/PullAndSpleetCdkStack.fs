namespace PullAndSpleetCdk

open Amazon.CDK

type PullAndSpleetCdkStack(scope, id, props) as this =
    inherit Stack(scope, id, props)
    
    // The code that defines your stack goes here