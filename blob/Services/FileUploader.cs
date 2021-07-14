using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Services
{
    public static class FileUploader
    {
        // https://riptutorial.com/dot-net/example/32520/upload-file-with-webrequest

        public static async Task<string> UploadFile(string url, string filename,
            Dictionary<string, object> postData)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            // boundary will separate each parameter
            string boundary = $"{Guid.NewGuid():N}";
            request.ContentType =
                $"multipart/form-data; {nameof(boundary)}={boundary}";
            request.Method = "POST";

            using (var requestStream = request.GetRequestStream())
            using (var writer = new StreamWriter(requestStream))
            {
                // put all POST data into request
                foreach (KeyValuePair<string, object> data in postData)
                {
                    await writer.WriteAsync(
                          $"\r\n--{boundary}\r\nContent-Disposition: " +
                          $"form-data; name=\"{data.Key}\"\r\n\r\n{data.Value}");
                }

                // file header
                await writer.WriteAsync(
                    $"\r\n--{boundary}\r\nContent-Disposition: " +
                    $"form-data; name=\"File\"; " +
                    $"filename=\"{Path.GetFileName(filename)}\"\r\n" +
                    "Content-Type: application/octet-stream\r\n\r\n");

                await writer.FlushAsync();
                using (FileStream fileStream = File.OpenRead(filename))
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
