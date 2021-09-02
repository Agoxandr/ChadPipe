using LibVLCSharp.Shared;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TextCopy;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace ChadOilPipe
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Core.Initialize();
            var vlc = new LibVLC();
            YoutubeClient youtube = new();
            var url = string.Empty;
            if (args.Length == 0)
            {
                url = ClipboardService.GetText();
            }
            else
            {
                url = args[0];
            }
            if (url != null && url.Contains("playlist"))
            {
                var playlist = await youtube.Playlists.GetAsync(url);
                var videos = await youtube.Playlists.GetVideosAsync(url);
                var albumTitle = playlist.Title.Replace("Album - ", "");
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                path += "/" + SanitizeFolderName(albumTitle);
                Directory.CreateDirectory(path);
                var cover = path + "/Cover.png";
                if (!File.Exists(cover))
                {
                    await CreateCoverAsync(videos[0].Url, path);
                }
                OpenUrl(videos[0].Url);
                await DownloadPlaylistAsync(url, path);
                var size = "D1";
                for (int i = 1; i <= videos.Count; i++)
                {
                    var artist = (videos[i - 1].Author.Title).Replace(" - Topic", "");
                    var title = videos[i - 1].Title;
                    if (videos.Count > 9)
                    {
                        size = "D2";
                    }
                    else if (videos.Count > 99)
                    {
                        size = "D3";
                    }
                    else if (videos.Count > 999)
                    {
                        size = "D4";
                    }
                    var song = path + "/" + i.ToString(size) + ".webm";
                    var fullPath = new StringBuilder(path).Append('/').Append(i.ToString(size)).Append(' ').Append(SanitizeFolderName(title)).Append(".mp3").ToString();
                    var arguments = new string[] { "-i", song, "-i", cover, "-map", "0:0", "-map", "1:0", "-b:a", "160k", "-id3v2_version", "3", "-metadata:s:v", "title=\"Cover\"", "-metadata:s:v", "comment=\"Cover (front)\"", fullPath };
                    var process = CreateProcess("ffmpeg", arguments);
                    process.Start();
                    await process.WaitForExitAsync();
                    var media = new Media(vlc, fullPath);
                    media.SetMeta(MetadataType.Title, title);
                    media.SetMeta(MetadataType.Album, albumTitle);
                    media.SetMeta(MetadataType.Artist, artist);
                    media.SetMeta(MetadataType.TrackNumber, i.ToString());
                    media.SetMeta(MetadataType.TrackTotal, videos.Count.ToString());
                    media.SaveMeta();
                }
                for (int i = 1; i <= videos.Count; i++)
                {
                    File.Delete(path + "/" + i.ToString(size) + ".webm");
                }
                File.Delete(cover);
            }
            else
            {
                Console.WriteLine("Invalid URL.");
            }
        }

        private static async Task CreateCoverAsync(string videoUrl, string path)
        {
            // Get URL of best video stream.
            var process = CreateProcess("youtube-dl", new string[] { "-f", "bestvideo", "--get-url", videoUrl });
            process.StartInfo.RedirectStandardOutput = true;
            string videoOnlyUrl = string.Empty;
            process.OutputDataReceived += (sender, e) => videoOnlyUrl += e.Data;
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();
            await ConvertCoverAsync(videoOnlyUrl, path);
        }

        private static async Task ConvertCoverAsync(string videoOnlyUrl, string path)
        {
            // Download first frame of best video stream.
            var process = CreateProcess("ffmpeg", new string[] { "-ss", "0", "-i", videoOnlyUrl, "-vframes", "1", "-q:v", "2", path + "/Cover.png" });
            process.Start();
            await process.WaitForExitAsync();
        }

        private static async Task DownloadPlaylistAsync(string url, string path)
        {
            var process = CreateProcess("youtube-dl", new string[] { "-f", "bestaudio", "-o", path + "/%(playlist_index)s.webm", "-k", url });
            process.Start();
            await process.WaitForExitAsync();
        }

        private static Process CreateProcess(string fileName, string[] argumentList)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName
            };
            foreach (var item in argumentList)
            {
                processStartInfo.ArgumentList.Add(item);
            }
            var process = new Process
            {
                StartInfo = processStartInfo,
            };
            return process;
        }

        public static string SanitizeFolderName(string name)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(name, "");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
