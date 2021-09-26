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

        private List<string> joinarr = new List<string> { ".css", ".js" };

        private string mappath = HttpKit.GetMapPath("/");

        private void button1_Click(object sender, EventArgs e)
        {
            var url = this.textBox1.Text;
            if (String.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("请输入要下载的url地址");

                return;
            }

            try
            {
                this.textBox1.Enabled = false;
                this.button1.Enabled = false;

                var arr = url.Split('/').ToList();
                if (arr.Count < 3)
                {
                    MessageBox.Show("请输入正确的url");
                    return;
                }
                var hostname = arr[0] + "//" + arr[2] + "/";
                arr.RemoveAt(arr.Count - 1);
                var thispath = String.Join("/", arr) + "/";

                GetFilePaths(url, hostname);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.textBox1.Enabled = true;
                this.button1.Enabled = true;

            }

        }


        private Regex linkregex = new Regex(@"<link (((?!href=(""|')).)*)href=(""|')(?<url>((?!(""|')).)*)(""|')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex srcregex = new Regex(@"src=(""|')(?<url>((?!(""|')).)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex urlregex = new Regex(@"url\((""|')(?<url>((?!(""|')).)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void GetFilePaths(string url, string hostname)
        {
            var iscatch = false;
            try
            {
                var str = "";
                var filepath = url.Split('/').Last();
                var filename = "";
                if (String.IsNullOrWhiteSpace(filepath))
                {
                    filepath = "index.html";
                }

                filename = filepath + "";
                filepath = mappath + url.Replace(hostname, "").Replace("/", "\\").Replace(filepath, "") + filepath;
                DirectoryEx.CreateFolder(filepath);

                var hasjoin = joinarr.Any(a => filename.EndsWith(a, StringComparison.OrdinalIgnoreCase));

                using (var client = new GZipWebClient())
                {
                    client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                    try
                    {
                        client.DownloadFile(url, filepath);
                        this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText(index + ":" + url + "====>" + filepath + "==>"); index++; }));
                        if (hasjoin || url == this.textBox1.Text)
                        {
                            Thread.Sleep(1000);
                            str = client.DownloadString(url);
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception exx)
                    {
                        Thread.Sleep(3000);
                        client.DownloadFile(url, filepath);
                        this.richTextBox1.Invoke((Action)(() => { this.richTextBox1.AppendText(index + ":" + url + "====>" + filepath + "==>"); index++; }));
                        if (hasjoin || url == this.textBox1.Text)
                        {
                            str = client.DownloadString(url);
                        }
                        else
                        {
                            return;
                        }
                    }
                }


                if (url == this.textBox1.Text || hasjoin)
                {
                    var urllist = new List<string>();
                    var urldic = url.Replace(filename, "");


                    var links = linkregex.Matches(str);
                    foreach (Match m in links)
                    {
                        if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        {
                            urllist.Add(m.Groups["url"].Value);
                        }
                    }

                    links = srcregex.Matches(str);
                    foreach (Match m in links)
                    {
                        if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        {
                            urllist.Add(m.Groups["url"].Value);
                        }
                    }


                    links = urlregex.Matches(str);
                    foreach (Match m in links)
                    {
                        if (!m.Groups["url"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !m.Groups["url"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                        {
                            urllist.Add(m.Groups["url"].Value);
                        }
                    }

                    urllist = urllist.Distinct().Select(a =>
                    {
                        if (a.StartsWith("/"))
                        {
                            return hostname + a.Substring(1);
                        }
                        return urldic + a;
                    }).ToList();

                    if (urllist.Count > 0)
                    {
                        foreach (var item in urllist)
                        {
                            GetFilePaths(item, hostname);
                        }
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
