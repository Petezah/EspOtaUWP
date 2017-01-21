using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;

namespace EspOtaUWP
{
    public static class OtaUpdater
    {
        public static IAsyncOperationWithProgress<HttpStatusCode, HttpProgress> PostFirmwareAsync(
            StorageFile firmware,
            Uri uri,
            string username,
            string password)
        {
            return AsyncInfo.Run<HttpStatusCode, HttpProgress>(
                (cancellationToken, progress) => PostFirmwareAsyncInternal(
                    firmware,
                    uri,
                    username,
                    password,
                    progress));
        }

        private static async Task<HttpStatusCode> PostFirmwareAsyncInternal(
            StorageFile firmware,
            Uri uri,
            string username,
            string password,
            IProgress<HttpProgress> progressProvider)
        {
            using (var firmwareStream = await firmware.OpenAsync(FileAccessMode.Read))
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
                    {
                        var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                        var content = new HttpMultipartFormDataContent(boundary);
                        content.Headers.Remove("Content-Type");
                        content.Headers.TryAppendWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                        content.Add(new HttpStreamContent(firmwareStream), "file", firmware.Name);

                        request.Content = content;
                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                        {
                            var authentication = string.Format("{0}:{1}", username, password);
                            var encodedAuthentication = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authentication));
                            request.Headers.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Basic", encodedAuthentication);
                        }

                        var httpTaskWithProgress = client.SendRequestAsync(request);
                        httpTaskWithProgress.Progress += (asyncInfo, progress) => progressProvider.Report(progress);
                        var result = await httpTaskWithProgress;

                        Debug.WriteLine("Status {0}", result.StatusCode);
                        return result.StatusCode;
                    }
                }
            }
        }
    }
}
