﻿using System;
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
    public WebServer(string uriPrefix, string baseFolder)
    {
        System.Threading.ThreadPool.SetMaxThreads(50, 1000);
        System.Threading.ThreadPool.SetMinThreads(50, 50);
        _listener = new HttpListener();
        _listener.Prefixes.Add(uriPrefix);
        _listener.Prefixes.Add(uriPrefix);
        _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
        login.Add("admin", "CasaControl");

        login.Add("gast", "gast");
        _baseFolder = baseFolder;
    }

    public void Start()
    {
        _listener.Start();
        Thread t = new Thread(delegate () {
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

    Hashtable login = new Hashtable();
    Hashtable loggedin = new Hashtable();

    public static string GetDataURL(string imgFile)
    {
        return "data:image/"
                    + Path.GetExtension(imgFile).Replace(".", "")
                    + ";base64,"
                    + Convert.ToBase64String(File.ReadAllBytes(imgFile));
    }

    private void Switch(string device,int state)
    {
        
        var value = (state == 1) ? "60" : "70";

        string rfslave = CasaSharp.Program.devices[device].addressCode;
        string mac = CasaSharp.Program.devices[device].macAddress;
        var code = CasaSharp.Program.GetBasisStation();

        var msg = "00ffff" + code + "08" + rfslave + value + "04040404";

        CasaSharp.Program.Switch(mac,msg);
    }
    
    void ProcessRequest(object listenerContext)
    {
        try
        {
            var context = (HttpListenerContext)listenerContext;
            var request = context.Request;
            string filename = Path.GetFileName(context.Request.RawUrl);
            string path = Path.Combine(_baseFolder, filename);
            byte[] msg;
            string post_response = "";


            string style = "<style>h1,h2{margin-left:20px;} body{padding:0;margin:0; background-color:#00a0e9; color:white;}#plugs{text-align:center; background-color:white;padding: 10px;line-height:30px;} #plug{background-color:white;text-align:center; font-weight:bold;-webkit-box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75); -moz-box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75);box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75); margin:10px;display:inline-block; border:1px solid black; width:200px; height:200px;} #title{padding:5px;background-color:#00a0e9; color:white;} img{background-color:#00a0e9;margin-top:5px;border:1px solid black;width:100px;height:100px;} button{padding:10px;border:1px solid black;-webkit-box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75); -moz-box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75);box-shadow: 0px 0px 5px 0px rgba(0,0,0,0.75); background-color:#00a0e9;color:white; margin-right:5px; }</style>";

            string content = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width,initial-scale=1'>" + style + "<title>CasaSharp</title></head><body><h1>CasaSharp</h1><h2>Smart Home Devices:</h2>";
            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            content += "<div id='plugs'>";
            foreach (var plug in CasaSharp.Program.devices)
            {
                string image = GetDataURL("./data/images/" + plug.Value.imageName);
                content += "<div id='plug'><div id='title'>" + plug.Key + "</div><img src='" + image + "'><br><a href='?switch=" + plug.Value.deviceName + "&state=1'><button>On</button></a><a href='?switch=" + plug.Value.deviceName + "&state=0'><button>Off</button></a></div>";
            }
            content += "</div><h2>CasaSharp &copy; 2017</h2></body></html>";
            msg = Encoding.UTF8.GetBytes(content);
            context.Response.ContentLength64 = msg.Length;
            using (Stream s = context.Response.OutputStream)
                s.Write(msg, 0, msg.Length);
            if (request.HttpMethod == "GET" && filename.Contains("?"))
            {
                Dictionary<string, string> postParams = new Dictionary<string, string>();
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
        catch (Exception ex){ Console.WriteLine(ex.Message); }
        
    }
}