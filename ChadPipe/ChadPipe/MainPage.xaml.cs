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
        }

        private async void Login()
        {
            try
            {
                CookieCollection cookieCollection = new CookieCollection
                {
                    new Cookie("YSC", "yg29FgfCzgY", "/", ".youtube.com"),
                    new Cookie("SID", "tQfyL6_aWFzjWZxNiqj7wRVIIZiKrHECMzIVsiLCvpxn-XE8Fv9lqnl5X0QNQTPr_hlQOw.", "/", ".youtube.com"),
                    new Cookie("VISITOR_INFO1_LIVE", "tzGbQ9vlfCg", "/", ".youtube.com"),
                    new Cookie("HSID", "AVQnVZJByVIbSq8LJ", "/", ".youtube.com"),
                    new Cookie("SSID", "ATEJKC0ONAkAdnaUq", "/", ".youtube.com"),
                    new Cookie("APISID", "OjcOBMysdDYfqEKo/Apg5eakbYP-bA1I2l", "/", ".youtube.com"),
                    new Cookie("SAPISID", "x9FVXe72QO-mo7fP/APNCsU03XNu50cbZK", "/", ".youtube.com"),
                    new Cookie("SAPISID", "x9FVXe72QO-mo7fP/APNCsU03XNu50cbZK", "/", ".youtube.com"),
                    new Cookie("LOGIN_INFO", "AFmmF2swRgIhAOLy2mXd2-IA6f0Yig7d9ure0XTABU6-t6oRoMSOGvubAiEAvBq6q5u9GMgJgiBdGftBAVdUpvZMs7r-9Br1k2xUCzI:QUQ3MjNmd3VuUHh1VHFZVDNDQXdRVmluc0w3LTYybzkxTkZvZXZhem9fcFVOZnlwUUVzUkc2R1lTOHJKTjdfUk9ueGo2dHk5MTVUV09xc2k1d29sTHhicUV6TjJJRElYdEtqTUw3VkNsVjdiZDZ5SDlVNm41Zll3dVRyalF4djVJdjhrS003aW13TVEzdnVYUGxLVWZIcHJFbUpVYkItZExHaDVYLU1LMWtjenFFYXBSc2szNTYyOF8zUXdWdzIxcWlEOGVIZk1EMThNcHJUZHJvZU4yVkYxUmhlOUtVNjdDajdyWnRvSlhQNm1jZW4tSUwtbE5xQkJmUEZIMDRDVnZKLVYzdDI3bU1IXw==", "/", ".youtube.com")
                };
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.CookieContainer.Add(cookieCollection);
                client = new HttpClient(clientHandler);
                youtube = new YoutubeClient(client);
                progressLabel.Text = "Waiting for youtube";
                await client.GetAsync("https://www.youtube.com/");
                Device.BeginInvokeOnMainThread(() =>
                {
                    progressLabel.Text = "Youtube loaded";
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

        private async Task Download(string id, bool playlist)
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
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = "Retrieved Stream info";
                    });
                    var status = await Utils.CheckAndRequestPermissionAsync(new StorageWrite());
                    if (status != PermissionStatus.Granted)
                    {
                        Done();
                        return;
                    }
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = "Permission granted";
                    });
                    string path = DependencyService.Get<IGetPathService>().GetPath();
                    if (path == null)
                    {
                        Done();
                        return;
                    }
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = "Found path";
                    });
                    StringBuilder pathStringBuilder = new StringBuilder(path).Append("/").Append(video.Author);
                    Directory.CreateDirectory(pathStringBuilder.ToString());
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        progressLabel.Text = "Created directory";
                    });
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
