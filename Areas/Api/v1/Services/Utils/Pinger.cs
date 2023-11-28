using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PikaCore.Areas.Api.v1.Services.Utils;

public static class Pinger
{
    public static async Task<bool> Ping(string address, int port)
    {
        var httpClient = new HttpClient();
        var r = await httpClient.GetAsync($"http://{address}:{port}/core/health");
        if (r.StatusCode == HttpStatusCode.OK) return await r.Content.ReadAsStringAsync() == "Healthy";

        return false;
    }
}