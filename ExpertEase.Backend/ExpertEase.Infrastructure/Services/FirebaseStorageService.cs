using ExpertEase.Application.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;

namespace ExpertEase.Infrastructure.Services;

public class FirebaseStorageService: IFirebaseStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName = "uniproject-38b1d.firebasestorage.app";
    
    public FirebaseStorageService(IConfiguration configuration)
    {
        var credentialPath = configuration["Firebase:PrivateKey"];
    
        if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
        {
            throw new InvalidOperationException("Firebase credential path is not set or file does not exist.");
        }

        var credential = GoogleCredential.FromFile(credentialPath);
        _storageClient = StorageClient.Create(credential);
    }

    
    public async Task<string> UploadImageAsync(Stream stream, string folder, string fileName, string contentType)
    {
        var objectName = $"{folder}/{fileName}";
        var uploaded = await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, stream);
        uploaded.Acl = new List<ObjectAccessControl>
        {
            new ObjectAccessControl { Entity = "allUsers", Role = "READER" }
        };
        await _storageClient.UpdateObjectAsync(uploaded);
        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }
    
    public async Task DeleteImageAsync(string objectName, CancellationToken cancellationToken = default)
    {
        await _storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
    }
}