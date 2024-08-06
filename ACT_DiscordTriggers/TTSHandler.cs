using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ACT_DiscordTriggers
{
    class TTSHandler
    {
        public string Command { get; set; }
        public string CommandArguments { get; set; }
        public Exception LastException { get; private set; }
        private Process process = null;
        private FileInfo CommandInfo;
        private ListBox log;
        UdpClient uc = new UdpClient();
        public TTSHandler(ListBox log)
        {
            this.log = log;
        }
        public bool Open()
        {
            CommandInfo = new FileInfo(Command);
            if (process == null && CommandInfo.Exists)
            {
                try
                {
                    process = new Process
                    {
                        StartInfo = {
                            FileName = Command,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            Arguments = CommandArguments,
                            RedirectStandardInput = true
                        }
                    };
                    process.Exited += new EventHandler(delegate (object o, EventArgs e) { process = null; });
                    process.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    this.log.Items.Add(ex.ToString());
                    this.LastException = ex;
                    process = null;
                }
            }
            else
            {
                this.log.Items.Add("Command not found or process not null");
            }
            return false;
        }

        public void Play(string text)
        {
            try
            {
                if (process == null)
                {
                    var opened = Open();
                    if (!opened)
                    {
                        this.LastException = new Exception("TTSCommand Not Found " + CommandInfo.FullName);
                        this.log.Items.Add(this.LastException.ToString());
                        return;
                    }
                }
                process.StandardInput.Write(text.ToLower());
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.LastException = ex;
            }
        }

        public void PlaySocket(string text)
        {
            try
            {
                uc.SendAsync(Encoding.UTF8.GetBytes(text.ToLower()), Encoding.UTF8.GetByteCount(text.ToLower()), Command, 5555);
            }
            catch (Exception e)
            {
                log.Items.Add(e.ToString());
                this.LastException = e;
            }
        }

        public void PlaySingle(string text)
        {
            try
            {
                Process tempProcess = new Process
                {
                    StartInfo = {
                        FileName = Command,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Arguments = CommandArguments + " \"" + Regex.Replace(Regex.Replace(text, @"(\\*)"+"\"", @"$1$1\"+"\""), @"(\\+)$", @"$1$1") + "\""
                    }
                };
                tempProcess.Start();
            }
            catch (Exception ex)
            {
                this.log.Items.Add(ex.ToString());
                this.LastException = ex;
            }
        }

        public bool Close()
        {
            try
            {
                if (process != null)
                {
                    process.Kill();
                    process = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                process = null;
                this.log.Items.Add(ex.ToString());
                this.LastException = ex;
            }
            return false;
        }

        public void Restart()
        {
            Close();
            CommandInfo = new FileInfo(Command);
            Open();
        }
    }
}
