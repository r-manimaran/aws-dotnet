using Amazon.S3.Model;

namespace ImageUpload.Services;

    public interface ICustomerImageService
    {
        Task<PutObjectResponse> UploadImageAsync(Guid id, IFormFile file);
        Task<GetObjectResponse?> GetImageAsyc(Guid id);
        Task<DeleteObjectResponse?> DeleteImageAsync(Guid id);

    }

