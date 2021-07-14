using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Services
{
    public static class FileUploader
    {
        // https://riptutorial.com/dot-net/example/32520/upload-file-with-webrequest

        /// <summary>
        /// Uploads the specified file.
        /// </summary>
        /// <param name="uri">The target URI.</param>
        /// <param name="path">The file path.</param>
        /// <param name="token">The optional bearer token.</param>
        /// <param name="postData">The additional post data.</param>
        /// <returns>Response.</returns>
        /// <exception cref="ArgumentNullException">url or path</exception>
        public static async Task<string> UploadFile(string uri, string path,
            string token, Dictionary<string, object> postData = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            // boundary will separate each parameter
            string boundary = $"{Guid.NewGuid():N}";
            request.ContentType =
                $"multipart/form-data; {nameof(boundary)}={boundary}";
            request.Method = "POST";

            using (Stream requestStream = request.GetRequestStream())
            using (StreamWriter writer = new StreamWriter(requestStream))
            {
                // put all POST data into request
                if (postData?.Count > 0)
                {
                    foreach (KeyValuePair<string, object> data in postData)
                    {
                        await writer.WriteAsync(
                              $"\r\n--{boundary}\r\nContent-Disposition: " +
                              $"form-data; name=\"{data.Key}\"\r\n\r\n{data.Value}");
                    }
                }

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
            if (responseStream == null) return string.Empty;
            using var reader = new StreamReader(responseStream);
            return await reader.ReadToEndAsync();
        }
    }
}
