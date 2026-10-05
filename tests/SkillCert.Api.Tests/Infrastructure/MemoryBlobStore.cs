using System.Collections.Concurrent;
using SkillCert.Infrastructure.Storage;

namespace SkillCert.Api.Tests.Infrastructure;

/// <summary>Blob storage in memory, with a switch to make the next upload fail.</summary>
public sealed class MemoryBlobStore : IBlobStore
{
    public ConcurrentDictionary<string, byte[]> Blobs { get; } = [];

    public bool FailNextPut { get; set; }

    public bool IsConfigured => true;

    public Task PutAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        if (FailNextPut)
        {
            FailNextPut = false;
            throw new IOException("Simulated storage outage.");
        }

        Blobs[path] = content;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetAsync(string path, CancellationToken cancellationToken) => Task.FromResult(Blobs.GetValueOrDefault(path));
}
