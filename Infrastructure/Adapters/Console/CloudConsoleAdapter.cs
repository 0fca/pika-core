using System;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Pika.Domain.Storage.Entity.View;

namespace PikaCore.Infrastructure.Adapters.Console;

public sealed class CloudConsoleAdapter : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly TcpClient _client;
    
    public CloudConsoleAdapter(IConfiguration configuration)
    {
        this._configuration = configuration;
        this._client = new TcpClient(_configuration.GetConnectionString("QuoyRelayServer").Split(":")[0],
            int.Parse(_configuration.GetConnectionString("QuoyRelayServer").Split(":")[1]));
    }
    
    public string ExecuteCommand(CommandsView command)
    {
        var s = command.ToString();
        return this.SendTcpCmdPacket(s);
    }

    private string SendTcpCmdPacket(string command)
    {
        var commandAsBytes = System.Text.Encoding.ASCII.GetBytes(command.Trim());
        var stream = this._client.GetStream();
        if (stream.CanWrite)
        {
            stream.Write(commandAsBytes);
        }
        if (!stream.CanRead) return string.Empty;
        var data = new byte[4096];
        var b = stream.Read(data, 0, data.Length);
        var responseData = System.Text.Encoding.ASCII.GetString(data, 0, b);

        this.ProperlyClose(); 
        return responseData.TrimEnd();
    }

    private void ProperlyClose()
    {
        var stream = this._client.GetStream();
        var bytes = new ReadOnlySpan<byte>(new byte[]{255});
        if (stream.CanWrite)
        {
            stream.Write(bytes);
        }
        _client.Close();
    }

    public void Dispose()
    {
        _client.Close();
    }
}