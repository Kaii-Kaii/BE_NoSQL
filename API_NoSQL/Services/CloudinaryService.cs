using API_NoSQL.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace API_NoSQL.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;

        public CloudinaryService(IOptions<CloudinarySettings> options)
        {
            _settings = options.Value;
            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new Cloudinary(account)
            {
                Api = { Secure = true }
            };
        }

        public async Task<string> UploadImageAsync(IFormFile file, string? folder = null)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("Empty file", nameof(file));

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder) ? _settings.Folder : folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            {
                return result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? throw new InvalidOperationException("Upload returned no URL");
            }

            throw new InvalidOperationException($"Upload failed: {result.Error?.Message}");
        }
    }
}
