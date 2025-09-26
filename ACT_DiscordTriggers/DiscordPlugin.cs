using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Speech.Synthesis;
using System.Reflection;
using DiscordAPI;
using System.Windows.Threading;

namespace ACT_DiscordTriggers {
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
      this.chkAutoConnect = new System.Windows.Forms.CheckBox();
      this.discordConnectbtn = new System.Windows.Forms.Button();
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
      this.txtToken = new System.Windows.Forms.TextBox();
      this.lblBotTok = new System.Windows.Forms.Label();
      this.logList = new System.Windows.Forms.ListView();
      this.listColTim = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.listColMsg = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.txtBotStatus = new System.Windows.Forms.TextBox();
      this.lblBotStatus = new System.Windows.Forms.Label();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).BeginInit();
      this.SuspendLayout();
      // 
      // chkAutoConnect
      // 
      this.chkAutoConnect.AutoSize = true;
      this.chkAutoConnect.Location = new System.Drawing.Point(180, 98);
      this.chkAutoConnect.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.chkAutoConnect.Name = "chkAutoConnect";
      this.chkAutoConnect.Size = new System.Drawing.Size(133, 24);
      this.chkAutoConnect.TabIndex = 59;
      this.chkAutoConnect.Text = "Auto Connect";
      this.chkAutoConnect.UseVisualStyleBackColor = true;
      // 
      // discordConnectbtn
      // 
      this.discordConnectbtn.Location = new System.Drawing.Point(32, 92);
      this.discordConnectbtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.discordConnectbtn.Name = "discordConnectbtn";
      this.discordConnectbtn.Size = new System.Drawing.Size(140, 35);
      this.discordConnectbtn.TabIndex = 58;
      this.discordConnectbtn.Text = "Connect";
      this.discordConnectbtn.UseVisualStyleBackColor = true;
      this.discordConnectbtn.Click += new System.EventHandler(this.discordConnectbtn_Click);
      // 
      // sliderTTSSpeed
      // 
      this.sliderTTSSpeed.Location = new System.Drawing.Point(432, 177);
      this.sliderTTSSpeed.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.sliderTTSSpeed.Maximum = 20;
      this.sliderTTSSpeed.Name = "sliderTTSSpeed";
      this.sliderTTSSpeed.Size = new System.Drawing.Size(290, 69);
      this.sliderTTSSpeed.TabIndex = 57;
      this.sliderTTSSpeed.Value = 10;
      // 
      // lblTTSSpeed
      // 
      this.lblTTSSpeed.AutoSize = true;
      this.lblTTSSpeed.Location = new System.Drawing.Point(428, 152);
      this.lblTTSSpeed.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTSSpeed.Name = "lblTTSSpeed";
      this.lblTTSSpeed.Size = new System.Drawing.Size(89, 20);
      this.lblTTSSpeed.TabIndex = 56;
      this.lblTTSSpeed.Text = "TTS Speed";
      // 
      // sliderTTSVol
      // 
      this.sliderTTSVol.Location = new System.Drawing.Point(432, 103);
      this.sliderTTSVol.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.sliderTTSVol.Maximum = 20;
      this.sliderTTSVol.Name = "sliderTTSVol";
      this.sliderTTSVol.Size = new System.Drawing.Size(290, 69);
      this.sliderTTSVol.TabIndex = 55;
      this.sliderTTSVol.Value = 10;
      // 
      // lblTTSVol
      // 
      this.lblTTSVol.AutoSize = true;
      this.lblTTSVol.Location = new System.Drawing.Point(428, 83);
      this.lblTTSVol.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTSVol.Name = "lblTTSVol";
      this.lblTTSVol.Size = new System.Drawing.Size(96, 20);
      this.lblTTSVol.TabIndex = 54;
      this.lblTTSVol.Text = "TTS Volume";
      // 
      // cmbChan
      // 
      this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbChan.FormattingEnabled = true;
      this.cmbChan.Location = new System.Drawing.Point(32, 245);
      this.cmbChan.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbChan.Name = "cmbChan";
      this.cmbChan.Size = new System.Drawing.Size(288, 28);
      this.cmbChan.TabIndex = 53;
      // 
      // lblChan
      // 
      this.lblChan.AutoSize = true;
      this.lblChan.Location = new System.Drawing.Point(27, 217);
      this.lblChan.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblChan.Name = "lblChan";
      this.lblChan.Size = new System.Drawing.Size(68, 20);
      this.lblChan.TabIndex = 52;
      this.lblChan.Text = "Channel";
      // 
      // cmbServer
      // 
      this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbServer.FormattingEnabled = true;
      this.cmbServer.Location = new System.Drawing.Point(32, 174);
      this.cmbServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbServer.Name = "cmbServer";
      this.cmbServer.Size = new System.Drawing.Size(288, 28);
      this.cmbServer.TabIndex = 51;
      this.cmbServer.SelectedValueChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
      // 
      // lblServer
      // 
      this.lblServer.AutoSize = true;
      this.lblServer.Location = new System.Drawing.Point(27, 152);
      this.lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblServer.Name = "lblServer";
      this.lblServer.Size = new System.Drawing.Size(55, 20);
      this.lblServer.TabIndex = 50;
      this.lblServer.Text = "Server";
      // 
      // cmbTTS
      // 
      this.cmbTTS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbTTS.FormattingEnabled = true;
      this.cmbTTS.Location = new System.Drawing.Point(432, 50);
      this.cmbTTS.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbTTS.Name = "cmbTTS";
      this.cmbTTS.Size = new System.Drawing.Size(288, 28);
      this.cmbTTS.TabIndex = 49;
      // 
      // lblTTS
      // 
      this.lblTTS.AutoSize = true;
      this.lblTTS.Location = new System.Drawing.Point(428, 28);
      this.lblTTS.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTS.Name = "lblTTS";
      this.lblTTS.Size = new System.Drawing.Size(82, 20);
      this.lblTTS.TabIndex = 48;
      this.lblTTS.Text = "TTS Voice";
      // 
      // btnLeave
      // 
      this.btnLeave.Enabled = false;
      this.btnLeave.Location = new System.Drawing.Point(180, 286);
      this.btnLeave.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.btnLeave.Name = "btnLeave";
      this.btnLeave.Size = new System.Drawing.Size(141, 35);
      this.btnLeave.TabIndex = 47;
      this.btnLeave.Text = "Leave Channel";
      this.btnLeave.UseVisualStyleBackColor = true;
      this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
      // 
      // btnJoin
      // 
      this.btnJoin.Enabled = false;
      this.btnJoin.Location = new System.Drawing.Point(32, 285);
      this.btnJoin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.btnJoin.Name = "btnJoin";
      this.btnJoin.Size = new System.Drawing.Size(140, 35);
      this.btnJoin.TabIndex = 46;
      this.btnJoin.Text = "Join Channel";
      this.btnJoin.UseVisualStyleBackColor = true;
      this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
      // 
      // lblLog
      // 
      this.lblLog.AutoSize = true;
      this.lblLog.Location = new System.Drawing.Point(27, 368);
      this.lblLog.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblLog.Name = "lblLog";
      this.lblLog.Size = new System.Drawing.Size(88, 20);
      this.lblLog.TabIndex = 45;
      this.lblLog.Text = "Debug Log";
      // 
      // txtToken
      // 
      this.txtToken.Location = new System.Drawing.Point(32, 52);
      this.txtToken.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.txtToken.Name = "txtToken";
      this.txtToken.Size = new System.Drawing.Size(288, 26);
      this.txtToken.TabIndex = 43;
      this.txtToken.UseSystemPasswordChar = true;
      // 
      // lblBotTok
      // 
      this.lblBotTok.AutoSize = true;
      this.lblBotTok.Location = new System.Drawing.Point(27, 28);
      this.lblBotTok.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblBotTok.Name = "lblBotTok";
      this.lblBotTok.Size = new System.Drawing.Size(140, 20);
      this.lblBotTok.TabIndex = 42;
      this.lblBotTok.Text = "Discord Bot Token";
      // 
      // logList
      // 
      this.logList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.logList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.listColTim,
            this.listColMsg});
      this.logList.FullRowSelect = true;
      this.logList.HideSelection = false;
      this.logList.Location = new System.Drawing.Point(32, 392);
      this.logList.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.logList.Name = "logList";
      this.logList.Size = new System.Drawing.Size(690, 189);
      this.logList.TabIndex = 61;
      this.logList.UseCompatibleStateImageBehavior = false;
      this.logList.View = System.Windows.Forms.View.Details;
      this.logList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LogList_KeyUp);
      // 
      // listColTim
      // 
      this.listColTim.Text = "Timestamp";
      this.listColTim.Width = 120;
      // 
      // listColMsg
      // 
      this.listColMsg.Text = "Message";
      this.listColMsg.Width = 315;
      // 
      // txtBotStatus
      // 
      this.txtBotStatus.Location = new System.Drawing.Point(432, 254);
      this.txtBotStatus.Name = "txtBotStatus";
      this.txtBotStatus.Size = new System.Drawing.Size(289, 26);
      this.txtBotStatus.TabIndex = 62;
      this.txtBotStatus.Text = "Playing with ACT Triggers";
      this.txtBotStatus.TextChanged += new System.EventHandler(this.txtBotStatus_TextChanged);
      // 
      // lblBotStatus
      // 
      this.lblBotStatus.AutoSize = true;
      this.lblBotStatus.Location = new System.Drawing.Point(428, 226);
      this.lblBotStatus.Name = "lblBotStatus";
      this.lblBotStatus.Size = new System.Drawing.Size(85, 20);
      this.lblBotStatus.TabIndex = 63;
      this.lblBotStatus.Text = "Bot Status";
      // 
      // DiscordPlugin
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.lblBotStatus);
      this.Controls.Add(this.txtBotStatus);
      this.Controls.Add(this.logList);
      this.Controls.Add(this.chkAutoConnect);
      this.Controls.Add(this.discordConnectbtn);
      this.Controls.Add(this.sliderTTSSpeed);
      this.Controls.Add(this.lblTTSSpeed);
      this.Controls.Add(this.sliderTTSVol);
      this.Controls.Add(this.lblTTSVol);
      this.Controls.Add(this.cmbChan);
      this.Controls.Add(this.lblChan);
      this.Controls.Add(this.cmbServer);
      this.Controls.Add(this.lblServer);
      this.Controls.Add(this.cmbTTS);
      this.Controls.Add(this.lblTTS);
      this.Controls.Add(this.btnLeave);
      this.Controls.Add(this.btnJoin);
      this.Controls.Add(this.lblLog);
      this.Controls.Add(this.txtToken);
      this.Controls.Add(this.lblBotTok);
      this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.Name = "DiscordPlugin";
      this.Size = new System.Drawing.Size(759, 608);
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    #endregion

    #endregion

    #region Init Variables
    FormActMain.PlayTtsDelegate oldTTS;
    FormActMain.PlaySoundDelegate oldSound;
    Label lblStatus;
    string settingsFile;
    SettingsSerializer xmlSettings;
    private CheckBox chkAutoConnect;
    private Button discordConnectbtn;
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
    private TextBox txtToken;
    private ListView logList;
    private ColumnHeader listColTim;
    private ColumnHeader listColMsg;
    private TextBox txtBotStatus;
    private Label lblBotStatus;
    private Label lblBotTok;
    private readonly DispatcherTimer statusDebounceTimer = new DispatcherTimer();
    #endregion

    public DiscordPlugin() {
      //Load UI Components and Assemblies
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      InitializeComponent();
      InitializeDebounceTimer();

      //Add installed voices to dropdown
      var tts = new SpeechSynthesizer();
      foreach (InstalledVoice v in tts.GetInstalledVoices())
        cmbTTS.Items.Add(v.VoiceInfo.Name);
      cmbTTS.SelectedIndex = 0;
    }

    private void InitializeDebounceTimer() {
      statusDebounceTimer.Interval = TimeSpan.FromMilliseconds(10000);
      statusDebounceTimer.Tick += DebounceTimer_Tick;
    }

    private async void DebounceTimer_Tick(object sender, EventArgs e) {
      statusDebounceTimer.Stop();
      await DiscordClient.SetGameAsync(txtBotStatus.Text);
    }

    #region IActPluginV1 Members
    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
      //ACT Stuff
      oldTTS = ActGlobals.oFormActMain.PlayTtsMethod;
      oldSound = ActGlobals.oFormActMain.PlaySoundMethod;
      lblStatus = pluginStatusText;
      pluginScreenSpace.Controls.Add(this);
      pluginScreenSpace.Text = "Discord Triggers";
      Dock = DockStyle.Fill;

      //Get plugin name
      string configName = "ACT_DiscordTriggers";
      try {
        string pluginName = ActGlobals.oFormActMain.PluginGetSelfData(this).pluginFile.FullName;
        pluginName = Path.GetFileNameWithoutExtension(pluginName).Trim();
        if (pluginName.Length >= 0)
          configName = pluginName;
      } catch (Exception) { }

      //Load Settings file
      settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, $"Config\\{configName}.config.xml");
      xmlSettings = new SettingsSerializer(this);
      LoadSettings();

      //Discord Bot Stuff
      DiscordClient.BotReady += BotReady;
      DiscordClient.Log += Log;

      if (chkAutoConnect.Checked)
        discordConnectbtn_Click(null, EventArgs.Empty);

      lblStatus.Text = "Plugin Started";
    }

    public async void DeInitPlugin() {
      ActGlobals.oFormActMain.PlayTtsMethod = oldTTS;
      ActGlobals.oFormActMain.PlaySoundMethod = oldSound;
      SaveSettings();
      try {
        await DiscordClient.deInIt();
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
      Log("Playing TTS for text: " + text);
      try {
        DiscordClient.Speak(text, cmbTTS.SelectedItem.ToString(), sliderTTSVol.Value, sliderTTSSpeed.Value);
      } catch (Exception ex) {
        Log("Error playing TTS");
        Log(ex.Message);
      }
    }

    private void speakFile(string path, int volume) {
      Log("Playing Audio file: " + path);
      try {
        DiscordClient.SpeakFile(path);
      } catch (Exception ex) {
        Log("Error playing File");
        Log(ex.Message);
      }
    }

    private void BotReady() {
      btnJoin.Enabled = true;
      populateServers();
    }


    private void populateServers() {
      try {
        string[] servers = DiscordClient.getServers();
        Log("Found " + servers.Length + " discord server(s).");

        cmbServer.Items.Clear();
        cmbChan.Items.Clear();

        foreach (string server in servers)
          cmbServer.Items.Add(server);

        if (cmbServer.Items.Count > 0)
          cmbServer.SelectedIndex = 0;
      } catch (Exception ex) {
        Log("Error populating servers.");
        Log(ex.Message);
      }
    }

    private void populateChannels(string server) {
      try {
        cmbChan.Items.Clear();
        cmbChan.Items.AddRange(DiscordClient.getChannels(server));
        if (cmbChan.Items.Count > 0) {
          cmbChan.SelectedIndex = 0;
          Log("Found " + cmbChan.Items.Count + " available voice channel(s) for " + server);
        } else {
          Log("Error: Could not find any available voice channels for " + server);
        }
      } catch (Exception ex) {
        Log("Error populating channels.");
        Log(ex.Message);
      }
    }
    #endregion

    #region UI Events
    private async void btnJoin_Click(object sender, EventArgs e) {
      btnJoin.Enabled = false;
      if (await DiscordClient.JoinChannel(cmbServer.SelectedItem.ToString(), cmbChan.SelectedItem.ToString())) {
        btnLeave.Enabled = true;
        ActGlobals.oFormActMain.PlayTtsMethod = speak;
        ActGlobals.oFormActMain.PlaySoundMethod = speakFile;
      } else {
        Log("Unable to join channel. Does your bot have permission to join this channel?");
        btnJoin.Enabled = true;
        populateServers();
      }
    }

    private void btnLeave_Click(object sender, EventArgs e) {
      btnLeave.Enabled = false;
      try {
        DiscordClient.LeaveChannel();
        btnJoin.Enabled = true;
        btnLeave.Enabled = false;
        Log("Left channel.");
        ActGlobals.oFormActMain.PlayTtsMethod = oldTTS;
        ActGlobals.oFormActMain.PlaySoundMethod = oldSound;
        btnJoin.Enabled = true;
      } catch (Exception ex) {
        Log("Error leaving channel. Possible connection issue.");
        btnLeave.Enabled = true;
        Log(ex.Message);
      }
    }

    private void cmbServer_SelectedIndexChanged(object sender, EventArgs e) {
      populateChannels(cmbServer.SelectedItem.ToString());
    }

    private void discordConnectbtn_Click(object sender, EventArgs e) {
      if (DiscordClient.IsConnected()) {
        Log("Already connected to Discord.");
        return;
      }
      DiscordClient.InIt(txtToken.Text, txtBotStatus.Text);
    }

    private void LogList_KeyUp(object sender, KeyEventArgs e) {
      if (sender != logList)
        return;

      if (e.Control && e.KeyCode == Keys.C && logList.SelectedItems.Count > 0) {
        var builder = new StringBuilder();
        foreach (ListViewItem item in logList.SelectedItems)
          builder.AppendLine(item.SubItems[1].Text);

        string clipboard = builder.ToString();
        if (clipboard.Length > 0)
          Clipboard.SetText(builder.ToString());
      }
    }

    private void txtBotStatus_TextChanged(object sender, EventArgs e) {
       statusDebounceTimer.Stop();
       statusDebounceTimer.Start();
    }
    #endregion

    #region Settings
    public void Log(string text) {
      string[] row = new string[2];
      row[0] = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
      row[1] = text;
      logList.Items.Add(new ListViewItem(row));
    }

    public void LoadSettings() {
      xmlSettings.AddControlSetting(txtToken.Name, txtToken);
      xmlSettings.AddControlSetting(sliderTTSVol.Name, sliderTTSVol);
      xmlSettings.AddControlSetting(sliderTTSSpeed.Name, sliderTTSSpeed);
      xmlSettings.AddControlSetting(chkAutoConnect.Name, chkAutoConnect);
      xmlSettings.AddControlSetting(txtBotStatus.Name, txtBotStatus);
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

    public bool SaveSettings() {
      try {
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
      } catch {
        return false;
      }
      return true;
    }
    #endregion
  }
}