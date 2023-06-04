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
        /// Downloads a file from a specified URL and saves it to a local cache.
        /// If the file already exists in the cache, the function returns the path to the cached file.
        /// If the file does not exist in the cache, the function downloads the file, saves it to the cache, and then returns the path to the cached file.
        /// In the event of an error with file retrieval, the most recently used file will be returned, if it exists.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the path to the downloaded (or cached) file.</returns>
        public static async Task<string> DownloadAndCacheFileAsync(string url)
        {
            var tedDirectory = Path.Combine(Path.GetTempPath(), "TED");
            var recentPath = Path.Combine(tedDirectory, "recent.png");

            try
            {
                using (var client = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(10)
                })
                {
                    var response = await client.GetAsync(url);

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException e)
                    {
                        // If we catch here, it's a URL error or server-side issue.
                        if (File.Exists(recentPath))
                        {
                            return recentPath;
                        }

                        return string.Empty;
                    }

                    var fileName = "ted.png";

                    if (response.Headers.ETag != null)
                    {
                        fileName = $"{response.Headers.ETag?.Tag.Replace("\"", string.Empty)}.png";
                    }
                    else if (response.Content.Headers.ContentDisposition != null)
                    {
                        fileName = $"{response.Content.Headers.ContentDisposition.FileName}";
                    }

                    if (!Directory.Exists(tedDirectory))
                    {
                        Directory.CreateDirectory(tedDirectory);
                    }

                    var downloadPath = Path.Combine(tedDirectory, fileName);


                    if (File.Exists(downloadPath))
                    {
                        return downloadPath;
                    }

                    var filesToDelete = Directory.GetFiles(tedDirectory)
                                            .Where(filePath => Path.GetFileNameWithoutExtension(filePath) != fileName && Path.GetFileNameWithoutExtension(filePath) != "recent");
                    foreach (var fileToDelete in filesToDelete)
                    {
                        File.Delete(fileToDelete);
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(downloadPath, FileMode.CreateNew))
                        {
                            await stream.CopyToAsync(fileStream);
                            if(File.Exists(recentPath))
                            {
                                File.Delete(recentPath);
                            }
                            File.Copy(downloadPath, recentPath);
                            return downloadPath;
                        }
                    }
                }
            } catch(HttpRequestException e)
            {
                // If we catch here, we're offline.
                if (File.Exists(recentPath))
                {
                    return recentPath;
                }

                return string.Empty;
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
