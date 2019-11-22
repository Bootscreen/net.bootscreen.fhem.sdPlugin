using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace net.bootscreen.fhem
{
    [PluginActionId("net.bootscreen.fhem.onecommand")]
    public class PluginAction : PluginBase
    {
        //Task longpoll;
        readonly FHEMClient fhem_client = FHEMClient.Instance;
        //bool restart = false;
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    FHEM_command = String.Empty,
                    FHEM_status_dev = String.Empty,
                    FHEM_ip = String.Empty,
                    FHEM_port = -1,
                    FHEM_user = String.Empty,
                    FHEM_pw = String.Empty,
                    FHEM_csrf = String.Empty
                };

                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fhem_command")]
            public string FHEM_command { get; set; }
            [JsonProperty(PropertyName = "fhem_device")]
            public string FHEM_status_dev { get; set; }
            [JsonProperty(PropertyName = "fhem_ip")]
            public string FHEM_ip { get; set; }
            [JsonProperty(PropertyName = "fhem_port")]
            public int FHEM_port { get; set; }
            [JsonProperty(PropertyName = "fhem_username")]
            public string FHEM_user { get; set; }
            [JsonProperty(PropertyName = "fhem_password")]
            public string FHEM_pw { get; set; }
            [JsonProperty(PropertyName = "fhem_csrf")]
            public string FHEM_csrf { get; set; }
            [JsonProperty(PropertyName = "fhem_filter")]
            public string FHEM_filter { get; set; }
        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "CreateDefaultSettings");
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "LoadSettings");
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            Connection.GetGlobalSettingsAsync();

            Console.WriteLine(fhem_client);

            Logger.Instance.LogMessage(TracingLevel.DEBUG, this.GetHashCode() + "fhem_client : " + fhem_client.GetHashCode());
            //if (settings_complete())
            //{
            //    if (longpoll == null)
            //    {
            //        longpoll = longpoll_Async();
            //    }
            //    if (longpoll.Status != TaskStatus.Running)
            //    {
            //        longpoll.Start();
            //    }
            //}
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            Task.Run(ExecuteCommand);
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ReceivedSettings");
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, payload.Settings.ToString());
            Tools.AutoPopulateSettings(settings, payload.Settings);

            SettingsComplete();
        }

        private bool SettingsComplete()
        {
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Test Settings");
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, settings.FHEM_ip);
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, settings.FHEM_port.ToString());
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, settings.FHEM_status_dev);
            if (settings.FHEM_ip != null && settings.FHEM_ip != String.Empty &&
                settings.FHEM_port > 0 &&
                settings.FHEM_status_dev != null && settings.FHEM_status_dev != String.Empty)
            {
                RequestState().Wait();
                return true;
            }
            else
                return false;
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);

            if (SettingsComplete())
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "fhem_client: " + fhem_client.GetHashCode());
                fhem_client.Connect(settings.FHEM_ip,settings.FHEM_port,settings.FHEM_user, settings.FHEM_pw, settings.FHEM_csrf);
                fhem_client.FHEMMessageEvent += Fhem_client_FHEMMessageEvent;
            }
        }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion

        async Task ExecuteCommand()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "request_state: " + Connection.ContextId + ": " + settings.FHEM_status_dev);
            var url = "http://" + settings.FHEM_ip + ":" + settings.FHEM_port + "/fhem?cmd="  +settings.FHEM_command + "&XHR=1&fwcsrf=" + settings.FHEM_csrf;
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "URL:" + url);
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                var json = wc.DownloadString(HttpUtility.UrlEncode(url));
                
                if(json.Length == 0)
                {
                    await Connection.ShowOk();
                }
                else
                {
                    await Connection.ShowAlert();
                }
            }
        }

        async Task RequestState()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "request_state: " + Connection.ContextId + ": " + settings.FHEM_status_dev);
            var url = "http://" + settings.FHEM_ip + ":" + settings.FHEM_port + "/fhem?cmd=jsonlist2%20" + settings.FHEM_status_dev + "%20StreamDeckValue&XHR=1&fwcsrf=" + settings.FHEM_csrf;
            //var url = "http://" + settings.FHEM_ip + ":" + settings.FHEM_port + "/fhem?XHR=1&fwcsrf=" + settings.FHEM_csrf + "&inform=type=status;filter=StreamDeckValue=..*;fmt=JSON";
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "URL:" + url);
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                var json = wc.DownloadString(url);
                //Response resp = JsonConvert.DeserializeObject<Response>(line);
                Rootobject resp = JsonConvert.DeserializeObject<Rootobject>(json);
                if (resp.Results[0].Name == settings.FHEM_status_dev)
                {
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, Connection.ContextId + ": " + resp.Results[0].Readings.StreamDeckValue.Value);
                    await Connection.SetTitleAsync(resp.Results[0].Readings.StreamDeckValue.Value);
                    //Console.WriteLine(resp[0] + ": " + resp[1] + " - " + resp[2]);
                }
            }
        }

        private void Fhem_client_FHEMMessageEvent(object sender, FHEMMessageEventArgs e)
        {
            if (e.Device == settings.FHEM_status_dev + "-StreamDeckValue")
            {
                Console.WriteLine("> received this message: {0} - {1} - {2}", e.Device, e.Value1, e.Value2);
                Connection.SetTitleAsync(e.Value1);
            }
        }

        //async Task LongpollAsync()
        //{

        //    Logger.Instance.LogMessage(TracingLevel.DEBUG, "longpoll_Async: " + Connection.ContextId + ": " + settings.FHEM_status_dev);
        //    var url = "http://" + settings.FHEM_ip + ":" + settings.FHEM_port + "/fhem?XHR=1&fwcsrf=" + settings.FHEM_csrf + "&inform=type=status;filter=" + settings.FHEM_status_dev + ";fmt=JSON";
        //    //var url = "http://" + settings.FHEM_ip + ":" + settings.FHEM_port + "/fhem?XHR=1&fwcsrf=" + settings.FHEM_csrf + "&inform=type=status;filter=StreamDeckValue=..*;fmt=JSON";
        //    //Logger.Instance.LogMessage(TracingLevel.DEBUG, "URL:" + url);
        //    using (var client = new HttpClient())
        //    {
        //        client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
        //        var request = new HttpRequestMessage(HttpMethod.Get, url);
        //        using (var response = await client.SendAsync(
        //            request,
        //            HttpCompletionOption.ResponseHeadersRead))
        //        {
        //            using (var body = await response.Content.ReadAsStreamAsync())
        //            {
        //                using (var reader = new StreamReader(body))
        //                {
        //                    while (!reader.EndOfStream)
        //                    {
        //                        if (restart)
        //                        {
        //                            restart = false;
        //                            Logger.Instance.LogMessage(TracingLevel.DEBUG, "TASK restart " + Connection.ContextId + ": " + longpoll.GetHashCode());
        //                            longpoll = Task.Run(LongpollAsync);
        //                            return;
        //                        }
        //                        string line = reader.ReadLine();
        //                        if (line.Length > 1)
        //                        {
        //                            //Response resp = JsonConvert.DeserializeObject<Response>(line);
        //                            List<string> resp = JsonConvert.DeserializeObject<List<string>>(line);
        //                            if (resp[0] == settings.FHEM_status_dev + "-StreamDeckValue")
        //                            {
        //                                Logger.Instance.LogMessage(TracingLevel.DEBUG, Connection.ContextId + ": " + longpoll.GetHashCode() + ": " + resp[1]);
        //                                await Connection.SetTitleAsync(resp[1]);
        //                                //Console.WriteLine(resp[0] + ": " + resp[1] + " - " + resp[2]);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
    }
}


public class Rootobject
{
    public string Arg { get; set; }
    public Result[] Results { get; set; }
#pragma warning disable IDE1006 // kommt so von FHEM
    public int totalResultsReturned { get; set; }
#pragma warning restore IDE1006 // kommt so von FHEM
}

public class Result
{
    public string Name { get; set; }
    public Internals Internals { get; set; }
    public Readings Readings { get; set; }
    public Attributes Attributes { get; set; }
}

public class Internals
{
}

public class Readings
{
    public Streamdeckvalue StreamDeckValue { get; set; }
}

public class Streamdeckvalue
{
    public string Value { get; set; }
    public string Time { get; set; }
}

public class Attributes
{
}
