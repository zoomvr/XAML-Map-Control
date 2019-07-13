// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// � 2019 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#else
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace MapControl
{
    public static partial class ImageLoader
    {
        /// <summary>
        /// The System.Net.Http.HttpClient instance used when image data is downloaded via a http or https Uri.
        /// </summary>
        public static HttpClient HttpClient { get; set; } = new HttpClient();


        public static async Task<ImageSource> LoadImageAsync(Uri uri)
        {
            ImageSource image = null;

            try
            {
                if (!uri.IsAbsoluteUri || uri.Scheme == "file")
                {
                    image = await LoadImageAsync(uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString);
                }
                else if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    var response = await GetHttpResponseAsync(uri);

                    if (response?.Stream != null)
                    {
                        using (var stream = response.Stream)
                        {
                            image = await LoadImageAsync(stream);
                        }
                    }
                }
                else
                {
                    image = new BitmapImage(uri);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ImageLoader: {0}: {1}", uri, ex.Message);
            }

            return image;
        }

        internal static async Task<HttpResponse> GetHttpResponseAsync(Uri uri, bool continueOnCapturedContext = true)
        {
            HttpResponse response = null;

            try
            {
                using (var responseMessage = await HttpClient.GetAsync(uri).ConfigureAwait(continueOnCapturedContext))
                {
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        response = await HttpResponse.Create(responseMessage, continueOnCapturedContext);
                    }
                    else
                    {
                        Debug.WriteLine("ImageLoader: {0}: {1} {2}", uri, (int)responseMessage.StatusCode, responseMessage.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ImageLoader: {0}: {1}", uri, ex.Message);
            }

            return response;
        }

        internal class HttpResponse
        {
            public MemoryStream Stream { get; private set; }
            public TimeSpan? MaxAge { get; private set; }

            internal static async Task<HttpResponse> Create(HttpResponseMessage message, bool continueOnCapturedContext)
            {
                var response = new HttpResponse();
                IEnumerable<string> tileInfo;

                if (!message.Headers.TryGetValues("X-VE-Tile-Info", out tileInfo) || !tileInfo.Contains("no-tile"))
                {
                    response.Stream = new MemoryStream();
                    await message.Content.CopyToAsync(response.Stream).ConfigureAwait(continueOnCapturedContext);
                    response.Stream.Seek(0, SeekOrigin.Begin);
                    response.MaxAge = message.Headers.CacheControl?.MaxAge;
                }

                return response;
            }
        }
    }
}