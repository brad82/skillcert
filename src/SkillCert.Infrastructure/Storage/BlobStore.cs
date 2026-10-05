using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SkillCert.Infrastructure.Storage;

/// <summary>Where archived training-record PDFs live (spec §22): RustFS locally, DigitalOcean Spaces on the demo.</summary>
public interface IBlobStore
{
    /// <summary>False when no S3 settings are given (e.g. the CI stack); archival then skips its run.</summary>
    bool IsConfigured { get; }

    Task PutAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken);

    /// <returns>Null when nothing is stored at <paramref name="path"/>.</returns>
    Task<byte[]?> GetAsync(string path, CancellationToken cancellationToken);
}

/// <summary>S3 settings (section "S3"): ServiceUrl, AccessKey, SecretKey, Bucket and optional Region.</summary>
public sealed record S3Options(string ServiceUrl, string AccessKey, string SecretKey, string Bucket, string Region)
{
    public static S3Options? From(IConfiguration configuration)
    {
        var section = configuration.GetSection("S3");
        string? Value(string key) => string.IsNullOrWhiteSpace(section[key]) ? null : section[key];
        return Value("ServiceUrl") is { } url && Value("AccessKey") is { } access && Value("SecretKey") is { } secret && Value("Bucket") is { } bucket
            ? new S3Options(url, access, secret, bucket, Value("Region") ?? "us-east-1")
            : null;
    }
}

/// <summary>Any S3-compatible store. Creates the bucket on first write when it doesn't exist (RustFS starts empty).</summary>
public sealed class S3BlobStore(S3Options options) : IBlobStore, IDisposable
{
    private readonly AmazonS3Client _client = new(
        new BasicAWSCredentials(options.AccessKey, options.SecretKey),
        new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            AuthenticationRegion = options.Region,
            ForcePathStyle = true,
            // S3-compatible stores (RustFS, Spaces) don't all accept the SDK's newer default checksums.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        });

    private bool _bucketChecked;

    public bool IsConfigured => true;

    public async Task PutAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(cancellationToken);
        using var stream = new MemoryStream(content);
        await _client.PutObjectAsync(
            new PutObjectRequest { BucketName = options.Bucket, Key = path, InputStream = stream, ContentType = contentType },
            cancellationToken);
    }

    public async Task<byte[]?> GetAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(options.Bucket, path, cancellationToken);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketChecked)
        {
            return;
        }

        if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, options.Bucket))
        {
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = options.Bucket }, cancellationToken);
        }

        _bucketChecked = true;
    }
}

/// <summary>Stands in when no S3 settings are given. Reads find nothing; writes are a programming error.</summary>
public sealed class UnconfiguredBlobStore : IBlobStore
{
    public bool IsConfigured => false;

    public Task PutAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Blob storage isn't configured (S3 settings are missing).");

    public Task<byte[]?> GetAsync(string path, CancellationToken cancellationToken) => Task.FromResult<byte[]?>(null);
}

public static class BlobStoreServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStore(this IServiceCollection services, IConfiguration configuration)
    {
        if (S3Options.From(configuration) is { } options)
        {
            services.AddSingleton<IBlobStore>(_ => new S3BlobStore(options));
        }
        else
        {
            services.AddSingleton<IBlobStore, UnconfiguredBlobStore>();
        }

        return services;
    }
}
