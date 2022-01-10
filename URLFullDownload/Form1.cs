using ChinaBM.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace URLFullDownload
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int index = 1;

        //采集文件后缀
        private List<string> joinarr = new List<string> { ".css", ".js" };

        //当前根目录
        private string mappath = HttpKit.GetMapPath("/");

        //前缀
        private string qianzhui = "http:";

        //附加正则集合
        private List<string> reglist = new List<string>();

        //url下载文件集合
        private List<string> includeurllist = new List<string>();

        //已下载文件集合
        private List<string> downloadlist = new List<string>();

        private void button1_Click(object sender, EventArgs e)
        {
            var url = this.richTextBox3.Text;
            if (String.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("请输入要下载的url地址");

                return;
            }

            if (url.IndexOf("https://") > -1)
            {
                qianzhui = "https:";
            }

            try
            {
                this.richTextBox3.Enabled = false;
                this.button1.Enabled = false;
                reglist = this.richTextBox2.Text.ToSafeString().Split('\n').ToList().Where(a => !String.IsNullOrWhiteSpace(a)).Distinct().ToList();
                includeurllist = url.Split('\n').ToList();

                foreach (var item in includeurllist)
                {
                    if (!String.IsNullOrWhiteSpace(item))
                    {
                        GetFilePaths(item);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.richTextBox3.Enabled = true;
                this.button1.Enabled = true;

            }

        }


        private Regex linkregex = new Regex(@"<link (((?!href=(""|')).)*)href=(""|')(?<url>((?!(""|')).)*)(""|')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex srcregex = new Regex(@"src=(""|')(?<url>((?!(""|')).)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex urlregex = new Regex(@"url\((""|')?(?<url>((?!(""|'|\))).)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Dictionary<string, string> urlReplaces = new Dictionary<string, string>();

        private string[] houzuis = new string[] { "jpg", "jpeg", "png", "gif", "bmp4", "css", "js", "woff", "woff2" };

        private List<string> hostnamelist = new List<string>();

        private void GetFilePaths(string url)
        {
            var arr = url.Split('/').ToList();
            if (arr.Count < 3)
            {
                return;
            }
            var hostname = arr[0] + "//" + arr[2] + "/";
            arr.RemoveAt(arr.Count - 1);

            if (!hostnamelist.Contains(hostname))
            {
                hostnamelist.Add(hostname);
            }

            var iscatch = false;
            try
            {
                var str = "";
                var filepath = url.Split('/').Last().Split('?').First();
                var filename = "";
                if (String.IsNullOrWhiteSpace(filepath))
                {
                    if (includeurllist.IndexOf(url) > -1)
                    {
                        filepath = "index.html";
                    }
                    else
                    {
                        return;
                    }
                }

                filename = filepath + "";
                filepath = (mappath + url.Replace(hostname, "").Replace("/", "\\").Replace(filepath, "") + filepath).Split('?').First();
                DirectoryEx.CreateFolder(filepath);

                if (filepath.Last() == '\\')
                {
                    filepath = filepath + filename;
                }
                if (File.Exists(filepath))
                {
                    return;
                }

                var hasjoin = joinarr.Any(a => ("." + filename.Split('.').Last().Split('?').First()).EndsWith(a, StringComparison.OrdinalIgnoreCase));

                using (var client = new GZipWebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                    try
                    {
                        this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText(index + ":" + url + "====>" + filepath + "==>"); index++; }));
                        if (hasjoin || includeurllist.IndexOf(url) > -1)
                        {
                            str = client.DownloadString(url);
                        }
                        else
                        {
                            client.DownloadFile(url, filepath);
                            return;
                        }
                    }
                    catch (Exception exx)
                    {
                        Thread.Sleep(3000);
                        if (hasjoin || includeurllist.IndexOf(url) > -1)
                        {
                            str = client.DownloadString(url);
                        }
                        else
                        {
                            client.DownloadFile(url, filepath);
                            return;
                        }
                        //client.DownloadFile(url, filepath);
                        //this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText(index + ":" + url + "====>" + filepath + "==>"); index++; }));
                        //if (hasjoin || url == this.textBox1.Text)
                        //{
                        //    str = client.DownloadString(url);
                        //}
                        //else
                        //{
                        //    return;
                        //}
                    }
                }


                if (includeurllist.IndexOf(url) > -1 || hasjoin)
                {
                    var urllist = new List<string>();
                    var urldic = url;
                    if (!String.IsNullOrWhiteSpace(url.Split('/').Last()))
                    {
                        urldic = url.Replace(url.Split('/').Last(), "");
                    }




                    var links = linkregex.Matches(str);
                    foreach (Match m in links)
                    {
                        urllist.Add(m.Groups["url"].Value);

                        //if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    urllist.Add(m.Groups["url"].Value);
                        //}
                    }

                    links = srcregex.Matches(str);
                    foreach (Match m in links)
                    {
                        urllist.Add(m.Groups["url"].Value);
                        //if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    urllist.Add(m.Groups["url"].Value);
                        //}
                    }


                    links = urlregex.Matches(str);
                    foreach (Match m in links)
                    {
                        urllist.Add(m.Groups["url"].Value);
                        //if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    urllist.Add(m.Groups["url"].Value);
                        //}
                    }

                    if (reglist.Count > 0)
                    {
                        try
                        {
                            foreach (var item in reglist)
                            {
                                var regexobj = new Regex(item, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                                links = regexobj.Matches(str);
                                foreach (Match m in links)
                                {
                                    urllist.Add(m.Groups["url"].Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    urllist = urllist.Distinct().Select(a =>
                    {
                        var rurl = "";
                        if (a.StartsWith("//"))
                        {
                            rurl = qianzhui + a;
                        }
                        else if (a.StartsWith("/"))
                        {
                            rurl = hostname + a.Substring(1);
                        }
                        else if (a.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            rurl = a;
                        }
                        else
                        {
                            rurl = urldic + a;
                        }
                        urlReplaces[a] = rurl;

                        return rurl;
                    }).Where(a => houzuis.Any(b => a.Split('.').Last().StartsWith(b, StringComparison.OrdinalIgnoreCase))).ToList();

                    if (urllist.Count > 0)
                    {
                        foreach (var item in urllist)
                        {
                            GetFilePaths(item);
                        }
                    }

                    foreach (var host in hostnamelist)
                    {
                        str = str.Replace(host, "").Replace(host.Replace("http:", "").Replace("https:", ""), "");
                    }


                    using (var stream = new StreamWriter(filepath, false, Encoding.UTF8))
                    {
                        stream.Write(str);
                        stream.Close();
                    }

                }
            }
            catch (Exception ex)
            {
                iscatch = true;
                this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText("error:" + ex.Message + "\n"); }));
                //MessageBox.Show("GetFilePaths" + ex.Message);
            }
            finally
            {
                if (!iscatch)
                {
                    this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText("success\n"); }));
                }
            }
        }
    }
}
