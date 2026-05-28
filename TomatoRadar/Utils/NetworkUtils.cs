using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TomatoRadar.Utils
{
    static internal class NetworkUtils
    {
        static readonly HttpClient hc = new()
        {
            Timeout = TimeSpan.FromMilliseconds(20000)
        };

        public static void InitializeHttpClient()
        {
            hc.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        public static async Task<string> HttpGet(string url)
        {
            try
            {
                using HttpResponseMessage response = await hc.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }

        public static async Task<string> HttpPost(string url, string content, string mediaType)
        {
            try
            {
                using HttpResponseMessage response = await hc.PostAsync(url, new StringContent(content, Encoding.UTF8, mediaType));
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }

        public static async Task<string> HttpDownloadFile(string url, string filename, IProgress<double>? progress = null)
        {
            try
            {
                using HttpResponseMessage response = await hc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                using FileStream fs = new(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                if (progress == null)
                {
                    await response.Content.CopyToAsync(fs);
                }
                else
                {
                    long? totalBytes = response.Content.Headers.ContentLength;
                    using Stream contentStream = await response.Content.ReadAsStreamAsync();
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            progress.Report((double)totalRead / totalBytes.Value * 100);
                        }
                    }
                }

                return filename;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("HttpRequestFailed", ex);
            }
        }
    }
}
