using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using NAudio.Wave;
using Discord.Net.Providers.WS4Net;
using Discord.Net.Providers.UDPClient;
using System.Reflection;
using System.Collections.Generic;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace ACT_DiscordTriggers
{
	public class DiscordPlugin : UserControl, IActPluginV1 {
		#region Designer Created Code (Avoid editing)
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.txtFFLogsToken = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.sliderTTSSpeed = new System.Windows.Forms.TrackBar();
            this.lblTTSSpeed = new System.Windows.Forms.Label();
            this.sliderTTSVol = new System.Windows.Forms.TrackBar();
            this.lblTTSVol = new System.Windows.Forms.Label();
            this.cmbChan = new System.Windows.Forms.ComboBox();
            this.lblChan = new System.Windows.Forms.Label();
            this.cmbServer = new System.Windows.Forms.ComboBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.cmbTTS = new System.Windows.Forms.ComboBox();
            this.lblTTS = new System.Windows.Forms.Label();
            this.btnLeave = new System.Windows.Forms.Button();
            this.btnJoin = new System.Windows.Forms.Button();
            this.lblLog = new System.Windows.Forms.Label();
            this.logBox = new System.Windows.Forms.TextBox();
            this.txtToken = new System.Windows.Forms.TextBox();
            this.lblBotTok = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(833, 502);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.txtFFLogsToken);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.sliderTTSSpeed);
            this.tabPage1.Controls.Add(this.lblTTSSpeed);
            this.tabPage1.Controls.Add(this.sliderTTSVol);
            this.tabPage1.Controls.Add(this.lblTTSVol);
            this.tabPage1.Controls.Add(this.cmbChan);
            this.tabPage1.Controls.Add(this.lblChan);
            this.tabPage1.Controls.Add(this.cmbServer);
            this.tabPage1.Controls.Add(this.lblServer);
            this.tabPage1.Controls.Add(this.cmbTTS);
            this.tabPage1.Controls.Add(this.lblTTS);
            this.tabPage1.Controls.Add(this.btnLeave);
            this.tabPage1.Controls.Add(this.btnJoin);
            this.tabPage1.Controls.Add(this.lblLog);
            this.tabPage1.Controls.Add(this.logBox);
            this.tabPage1.Controls.Add(this.txtToken);
            this.tabPage1.Controls.Add(this.lblBotTok);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(825, 476);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Settings";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtFFLogsToken
            // 
            this.txtFFLogsToken.Location = new System.Drawing.Point(30, 67);
            this.txtFFLogsToken.Name = "txtFFLogsToken";
            this.txtFFLogsToken.Size = new System.Drawing.Size(193, 20);
            this.txtFFLogsToken.TabIndex = 39;
            this.txtFFLogsToken.UseSystemPasswordChar = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 38;
            this.label1.Text = "FFLogs Token";
            // 
            // sliderTTSSpeed
            // 
            this.sliderTTSSpeed.Location = new System.Drawing.Point(30, 347);
            this.sliderTTSSpeed.Maximum = 20;
            this.sliderTTSSpeed.Name = "sliderTTSSpeed";
            this.sliderTTSSpeed.Size = new System.Drawing.Size(193, 45);
            this.sliderTTSSpeed.TabIndex = 37;
            this.sliderTTSSpeed.Value = 10;
            // 
            // lblTTSSpeed
            // 
            this.lblTTSSpeed.AutoSize = true;
            this.lblTTSSpeed.Location = new System.Drawing.Point(27, 331);
            this.lblTTSSpeed.Name = "lblTTSSpeed";
            this.lblTTSSpeed.Size = new System.Drawing.Size(62, 13);
            this.lblTTSSpeed.TabIndex = 36;
            this.lblTTSSpeed.Text = "TTS Speed";
            // 
            // sliderTTSVol
            // 
            this.sliderTTSVol.Location = new System.Drawing.Point(30, 283);
            this.sliderTTSVol.Maximum = 20;
            this.sliderTTSVol.Name = "sliderTTSVol";
            this.sliderTTSVol.Size = new System.Drawing.Size(193, 45);
            this.sliderTTSVol.TabIndex = 35;
            this.sliderTTSVol.Value = 10;
            // 
            // lblTTSVol
            // 
            this.lblTTSVol.AutoSize = true;
            this.lblTTSVol.Location = new System.Drawing.Point(27, 267);
            this.lblTTSVol.Name = "lblTTSVol";
            this.lblTTSVol.Size = new System.Drawing.Size(66, 13);
            this.lblTTSVol.TabIndex = 34;
            this.lblTTSVol.Text = "TTS Volume";
            // 
            // cmbChan
            // 
            this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChan.FormattingEnabled = true;
            this.cmbChan.Location = new System.Drawing.Point(30, 151);
            this.cmbChan.Name = "cmbChan";
            this.cmbChan.Size = new System.Drawing.Size(193, 21);
            this.cmbChan.TabIndex = 33;
            // 
            // lblChan
            // 
            this.lblChan.AutoSize = true;
            this.lblChan.Location = new System.Drawing.Point(27, 134);
            this.lblChan.Name = "lblChan";
            this.lblChan.Size = new System.Drawing.Size(46, 13);
            this.lblChan.TabIndex = 32;
            this.lblChan.Text = "Channel";
            // 
            // cmbServer
            // 
            this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbServer.FormattingEnabled = true;
            this.cmbServer.Location = new System.Drawing.Point(30, 109);
            this.cmbServer.Name = "cmbServer";
            this.cmbServer.Size = new System.Drawing.Size(193, 21);
            this.cmbServer.TabIndex = 31;
            this.cmbServer.SelectedIndexChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(27, 92);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(38, 13);
            this.lblServer.TabIndex = 30;
            this.lblServer.Text = "Server";
            // 
            // cmbTTS
            // 
            this.cmbTTS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTTS.FormattingEnabled = true;
            this.cmbTTS.Location = new System.Drawing.Point(30, 230);
            this.cmbTTS.Name = "cmbTTS";
            this.cmbTTS.Size = new System.Drawing.Size(193, 21);
            this.cmbTTS.TabIndex = 29;
            // 
            // lblTTS
            // 
            this.lblTTS.AutoSize = true;
            this.lblTTS.Location = new System.Drawing.Point(27, 213);
            this.lblTTS.Name = "lblTTS";
            this.lblTTS.Size = new System.Drawing.Size(58, 13);
            this.lblTTS.TabIndex = 28;
            this.lblTTS.Text = "TTS Voice";
            // 
            // btnLeave
            // 
            this.btnLeave.Enabled = false;
            this.btnLeave.Location = new System.Drawing.Point(129, 178);
            this.btnLeave.Name = "btnLeave";
            this.btnLeave.Size = new System.Drawing.Size(94, 23);
            this.btnLeave.TabIndex = 27;
            this.btnLeave.Text = "Leave Channel";
            this.btnLeave.UseVisualStyleBackColor = true;
            this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
            // 
            // btnJoin
            // 
            this.btnJoin.Enabled = false;
            this.btnJoin.Location = new System.Drawing.Point(30, 178);
            this.btnJoin.Name = "btnJoin";
            this.btnJoin.Size = new System.Drawing.Size(93, 23);
            this.btnJoin.TabIndex = 26;
            this.btnJoin.Text = "Join Channel";
            this.btnJoin.UseVisualStyleBackColor = true;
            this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
            // 
            // lblLog
            // 
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(268, 8);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(60, 13);
            this.lblLog.TabIndex = 25;
            this.lblLog.Text = "Debug Log";
            // 
            // logBox
            // 
            this.logBox.BackColor = System.Drawing.SystemColors.Control;
            this.logBox.Location = new System.Drawing.Point(271, 34);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(424, 310);
            this.logBox.TabIndex = 24;
            // 
            // txtToken
            // 
            this.txtToken.Location = new System.Drawing.Point(30, 24);
            this.txtToken.Name = "txtToken";
            this.txtToken.Size = new System.Drawing.Size(193, 20);
            this.txtToken.TabIndex = 23;
            this.txtToken.UseSystemPasswordChar = true;
            // 
            // lblBotTok
            // 
            this.lblBotTok.AutoSize = true;
            this.lblBotTok.Location = new System.Drawing.Point(27, 8);
            this.lblBotTok.Name = "lblBotTok";
            this.lblBotTok.Size = new System.Drawing.Size(96, 13);
            this.lblBotTok.TabIndex = 22;
            this.lblBotTok.Text = "Discord Bot Token";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(825, 476);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Maps";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // DiscordPlugin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "DiscordPlugin";
            this.Size = new System.Drawing.Size(833, 502);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		#endregion
		public DiscordPlugin() {
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			InitializeComponent();
			var tts = new SpeechSynthesizer();
			foreach (InstalledVoice v in tts.GetInstalledVoices())
				cmbTTS.Items.Add(v.VoiceInfo.Name);
			cmbTTS.SelectedIndex = 0;
		}

		Label lblStatus;
		string settingsFile;
		SettingsSerializer xmlSettings;
		private DiscordSocketClient bot;
		private IAudioClient audioClient;
		private SpeechAudioFormatInfo formatInfo;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TrackBar sliderTTSSpeed;
        private Label lblTTSSpeed;
        private TrackBar sliderTTSVol;
        private Label lblTTSVol;
        private ComboBox cmbChan;
        private Label lblChan;
        private ComboBox cmbServer;
        private Label lblServer;
        private ComboBox cmbTTS;
        private Label lblTTS;
        private Button btnLeave;
        private Button btnJoin;
        private Label lblLog;
        private TextBox logBox;
        private TextBox txtToken;
        private Label lblBotTok;
        private TabPage tabPage2;
        public TextBox txtFFLogsToken;
        private Label label1;
        private AudioOutStream voiceStream;

        private Random ran = new Random();

        #region IActPluginV1 Members
        public async void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
			//ACT Stuff
			lblStatus = pluginStatusText;
			settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\ACT_DiscordTriggers.config.xml");
			pluginScreenSpace.Controls.Add(this);
			pluginScreenSpace.Text = "Discord Triggers";
			this.Dock = DockStyle.Fill;
			xmlSettings = new SettingsSerializer(this);
			LoadSettings();

			//Discord Bot Stuff
			voiceStream = null;
			formatInfo = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
			try {
				bot = new DiscordSocketClient();
                commands = new CommandService();
                services = new ServiceCollection().BuildServiceProvider();
            } catch (PlatformNotSupportedException) {
				Log("Unsupported Operating System. Bot may not work correctly.");
				bot = new DiscordSocketClient(new DiscordSocketConfig {
					WebSocketProvider = WS4NetProvider.Instance,
					UdpSocketProvider = UDPClientProvider.Instance,
				});
			}
			try {
				bot.Ready += Bot_Ready;
                ActGlobals.oFormActMain.OnCombatEnd += OFormActMain_OnCombatEnd;
                await commands.AddModuleAsync(typeof(DiscordTriggers));
                await bot.LoginAsync(TokenType.Bot, txtToken.Text);
				await bot.StartAsync();
                DiscordTriggers.Init(this);
                bot.MessageReceived += Bot_MessageReceived;
                Log("Plugin loaded successfully.");
			} catch (Exception ex) {
				Log("Error connecting bot. Make sure your Bot Token is correct then restart the plugin (Go to \"Plugin Listing\" tab, uncheck \"Enabled\" and then check it again).");
				Log(ex.Message);
			}
			lblStatus.Text = "Plugin Started";
		}

		public async void DeInitPlugin() {
			ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
			ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
			SaveSettings();
			try {
				bot.Ready -= Bot_Ready;
				if (audioClient?.ConnectionState == ConnectionState.Connected) {
					voiceStream?.Close();
					await audioClient.StopAsync();
				}
				await bot.StopAsync();
				await bot.LogoutAsync();
			} catch (Exception ex) {
				ActGlobals.oFormActMain.WriteExceptionLog(ex, "Error with DeInit of Discord Plugin.");
			}
			lblStatus.Text = "Plugin Exited";
		}

		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			try {
				var asm = new AssemblyName(args.Name);
				var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);
				string file;
				if (plugin != null) {
					file = plugin.pluginFile.DirectoryName;
					file = Path.Combine(file, asm.Name + ".dll");
					if (File.Exists(file)) {
						return Assembly.LoadFile(file);
					}
				}
				file = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Plugins\\Discord");
				file = Path.Combine(file, asm.Name + ".dll");
				if (File.Exists(file)) {
					return Assembly.LoadFrom(file);
				}
			} catch (Exception ex) {
				ActGlobals.oFormActMain.WriteExceptionLog(ex, "Error with loading an assembly for Discord Plugin.");
			}
			return null;
		}
		#endregion

		#region Discord Methods
		private void speak(string text) {
            if (voiceStream == null)
                voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 128 * 1024);
            SpeechSynthesizer tts = new SpeechSynthesizer();
            tts.SelectVoice((string)cmbTTS.SelectedItem);
            tts.Volume = sliderTTSVol.Value * 5;
            tts.Rate = sliderTTSSpeed.Value - 10;
            MemoryStream ms = new MemoryStream();
            tts.SetOutputToAudioStream(ms, formatInfo);

            tts.Speak(text);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(voiceStream);
            voiceStream.Flush();
        }

		private void speakFile(string path, int volume) {
			if (voiceStream == null)
				voiceStream = audioClient.CreatePCMStream(AudioApplication.Voice, 128 * 1024);
			try {
				WaveFileReader wav = new WaveFileReader(path);
				WaveFormat waveFormat = new WaveFormat(48000, 16, 2);
				WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(wav);
				WaveFormatConversionStream output = new WaveFormatConversionStream(waveFormat, pcm);
				output.CopyTo(voiceStream);
				voiceStream.Flush();
			} catch (Exception ex) {
				Log("Unable to read file: " + ex.Message);
			}
		}

		private void populateServers() {
			try {
				cmbServer.Items.Clear();
				cmbChan.Items.Clear();
				foreach (SocketGuild g in bot.Guilds)
					cmbServer.Items.Add(g);
				if (cmbServer.Items.Count > 0)
					cmbServer.SelectedIndex = 0;
			} catch (Exception ex) {
				Log("Error populating servers.");
				Log(ex.Message);
			}
		}

		private void populateChannels(SocketGuild g) {
			try {
				cmbChan.Items.Clear();
				var channels = new List<SocketVoiceChannel>(g.VoiceChannels);
				channels.Sort((x, y) => x.Position.CompareTo(y.Position));
				cmbChan.Items.AddRange(channels.ToArray());
				if (cmbChan.Items.Count > 0)
					cmbChan.SelectedIndex = 0;
			} catch (Exception ex) {
				Log("Error populating channels.");
				Log(ex.Message);
			}
		}
		#endregion

		#region UI Events
		private async void btnJoin_Click(object sender, EventArgs e) {
			btnJoin.Enabled = false;
			SocketVoiceChannel chan = (SocketVoiceChannel) cmbChan.SelectedItem;
			try {
				audioClient = await chan.ConnectAsync();
				Log("Joined channel: " + chan.Name);
				btnLeave.Enabled = true;
				ActGlobals.oFormActMain.PlayTtsMethod = ParseTrigger;
				ActGlobals.oFormActMain.PlaySoundMethod = speakFile;
				speak(" ");
			} catch (Exception ex) {
				Log("Unable to join channel. Does your bot have permission to join this channel?");
				btnJoin.Enabled = true;
				populateServers();
				Log(ex.Message);
				return;
			}
		}

		private void btnLeave_Click(object sender, EventArgs e) {
			btnLeave.Enabled = false;
			try {
				bot.SetStatusAsync(UserStatus.Offline);
				voiceStream?.Close();
				voiceStream = null;
				audioClient.StopAsync();
				btnJoin.Enabled = true;
				btnLeave.Enabled = false;
				Log("Left channel.");
				ActGlobals.oFormActMain.PlayTtsMethod = ActGlobals.oFormActMain.TTS;
				ActGlobals.oFormActMain.PlaySoundMethod = ActGlobals.oFormActMain.PlaySoundWmpApi;
				btnJoin.Enabled = true;
			} catch (Exception ex) {
				Log("Error leaving channel. Possible connection issue.");
				btnLeave.Enabled = true;
				Log(ex.Message);
			}
		}

		private void cmbServer_SelectedIndexChanged(object sender, EventArgs e) {
			populateChannels((SocketGuild) cmbServer.SelectedItem);
		}
		#endregion

		#region Discord Events
		private async Task Bot_Ready() {
			btnJoin.Enabled = true;
			await bot.SetGameAsync("with ACT Triggers");
			populateServers();
		}
        public void Log(string text)
        {
            logBox.AppendText(text + "\n");
        }

        public bool IsConnected()
        {
            return audioClient?.ConnectionState == ConnectionState.Connected;
        }

        private string activePlayer = "You";
        private bool normalCanals = true;

        public void ParseTrigger(string triggerText)
        {
            try
            {
                if (!IsConnected())
                    return;

                string text = triggerText;

                if (triggerText.StartsWith("#"))
                {
                    string player = triggerText.Substring(1);
                    activePlayer = player;
                    return;
                }

                switch (text)
                {
                    case "vault key":
                        text = CreateQoute();
                        break;
                    case "canalnormal":
                        normalCanals = true;
                        SetGameAsync(string.Format("in {0}'s hole", activePlayer));
                        text = string.Format("A portal to {0}'s hole has opened.", activePlayer);
                        break;
                    case "canalhard":
                        normalCanals = false;
                        SetGameAsync(string.Format("in {0}'s secret hole", activePlayer));
                        text = string.Format("A portal to {0}'s secret hole has opened.", activePlayer);
                        break;
                    case "canalend":
                        SetGameAsync(gameStatus[ran.Next(gameStatus.Length)]);
                        return;
                }

                speak(text);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private string[] gameStatus = new string[]
        {
            "with ACT Triggers",
            "with Trains",
            "with Pure Competence",
            "with Namazu Knights",
            "FINAL FANTASY XIV - STORMSBLOOD"
        };

        public void SetGameAsync(string text)
        {
            bot.SetGameAsync(text);
        }

        public string CreateQoute()
        {
            string dir = PickDirection();
            string q = qoutes[ran.Next(qoutes.Length)];

            return string.Format(q, dir);
        }

        private string PickDirection()
        {
            return normalCanals ? directions[ran.Next(directions.Length)] : harddirections[ran.Next(harddirections.Length)];

        }

        private string[] directions = new string[]
        {
            "to the Left",
            "to the Right"
        };

        private string[] harddirections = new string[]
        {
            "to the Left",
            "in the Middle",
            "to the Right"
        };

        private string[] qoutes = new string[]
        {
            "Every door is now {0}.",
            "My 100 percentile is pointing {0}.",
            "The secret to Buttys healing can be found {0}.",
            "Sloppy Right.",
            "Sloppy left",
            "The Sign over there points {0}.",
            "The future predicts {0}.",
            "Try hards pants can be found {0}.",
            "Buttys says {0}.",
            "My map points {0}.",
            "If I was to guess wrong, It would be {0}.",
            "Bel sama o'clock is {0}.",
            "If i was a Geordie, I would pick {0}.",
            "My Lettuce pizza is pointing {0}.",
            "Vivi says {0}.",
            "Ketzia is {0}.",
            "If i was to quit the static... I would go {0}.",
            "Left sword.",
            "Right sword.",
            "Cythers corpse is pointing {0}.",
            "President Trump says {0}.",
            "We should build the Mexican wall on the Right.",
            "We should build the Mexican wall on the Left.",
            "I used a potion and it said go {0}.",
            "Ketzia is always right.",
            "Tell Ketzia we went left",
            "If Brine was here tonight? I think he would of gone left.",
            "Screw you guys! I'm going {0}.",
            "Buttys did too much dps so we go {0}.",
            "Anyone that survived can go {0}.",
            "Bonjour detected! {0} incoming.",
            "My bucket is leaning {0}.",
            "This door smells funny, lets go {0}.",
            "The devil child said go {0}.",
            "The voices in my head say {0}.",
            "Mummy Bel, should we go {0}.",
            "Just like Dark Souls, {0}.",
            "Zero deaths {0}."
        };
        #endregion

        #region Parses
        private void OFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            var channel = bot.GetChannel(282263596434456576) as SocketTextChannel;

            StringBuilder text = new StringBuilder();
            TimeSpan totalTime = encounterInfo.encounter.EndTime - encounterInfo.encounter.StartTime;

            if (totalTime < new TimeSpan(0, 3, 0))
                return;

            //title
            text.AppendLine(encounterInfo.encounter.StartTime.ToShortDateString() + " " + encounterInfo.encounter.StartTime.ToLongTimeString());
            text.AppendLine("**[" + encounterInfo.encounter.ZoneName + "][" + encounterInfo.encounter.Title + "]<" + totalTime + ">**");



            StringBuilder parsedata = new StringBuilder();

            //parsedata.Append(CreateStringWithSpacing("Job", 6));
            parsedata.Append(CreateStringWithSpacing("Player", 10, false));
            parsedata.Append(CreateStringWithSpacing("DPS", 8));
            parsedata.Append(CreateStringWithSpacing("CRT", 6));
            parsedata.Append(CreateStringWithSpacing("DHIT", 6));
            parsedata.Append(CreateStringWithSpacing("DCRT", 6));
            parsedata.Append(CreateStringWithSpacing("Deaths", 8));
            parsedata.Append(CreateStringWithSpacing("Best Hit", 20));
            parsedata.AppendLine();


            List<CombatantData> playerData = encounterInfo.encounter.GetAllies();
            playerData.Sort((x, y) => y.DPS.CompareTo(x.DPS));

            string limitbreak = " Limit Break: ";
            bool usedLimitBreak = false;

            if (playerData.Count < 4)
                return;
            int parselogged = 0;
            for (int i = 0; i < playerData.Count; i++)
            {
                if (playerData[i].Name == "Limit Break")
                {
                    string limitbreakname = FormatMaxHit(playerData[i].GetMaxHit(true));
                    if (string.IsNullOrWhiteSpace(limitbreakname))
                        limitbreakname = "The Holy Grail of Tryhards Pants <7 Nubs Raised>";
                    limitbreak += limitbreakname;
                    usedLimitBreak = true;
                    continue;
                }


                if (parselogged != 0)
                    parsedata.AppendLine();

                double.TryParse(playerData[i].GetColumnByName("DirectHitPct").TrimEnd(new char[] { '%' }), out double directhit);
                double.TryParse(playerData[i].GetColumnByName("CritDirectHitPct").TrimEnd(new char[] { '%' }), out double directhitcrt);

                string dpsString = playerData[i].DPS.ToString();

                //parsedata.Append(CreateStringWithSpacing(":" + playerData[i].GetColumnByName("Job").ToLower() + ":", 6));
                parsedata.Append(CreateStringWithSpacing(SplitName(playerData[i].Name == "YOU" ? "Buttys" : playerData[i].Name), 10, false));
                parsedata.Append(CreateStringWithSpacing(Bracket(dpsString == "NaN" || (int)playerData[i].DPS < 0 ? "∞" : ((int)playerData[i].DPS).ToString()), 8));
                parsedata.Append(CreateStringWithSpacing(Bracket(((int)playerData[i].CritDamPerc).ToString() + "%"), 6));
                parsedata.Append(CreateStringWithSpacing(Bracket(((int)directhit).ToString() + "%"), 6));
                parsedata.Append(CreateStringWithSpacing(Bracket(((int)directhitcrt).ToString() + "%"), 6));
                parsedata.Append(CreateStringWithSpacing(Bracket(playerData[i].Deaths.ToString()), 8));
                parsedata.Append(CreateStringWithSpacing(FormatMaxHit(playerData[i].GetMaxHit(true)), 20));

                parselogged++;
            }

            parsedata = new StringBuilder("```md\n" + parsedata + "```");
            parsedata.AppendLine("Encounter DPS: <" + (int)encounterInfo.encounter.DPS + "> " + (usedLimitBreak ? limitbreak : ""));

            text.AppendLine(parsedata.ToString());

            channel.SendMessageAsync(text.ToString());

            Log("Parse Posted.");

        }

        public string CreateStringWithSpacing(string text, int size, bool centertext = true)
        {
            char spacing = ' ';
            char[] newtext = new char[size];
            if (centertext)
                return CenterString(text, size, spacing);

            for (int i = 0; i < newtext.Length; i++)
            {
                if (i < text.Length)
                    newtext[i] = text[i];
                else
                    newtext[i] = spacing;
            }
            return new string(newtext);
        }

        public string SplitName(string name)
        {
            return name.Split(' ')[0];
        }

        public string Bracket(string text)
        {
            return "<" + text + ">";
        }

        public string CenterString(string stringToCenter, int totalLength, char paddingCharacter)
        {
            return stringToCenter.PadLeft(
                ((totalLength - stringToCenter.Length) / 2) + stringToCenter.Length,
                  paddingCharacter).PadRight(totalLength, paddingCharacter);
        }

        public string FormatMaxHit(string text)
        {
            char splitter = '-';
            string[] splits = text.Split(splitter);
            if (splits.Length < 2)
                return text;
            return splits[0] + " <" + splits[1] + ">";
        }
        #endregion

        #region Commands

        private CommandService commands;
        private IServiceProvider services;
        char prefix = '!';

        private async Task Bot_MessageReceived(SocketMessage arg)
        {
            SocketUserMessage msg = arg as SocketUserMessage;

            if (msg == null)
                return;

            int pos = 0;

            if (!(msg.HasCharPrefix(prefix, ref pos) || msg.HasMentionPrefix(bot.CurrentUser, ref pos)))
                return;

            var content = new CommandContext(bot, msg);

            var result = await commands.ExecuteAsync(content, pos, services);
#if DEBUG
            if (!result.IsSuccess)
                Log(result.ErrorReason + " : " + msg.Content);
#endif
            await msg.DeleteAsync();
        }
        #endregion


        #region Settings
        public void LoadSettings() {
			xmlSettings.AddControlSetting(txtToken.Name, txtToken);
            xmlSettings.AddControlSetting(txtFFLogsToken.Name, txtFFLogsToken);
			xmlSettings.AddControlSetting(sliderTTSVol.Name, sliderTTSVol);
			xmlSettings.AddControlSetting(sliderTTSSpeed.Name, sliderTTSSpeed);
			if (File.Exists(settingsFile)) {
				FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				XmlTextReader xReader = new XmlTextReader(fs);
				try {
					while (xReader.Read())
						if (xReader.NodeType == XmlNodeType.Element)
							if (xReader.LocalName == "SettingsSerializer")
								xmlSettings.ImportFromXml(xReader);
				} catch (Exception ex) {
					lblStatus.Text = "Error loading settings: " + ex.Message;
				}
				xReader.Close();
			}
		}

		public void SaveSettings() {
			FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
			xWriter.Formatting = Formatting.Indented;
			xWriter.Indentation = 1;
			xWriter.IndentChar = '\t';
			xWriter.WriteStartDocument(true);
			xWriter.WriteStartElement("Config");
			xWriter.WriteStartElement("SettingsSerializer");
			xmlSettings.ExportToXml(xWriter);
			xWriter.WriteEndElement();
			xWriter.WriteEndElement();
			xWriter.WriteEndDocument();
			xWriter.Flush();
			xWriter.Close();
		}
        #endregion
    }
}