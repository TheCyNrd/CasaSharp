using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;

class WebServer
{
    HttpListener _listener;
    string _baseFolder;
    string logo = GetDataURL("./data/images/logo.png");
    Dictionary<string, string> postParams = new Dictionary<string, string>();
    Dictionary<string, string> images = new Dictionary<string, string>();
    public WebServer(string uriPrefix, string baseFolder)
    {
        System.Threading.ThreadPool.SetMaxThreads(50, 1000);
        System.Threading.ThreadPool.SetMinThreads(50, 50);
        _listener = new HttpListener();
        _listener.Prefixes.Add(uriPrefix);
        _listener.Prefixes.Add(uriPrefix);
        _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
        _baseFolder = baseFolder;
    }

    public void Start()
    {
        _listener.Start();
        Thread t = new Thread(delegate ()
        {
            foreach (string img in Directory.GetFiles(Application.StartupPath + "/data/images/"))
            {
                images.Add(Path.GetFileName(img),GetDataURL(img));
            }
            while (true)
                try
                {
                    HttpListenerContext request = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, request);
                }
                catch (HttpListenerException) { break; }
                catch (InvalidOperationException) { break; }
        });
        t.Start();
    }

    public static string GetDataURL(string imgFile)
    {
        byte[] b = File.ReadAllBytes(imgFile);
        string datauri = "data:image/"
                    + Path.GetExtension(imgFile).Replace(".", "")
                    + ";base64,"
                    + Convert.ToBase64String(File.ReadAllBytes(imgFile));
        b = null;
        return datauri;
    }

    private void Switch(string device, int state)
    {
        var value = (state == 1) ? "60" : "70";
        var statestr = (state == 1) ? "On" : "Off";

        string rfslave = CasaSharp.Program.devices[device].addressCode;
        string mac = CasaSharp.Program.devices[device].macAddress;
        var code = CasaSharp.Program.GetBasisStation();

        var msg = "00ffff" + code + "08" + rfslave + value + "04040404";

        CasaSharp.Program.Switch(mac, msg);
        Console.WriteLine(CasaSharp.Program.devices[device].deviceName + " has been turned "+statestr);
    }

    void ProcessRequest(object listenerContext)
    {
        CasaSharp.Program.Start();
        var context = (HttpListenerContext)listenerContext;
        var request = context.Request;
        string filename = Path.GetFileName(context.Request.RawUrl);
        string path = Path.Combine(_baseFolder, filename);
        byte[] msg;
        string post_response = "";
        postParams.Clear();
        HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
        try
        {
            if (identity.IsAuthenticated && identity.Name == CasaSharp.Program.config.Value("webserver_login") && identity.Password == CasaSharp.Program.config.Value("webserver_password"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                string style = "<style>#header,#footer{background-color:#00a0e9;} #header img{width:300px; filter: drop-shadow(5px 5px 5px #222);} h1,h2{margin-left:20px;} body{background-color:#00a0e9;padding:0;margin:0; color:white;}#plugs{text-align:center; background-color:white;padding: 10px;line-height:30px;} #plug{background-color:white;text-align:center; font-weight:bold; filter: drop-shadow(5px 5px 5px #222); margin:10px;display:inline-block; border:1px solid black; width:200px; height:200px;} #title{-webkit-box-shadow: 0px 2px 5px 0px rgba(0,0,0,0.75);-moz-box-shadow: 0px 2px 5px 0px rgba(0,0,0,0.75);box-shadow: 0px 2px 5px 0px rgba(0,0,0,0.75); padding:5px;background-color:#00a0e9; color:white;} #plug img{background-color:#00a0e9;margin-top:5px;border:1px solid black;width:100px;height:100px; } button{padding:10px;border:1px solid black; background-color:#00a0e9;color:white; margin-right:5px; }</style>";
                string content = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width,initial-scale=1'>" + style + "<title>CasaSharp</title></head><body><div id='header'><div style='color:white;text-align:right;font-weight:bold;padding-right:200px;'>Devices - Settings - </div><h1><img src='" + logo + "'></h1><h2>Devices:</h2></div>";

                content += "<div id='plugs'>";
                foreach (var plug in CasaSharp.Program.devices)
                {
                    content += "<div id='plug'><div id='title'>" + plug.Value.deviceName + "</div><img src='" + images[plug.Value.imageName] + "'><br><a href='?switch=" + plug.Value.addressCode + "&state=1'><button>On</button></a><a href='?switch=" + plug.Value.addressCode + "&state=0'><button>Off</button></a></div>";
                }

                content += "</div><div id='footer'><div style='color:white;text-align:center;font-weight:bold;'>CasaSharp &copy; TheCyNrd 2017</div></div></body></html>";
                msg = Encoding.UTF8.GetBytes(content);
                context.Response.ContentLength64 = msg.Length;
                using (Stream s = context.Response.OutputStream)
                {
                    s.Write(msg, 0, msg.Length);
                    s.Dispose();
                }
                if (request.HttpMethod == "GET" && filename.Contains("?"))
                {
                    string[] rawParams = filename.Split('&');
                    foreach (string param in rawParams)
                    {
                        string[] kvPair = param.Split('=');
                        string key = kvPair[0].Replace("?", "");
                        string value = HttpUtility.UrlDecode(kvPair[1]);
                        postParams.Add(key, value);
                    }
                    Switch(postParams["switch"], Int32.Parse(postParams["state"]));
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                msg = Encoding.UTF8.GetBytes("ACCESS DENIED");
                context.Response.ContentLength64 = msg.Length;
                using (Stream s = context.Response.OutputStream)
                    s.Write(msg, 0, msg.Length);
            }
        }
        catch { }
    }
}