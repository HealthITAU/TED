using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TED.Utils
{
    /// <summary>
    /// Provides utility methods for working with files
    /// </summary>
    internal class FileUtilities
    {
        /// <summary>
        /// Downloads a file from a specified URL and saves it to a cache.
        /// If the file already exists in the cache, the function returns the path to the cached file.
        /// If the file does not exist in the cache, the function downloads the file, saves it to the cache, and then returns the path to the cached file.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the path to the downloaded (or cached) file.</returns>
        public static async Task<string> DownloadAndCacheFileAsync(string url)
        {
            using (var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10)
            })
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var etag = response.Headers.ETag?.Tag.Replace("\"", string.Empty) ?? "untagged";
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), "TED")))
                {
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "TED"));
                }
                var path = Path.Combine(Path.GetTempPath(), "TED", $"{etag}.png");

                if (File.Exists(path))
                {
                    return path;
                }

                var filesToDelete = Directory.GetFiles(Path.Combine(Path.GetTempPath(), "TED"), "*.png")
                                        .Where(filePath => Path.GetFileNameWithoutExtension(filePath) != etag);
                foreach (var fileToDelete in filesToDelete)
                {
                    File.Delete(fileToDelete);
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var fileStream = new FileStream(path, FileMode.CreateNew))
                    {
                        await stream.CopyToAsync(fileStream);
                        return path;
                    }
                }
            }
        }

        /// <summary>
        /// Downloads a file from a specified URL and saves it to a cache.
        /// If the file already exists in the cache, the function returns the path to the cached file.
        /// If the file does not exist in the cache, the function downloads the file, saves it to the cache, and then returns the path to the cached file.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the path to the downloaded (or cached) file.</returns>
        public static bool PathIsLocalFile(string path) => Path.IsPathFullyQualified(path) && File.Exists(path);

        /// <summary>
        /// Determines whether a specified path represents a URL.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path represents a URL; otherwise, false.</returns>
        public static bool PathIsUrl(string path) => !Path.IsPathFullyQualified(path) && Uri.TryCreate(path, UriKind.Absolute, out _);
    }
}
