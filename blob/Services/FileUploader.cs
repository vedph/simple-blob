using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleBlob.Cli.Services;

public static class FileUploader
{
    // https://itnext.io/consuming-a-multipart-form-data-rest-method-with-outsystems-c466e404118d
    // https://riptutorial.com/dot-net/example/32520/upload-file-with-webrequest

    /// <summary>
    /// Uploads the specified file.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="path">The file path.</param>
    /// <param name="token">The optional bearer token.</param>
    /// <param name="mimeType">The media type.</param>
    /// <returns>Response.</returns>
    /// <exception cref="ArgumentNullException">url or path</exception>
    public static async Task<string> UploadFile(string uri,
        string path, string? token, string id, string mimeType)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (id == null) throw new ArgumentNullException(nameof(id));
        if (mimeType == null) throw new ArgumentNullException(nameof(mimeType));

        // https://makolyte.com/csharp-how-to-send-a-file-with-httpclient/
        // TODO: replace obsolete WebRequest with HttpClient request
        // using a code like the commented one, which results in an empty IFormFile
        /*
        using MultipartFormDataContent mfc = new();

        // mimeType
        mfc.Add(new StringContent(mimeType), name: "mimeType");
        // id
        mfc.Add(new StringContent(id), name: "id");
        // content
        StreamContent content = new(File.OpenRead(path));
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        mfc.Add(content, name: id, fileName: Path.GetFileName(path));

        HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.PostAsync(uri, mfc);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
        */

        HttpWebRequest request = WebRequest.CreateHttp(uri);
        // boundary will separate each parameter
        string boundary = $"{Guid.NewGuid():N}";
        request.ContentType =
            $"multipart/form-data; {nameof(boundary)}={boundary}";
        request.Method = "POST";

        using (Stream requestStream = request.GetRequestStream())
        using (StreamWriter writer = new(requestStream))
        {
            // put all POST data into request
            await writer.WriteAsync($"\r\n--{boundary}\r\nContent-Disposition: " +
                  $"form-data; name=\"id\"\r\n\r\n{id}");
            await writer.WriteAsync($"\r\n--{boundary}\r\nContent-Disposition: " +
                  $"form-data; name=\"mimeType\"\r\n\r\n{mimeType}");

            // token if any
            if (token != null)
                request.Headers.Add("Authorization", "Bearer " + token);

            // file header
            await writer.WriteAsync(
                $"\r\n--{boundary}\r\nContent-Disposition: " +
                "form-data; name=\"File\"; " +
                $"filename=\"{Path.GetFileName(path)}\"\r\n" +
                "Content-Type: application/octet-stream\r\n\r\n");

            await writer.FlushAsync();
            using (FileStream fileStream = File.OpenRead(path))
                await fileStream.CopyToAsync(requestStream);

            await writer.WriteAsync($"\r\n--{boundary}--\r\n");
        }

        using HttpWebResponse response = (HttpWebResponse)
            await request.GetResponseAsync();
        using Stream responseStream = response.GetResponseStream();
        if (responseStream == null) return "";
        using var reader = new StreamReader(responseStream);
        return await reader.ReadToEndAsync();
    }
}
