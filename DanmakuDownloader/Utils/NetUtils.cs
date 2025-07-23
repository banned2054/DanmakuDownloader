﻿using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DanmakuDownloader.Utils;

internal class NetUtils
{
    public static async Task<string> FetchAsync(string                      url,
                                                Dictionary<string, string>? headers     = null,
                                                bool                        enableProxy = false,
                                                object?                     body        = null)
    {
        var client  = CreateRestClient(enableProxy);
        var request = CreateRestRequest(url, headers);

        if (body != null)
        {
            request.Method = Method.Post;

            // 如果传的是字节流，认为是二进制上传
            if (body is byte[] bytes)
            {
                request.AddBody(bytes);
            }
            else
            {
                // 默认序列化为 JSON
                var json = JsonConvert.SerializeObject(body);
                request.AddStringBody(json, DataFormat.Json);
            }
        }

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.RawBytes == null)
        {
            throw new HttpRequestException($"Fetch failed: {url}, Status: {response.StatusCode}");
        }

        var respBytes = response.RawBytes;

        var result = Encoding.UTF8.GetString(respBytes);
        return result;
    }

    private static RestClient CreateRestClient(bool enableProxy)
    {
        var options = new RestClientOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        };

        var proxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (!enableProxy || string.IsNullOrWhiteSpace(proxy) || !IsProxyAvailable(proxy))
            return new RestClient(options);
        var proxyUri = new Uri(proxy);
        options.Proxy = new WebProxy(proxyUri.Host, proxyUri.Port);

        return new RestClient(options);
    }

    private static RestRequest CreateRestRequest(string url, Dictionary<string, string>? headers = null)
    {
        var request = new RestRequest(url.Trim());

        if (headers == null) return request;
        foreach (var (key, value) in headers)
        {
            request.AddHeader(key, value);
        }

        return request;
    }

    private static bool IsProxyAvailable(string proxy)
    {
        try
        {
            var       uri    = new Uri(proxy);
            using var client = new TcpClient();
            var       task   = client.ConnectAsync(uri.Host, uri.Port);
            return task.Wait(1000) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}