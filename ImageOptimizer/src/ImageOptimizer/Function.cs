using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Util;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizer
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; } = new AmazonS3Client();

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
            foreach (var record in eventRecords)
            {
                var s3Event = record.S3;
                if (s3Event == null)
                {
                    continue;
                }

                try
                {
                    var response = await S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                    // context.Logger.LogInformation(response.Headers.ContentType);

                    if (response.Metadata["x-amz-meta-resized"] == true.ToString())
                    {
                        context.Logger.LogInformation($"Item with key {s3Event.Object.Key} is already resized");
                        continue;
                    }

                    await using var itemStream = await S3Client.GetObjectStreamAsync(s3Event.Bucket.Name,
                        s3Event.Object.Key,
                        new Dictionary<string, object>());
                    using var outStream = new MemoryStream();
                    using(var image = await Image.LoadAsync(itemStream))
                    {
                        image.Mutate(x => x.Resize(500, 500, LanczosResampler.Lanczos3).GaussianBlur());
                        //To Blur use the blur option
                        //image.Mutate(x=>x.BokehBlur())
                        var originalName = response.Metadata["x-amz-meta-originalname"];
                        await image.SaveAsync(outStream, image.DetectEncoder(originalName));
                    }

                    await S3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = s3Event.Bucket.Name,
                        Key = s3Event.Object.Key,
                        Metadata =
                        {
                           ["x-amz-meta-originalname"] = response.Metadata["x-amz-meta-originalname"],
                           ["x-amz-meta-extension"] =response.Metadata["x-amz-meta-extension"],
                           ["x-amzz-meta-resized"]=true.ToString()
                        },
                        ContentType = response.Headers.ContentType,
                        InputStream = outStream
                    });
                    context.Logger.LogInformation($"Resized image with key:{s3Event.Object.Key}");

                }
                catch (Exception e)
                {
                    context.Logger.LogError($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                    context.Logger.LogError(e.Message);
                    context.Logger.LogError(e.StackTrace);
                    throw;
                }
            }
        }
    }
}