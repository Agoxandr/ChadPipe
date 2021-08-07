using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextCopy;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace ChadPipe
{
    internal class Program
    {
        private static async Task Main()
        {
            var url = ClipboardService.GetText();
            var youtube = new YoutubeClient();
            Core.Initialize();
            var vlc = new LibVLC();
            if (url.Contains("playlist"))
            {
                var playlist = await youtube.Playlists.GetAsync(url);
                var videos = await youtube.Playlists.GetVideosAsync(url);
                int index = 1;
                foreach (var video in videos)
                {
                    await Download(youtube, vlc, video.Url, index, videos.Count, playlist.Title);
                    index++;
                }
            }
            else
            {
                await Search(youtube, url);
            }
        }

        private static async Task Search(YoutubeClient youtube, string searchQuery)
        {
            var results = await youtube.Search.GetVideosAsync(searchQuery);
            foreach (var result in results)
            {
                Console.WriteLine(result.Title);
            }
        }

        private static async Task Download(YoutubeClient youtube, LibVLC vlc, string id, int index, int max, string playlist)
        {
            try
            {
                playlist = playlist.Replace("Album - ", "");
                Console.Out.WriteLine("New Download:");
                var video = await youtube.Videos.GetAsync(id);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(id);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var artist = video.Author.Title;
                StringBuilder path = new StringBuilder("./Music");
                artist = artist.Replace(" - Topic", "");
                if (playlist != null)
                {
                    path.Append('/').Append(SanitizeFolderName(playlist));
                }
                var directory = path.ToString();
                var image = new StringBuilder(directory).Append("/image.jpeg").ToString();
                var output = new StringBuilder(directory).Append("/output.jpeg").ToString();
                var song = new StringBuilder(directory).Append("/song").ToString();
                var songmp3 = new StringBuilder(directory).Append("/song.mp3").ToString();
                Directory.CreateDirectory(directory);
                var fullPath = path.Append('/').Append(index.ToString("D3")).Append(' ').Append(SanitizeFolderName(video.Title)).Append(".mp3").ToString();
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(video.Thumbnails.GetWithHighestResolution().Url, image);
                await youtube.Videos.Streams.DownloadAsync(streamInfo, song);
                var vs = new string[] { "-i", song, songmp3 };
                await Process(vs);
                var vs2 = new string[] { "-i", image, "-vf", "crop=720:720:280:0", output };
                await Process(vs2);
                var vs1 = new string[] { "-i", songmp3, "-i", output, "-map", "0:0", "-map", "1:0", "-c", "copy", "-id3v2_version", "3", "-metadata:s:v", "title=\"Album cover\"", "-metadata:s:v", "comment=\"Cover (front)\"", fullPath };
                await Process(vs1);
                var media = new Media(vlc, fullPath);
                media.SetMeta(MetadataType.Title, video.Title);
                media.SetMeta(MetadataType.Album, playlist);
                media.SetMeta(MetadataType.Artist, artist);
                media.SetMeta(MetadataType.TrackNumber, index.ToString());
                media.SetMeta(MetadataType.TrackTotal, max.ToString());
                media.SaveMeta();
                File.Delete(image);
                File.Delete(output);
                File.Delete(song);
                File.Delete(songmp3);
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine(exception.Message);
                Console.ReadLine();
            }
        }

        public static string SanitizeFolderName(string name)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(name, "");
        }

        private static async Task Process(string[] argumentList)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe"
            };
            foreach (var item in argumentList)
            {
                processStartInfo.ArgumentList.Add(item);
            }
            var process = new Process
            {
                StartInfo = processStartInfo,
            };
            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
