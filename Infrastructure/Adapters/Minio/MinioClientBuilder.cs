using System;
using Minio;

namespace PikaCore.Infrastructure.Adapters.Minio;

internal class MinioClientBuilder
{
    private static MinioClientBuilder? _minioClientBuilder;
    private static MinioClient? _minioClient;
    private MinioClientBuilder(){}

    internal static MinioClientBuilder Instance()
    {
        return _minioClientBuilder ??= new MinioClientBuilder();
    }

    internal MinioClientBuilder CreateClientInstance()
    {
        _minioClient ??= new MinioClient();

        return this;
    }

    internal MinioClient ConfigureClient(string endpoint, string clientId, string password)
    {
        if (_minioClient == null)
        {
            throw new InvalidOperationException("INIT: Client is null, please, create its instance first");
        }
        return _minioClient
            .WithEndpoint(endpoint)
            .WithCredentials(clientId, password)
            .Build();
    }
}