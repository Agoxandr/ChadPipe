using ChadPipe.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static Xamarin.Essentials.Permissions;

namespace ChadPipe
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private CancellationTokenSource cts;
        private HttpClient client;
        private YoutubeClient youtube;

        public MainPage()
        {
            InitializeComponent();
            Login();
        }

        public MainPage(string id)
        {
            InitializeComponent();
            Login();
            targetEntry.Text = id;
            playlistCheckBox.IsChecked = id.Contains("playlist");
        }

        private async void Login()
        {
            try
            {
                CookieCollection cookieCollection = new CookieCollection
                {
                    new Cookie("YSC", "XfBPycRPIDA", "/", ".youtube.com"),
                    new Cookie("VISITOR_INFO1_LIVE", "z0ESiRgYpmk", "/", ".youtube.com"),
                    new Cookie("GPS", "1", "/", ".youtube.com"),
                    new Cookie("CONSENT", "WP.288161", "/", ".youtube.com")
                };
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.CookieContainer.Add(cookieCollection);
                client = new HttpClient(clientHandler);
                youtube = new YoutubeClient(client);
                progressLabel.Text = "Waiting for youtube";
                await client.GetAsync("https://www.youtube.com/");
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "YouTube loaded";
                    downloadButton.IsEnabled = true;
                });
            }
            catch (Exception exception)
            {
                await DisplayAlert("Error", exception.Message, "OK");
            }
        }

        private void Download(object sender, EventArgs e)
        {
            if (!playlistCheckBox.IsChecked)
            {
                _ = Download(targetEntry.Text, false);
            }
            else
            {
                DownloadPlaylist();
            }
            downloadButton.IsEnabled = false;
            cancelButton.IsEnabled = true;
        }

        private async void DownloadPlaylist()
        {
            try
            {
                var id = targetEntry.Text;
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "Loading playlist";
                });
                var videos = await youtube.Playlists.GetVideosAsync(id);
                foreach (var video in videos)
                {
                    await Download(video.Url, true);
                }
                Done();
            }
            catch (Exception exception)
            {
                await DisplayAlert("Error", exception.Message, "OK");
            }
        }

        private async Task Download(string id, bool playlist, string additionalPath = null)
        {
            cts = new CancellationTokenSource();
            try
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "Loading video";
                });
                var video = await youtube.Videos.GetAsync(id);
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "Loading stream manifest";
                });
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(id);
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "Loading stream info with highest bitrate";
                });
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();
                if (streamInfo != null)
                {
                    var status = await Utils.CheckAndRequestPermissionAsync(new StorageWrite());
                    if (status != PermissionStatus.Granted)
                    {
                        Done();
                        return;
                    }
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = "Creating directory";
                    });
                    string path = DependencyService.Get<IGetPathService>().GetPath();
                    if (path == null)
                    {
                        Done();
                        return;
                    }
                    string artist = video.Author;
                    artist.Replace(" - Topic", "");
                    StringBuilder pathStringBuilder = new StringBuilder(path).Append("/").Append(artist);
                    if (playlist)
                    {
                        pathStringBuilder.Append("/").Append(additionalPath);
                    }
                    Directory.CreateDirectory(pathStringBuilder.ToString());
                    // Download the stream to file
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, pathStringBuilder.Append("/").Append(video.Title.Replace('/', '-').Replace('\\', '-')).ToString(), new Progress<double>(progress => Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = new StringBuilder().Append(Math.Round(progress * 100.0)).Append("%").ToString();
                    })), cts.Token);
                }
                else
                {
                    await DisplayAlert("Error", "Stream info unloadable.", "OK");
                    return;
                }
            }
            catch (Exception exception)
            {
                await DisplayAlert("Error", exception.Message, "OK");
            }
            cts = null;
            if (!playlist)
            {
                Done();
            }
        }

        private void Done()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                downloadButton.IsEnabled = true;
                cancelButton.IsEnabled = false;
            });
        }

        private void Cancel(object sender, EventArgs e)
        {
            Cancel();
        }

        private void Cancel()
        {
            if (cts != null)
            {
                cts.Cancel();
            }
            downloadButton.IsEnabled = true;
            cancelButton.IsEnabled = false;
        }
    }
}
