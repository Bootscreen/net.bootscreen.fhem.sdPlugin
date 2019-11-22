using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

sealed class FHEMClient
{
    public string FHEM_ip { get; set; } = String.Empty;
    public int FHEM_port { get; set; } = -1;
    public string FHEM_user { get; set; } = String.Empty;
    public string FHEM_pw { get; set; } = String.Empty;
    public string FHEM_csrf { get; set; } = String.Empty;
    public string FHEM_filter { get; set; } = "StreamDeckValue=..*";

    public event EventHandler<FHEMMessageEventArgs> FHEMMessageEvent;

    public bool connected { get; private set; } = false;
    private bool end_task = false;

    private Task message_task;

    private HttpClient client = new HttpClient();
    private HttpRequestMessage requestMessage  = new HttpRequestMessage();
    private readonly int timeout = Timeout.Infinite;

    private readonly string url = "http://{0}:{1}/fhem?XHR=1&fwcsrf={2}&inform=type=status;filter={3};fmt=JSON";
    private readonly string url_with_login = "http://{0}:{1}@{2}:{3}/fhem?XHR=1&fwcsrf={4}&inform=type=status;filter={5};fmt=JSON";


    public static readonly FHEMClient Instance = new FHEMClient();

    private FHEMClient()
    {
    }

    //public FHEMClient(string fhem_ip, int fhem_port, string fhem_csrf)
    //{
    //    FHEM_ip = fhem_ip;
    //    FHEM_port = fhem_port;
    //    FHEM_csrf = fhem_csrf;
    //}

    //public FHEMClient(string fhem_ip, int fhem_port, string fhem_user, string fhem_pw, string fhem_csrf)
    //{
    //    FHEM_ip = fhem_ip;
    //    FHEM_port = fhem_port;
    //    FHEM_user = fhem_user;
    //    FHEM_pw = fhem_pw;
    //    FHEM_csrf = fhem_csrf;
    //}
    //public FHEMClient(string fhem_ip, int fhem_port, string fhem_user, string fhem_pw, string fhem_csrf, string fhem_filter)
    //{
    //    FHEM_ip = fhem_ip;
    //    FHEM_port = fhem_port;
    //    FHEM_user = fhem_user;
    //    FHEM_pw = fhem_pw;
    //    FHEM_csrf = fhem_csrf;
    //    FHEM_filter = fhem_filter;
    //}


    public bool Connect(string fhem_ip, int fhem_port, string fhem_csrf)
    {
        FHEM_ip = fhem_ip;
        FHEM_port = fhem_port;
        FHEM_csrf = fhem_csrf;

        return Connect();
    }
    public bool Connect(string fhem_ip, int fhem_port, string fhem_user, string fhem_pw, string fhem_csrf)
    {
        FHEM_ip = fhem_ip;
        FHEM_port = fhem_port;
        FHEM_user = fhem_user;
        FHEM_pw = fhem_pw;
        FHEM_csrf = fhem_csrf;

        return Connect();
    }
    public bool Connect(string fhem_ip, int fhem_port, string fhem_user, string fhem_pw, string fhem_csrf, string fhem_filter)
    {
        FHEM_ip = fhem_ip;
        FHEM_port = fhem_port;
        FHEM_user = fhem_user;
        FHEM_pw = fhem_pw;
        FHEM_csrf = fhem_csrf;
        FHEM_filter = fhem_filter;

        return Connect();
    }

    public bool Connect()
    {
        if (!connected)
        {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            string filled_url;
            if (FHEM_user.Length > 0)
            {
                filled_url = String.Format(url_with_login, FHEM_user, FHEM_pw, FHEM_ip, FHEM_port, FHEM_csrf, FHEM_filter);
            }
            else
            {
                filled_url = String.Format(url, FHEM_ip, FHEM_port, FHEM_csrf, FHEM_filter);
            }

            requestMessage = new HttpRequestMessage(HttpMethod.Get, filled_url);

            if (requestMessage == null)
            {
                Disconnect();
                return false;
            }

            message_task = GetMessages();
            connected = true;
            return true;
        }
        else
        {

        }
        return false;
    }

    public async Task GetMessages()
    {
        using (var response = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead))
        {
            using (var body = await response.Content.ReadAsStreamAsync())
            {
                using (var reader = new StreamReader(body))
                { 
                    while (!reader.EndOfStream || end_task)
                    {
                        string line = reader.ReadLine();
                        if (line.Length > 1)
                        {
                            List<string> resp = JsonConvert.DeserializeObject<List<string>>(line);
                            OnFHEMMessageEvent(new FHEMMessageEventArgs(resp[0], resp[1], resp[2]));
                        }
                    }
                }
            }
        }
    }

    void OnFHEMMessageEvent(FHEMMessageEventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        EventHandler<FHEMMessageEventArgs> handler = FHEMMessageEvent;

        // Event will be null if there are no subscribers
        if (handler != null)
        {
            // Use the () operator to raise the event.
            handler(this, e);
        }
    }

    public void Disconnect()
    {
        requestMessage.Dispose();
        client.Dispose();
    }
}

public class FHEMMessageEventArgs : EventArgs
{
    public FHEMMessageEventArgs(string device_arg, string value1_arg, string value2_arg)
    {
        Device = device_arg;
        Value1 = value1_arg;
        Value2 = value2_arg;
    }

    public string Device { get; set; }
    public string Value1 { get; set; }
    public string Value2 { get; set; }

}
//    var url = "http://192.168.66.32:8088/fhem?XHR=1&fwcsrf=asdfdshzwegfdfthewr4gt&inform=type=status;filter=StreamDeckValue=..*;fmt=JSON";
//        using (var client = new HttpClient())
//        {
//            client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
//            var request = new HttpRequestMessage(HttpMethod.Get, url);
//            using (var response = await client.SendAsync(
//                request,
//                HttpCompletionOption.ResponseHeadersRead))
//            {
//                using (var body = await response.Content.ReadAsStreamAsync())
//                {
//                    using (var reader = new StreamReader(body))
//                    {
//                        while (!reader.EndOfStream)
//                        {
//                            string line = reader.ReadLine();
//                            if (line.Length > 1)
//                            {
//                                //Response resp = JsonConvert.DeserializeObject<Response>(line);
//                                List<string> resp = JsonConvert.DeserializeObject<List<string>>(line);
////if (resp[0].EndsWith("-StreamDeckValue"))
////{
//Console.WriteLine(resp[0] + ": " + resp[1] + " - " + resp[2]);
//                                //}
//                            }
//                        }
//                    }
//                }
//            }
//        }
//}