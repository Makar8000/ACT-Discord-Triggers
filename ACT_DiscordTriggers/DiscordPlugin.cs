using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Reflection;
using DiscordAPI;

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
			this.cmbChan = new System.Windows.Forms.ComboBox();
			this.lblChan = new System.Windows.Forms.Label();
			this.cmbServer = new System.Windows.Forms.ComboBox();
			this.lblServer = new System.Windows.Forms.Label();
			this.btnLeave = new System.Windows.Forms.Button();
			this.btnJoin = new System.Windows.Forms.Button();
			this.lblLog = new System.Windows.Forms.Label();
			this.txtToken = new System.Windows.Forms.TextBox();
			this.lblBotTok = new System.Windows.Forms.Label();
			this.logList = new System.Windows.Forms.ListView();
			this.listColTim = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.listColMsg = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.txtArguments = new System.Windows.Forms.TextBox();
			this.lblArguments = new System.Windows.Forms.Label();
			this.lblTTSBinary = new System.Windows.Forms.Label();
			this.txtTTSBinaryPath = new System.Windows.Forms.TextBox();
			this.btnSelectBinary = new System.Windows.Forms.Button();
			this.lstLogs = new System.Windows.Forms.ListBox();
			this.lblPipe = new System.Windows.Forms.Label();
			this.chkUsePipe = new System.Windows.Forms.CheckBox();
			this.lblUseSocket = new System.Windows.Forms.Label();
			this.chkUseSocket = new System.Windows.Forms.CheckBox();
			this.opnTTS = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			// 
			// chkAutoConnect
			// 
			this.chkAutoConnect.AutoSize = true;
			this.chkAutoConnect.Location = new System.Drawing.Point(120, 64);
			this.chkAutoConnect.Name = "chkAutoConnect";
			this.chkAutoConnect.Size = new System.Drawing.Size(91, 17);
			this.chkAutoConnect.TabIndex = 59;
			this.chkAutoConnect.Text = "Auto Connect";
			this.chkAutoConnect.UseVisualStyleBackColor = true;
			// 
			// discordConnectbtn
			// 
			this.discordConnectbtn.Location = new System.Drawing.Point(21, 60);
			this.discordConnectbtn.Name = "discordConnectbtn";
			this.discordConnectbtn.Size = new System.Drawing.Size(93, 23);
			this.discordConnectbtn.TabIndex = 58;
			this.discordConnectbtn.Text = "Connect";
			this.discordConnectbtn.UseVisualStyleBackColor = true;
			this.discordConnectbtn.Click += new System.EventHandler(this.discordConnectbtn_Click);
			// 
			// cmbChan
			// 
			this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbChan.FormattingEnabled = true;
			this.cmbChan.Location = new System.Drawing.Point(21, 159);
			this.cmbChan.Name = "cmbChan";
			this.cmbChan.Size = new System.Drawing.Size(193, 21);
			this.cmbChan.TabIndex = 53;
			// 
			// lblChan
			// 
			this.lblChan.AutoSize = true;
			this.lblChan.Location = new System.Drawing.Point(18, 141);
			this.lblChan.Name = "lblChan";
			this.lblChan.Size = new System.Drawing.Size(46, 13);
			this.lblChan.TabIndex = 52;
			this.lblChan.Text = "Channel";
			// 
			// cmbServer
			// 
			this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbServer.FormattingEnabled = true;
			this.cmbServer.Location = new System.Drawing.Point(21, 113);
			this.cmbServer.Name = "cmbServer";
			this.cmbServer.Size = new System.Drawing.Size(193, 21);
			this.cmbServer.TabIndex = 51;
			this.cmbServer.SelectedValueChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
			// 
			// lblServer
			// 
			this.lblServer.AutoSize = true;
			this.lblServer.Location = new System.Drawing.Point(18, 99);
			this.lblServer.Name = "lblServer";
			this.lblServer.Size = new System.Drawing.Size(38, 13);
			this.lblServer.TabIndex = 50;
			this.lblServer.Text = "Server";
			// 
			// btnLeave
			// 
			this.btnLeave.Enabled = false;
			this.btnLeave.Location = new System.Drawing.Point(120, 186);
			this.btnLeave.Name = "btnLeave";
			this.btnLeave.Size = new System.Drawing.Size(94, 23);
			this.btnLeave.TabIndex = 47;
			this.btnLeave.Text = "Leave Channel";
			this.btnLeave.UseVisualStyleBackColor = true;
			this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
			// 
			// btnJoin
			// 
			this.btnJoin.Enabled = false;
			this.btnJoin.Location = new System.Drawing.Point(21, 185);
			this.btnJoin.Name = "btnJoin";
			this.btnJoin.Size = new System.Drawing.Size(93, 23);
			this.btnJoin.TabIndex = 46;
			this.btnJoin.Text = "Join Channel";
			this.btnJoin.UseVisualStyleBackColor = true;
			this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
			// 
			// lblLog
			// 
			this.lblLog.AutoSize = true;
			this.lblLog.Location = new System.Drawing.Point(18, 239);
			this.lblLog.Name = "lblLog";
			this.lblLog.Size = new System.Drawing.Size(60, 13);
			this.lblLog.TabIndex = 45;
			this.lblLog.Text = "Debug Log";
			// 
			// txtToken
			// 
			this.txtToken.Location = new System.Drawing.Point(21, 34);
			this.txtToken.Name = "txtToken";
			this.txtToken.Size = new System.Drawing.Size(193, 20);
			this.txtToken.TabIndex = 43;
			this.txtToken.UseSystemPasswordChar = true;
			// 
			// lblBotTok
			// 
			this.lblBotTok.AutoSize = true;
			this.lblBotTok.Location = new System.Drawing.Point(18, 18);
			this.lblBotTok.Name = "lblBotTok";
			this.lblBotTok.Size = new System.Drawing.Size(96, 13);
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
			this.logList.Location = new System.Drawing.Point(21, 255);
			this.logList.Name = "logList";
			this.logList.Size = new System.Drawing.Size(854, 124);
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
			// txtArguments
			// 
			this.txtArguments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtArguments.Location = new System.Drawing.Point(292, 15);
			this.txtArguments.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.txtArguments.Name = "txtArguments";
			this.txtArguments.Size = new System.Drawing.Size(583, 20);
			this.txtArguments.TabIndex = 62;
			this.txtArguments.Text = "-a 15 -g 0 -p 50 -s 175";
			this.txtArguments.Leave += new System.EventHandler(this.TxtArguments_Leave);
			// 
			// lblArguments
			// 
			this.lblArguments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblArguments.AutoSize = true;
			this.lblArguments.Location = new System.Drawing.Point(221, 18);
			this.lblArguments.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblArguments.Name = "lblArguments";
			this.lblArguments.Size = new System.Drawing.Size(57, 13);
			this.lblArguments.TabIndex = 63;
			this.lblArguments.Text = "Arguments";
			this.lblArguments.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblTTSBinary
			// 
			this.lblTTSBinary.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblTTSBinary.AutoSize = true;
			this.lblTTSBinary.Location = new System.Drawing.Point(224, 70);
			this.lblTTSBinary.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblTTSBinary.Name = "lblTTSBinary";
			this.lblTTSBinary.Size = new System.Drawing.Size(60, 13);
			this.lblTTSBinary.TabIndex = 65;
			this.lblTTSBinary.Text = "TTS Binary";
			this.lblTTSBinary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtTTSBinaryPath
			// 
			this.txtTTSBinaryPath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtTTSBinaryPath.Location = new System.Drawing.Point(292, 70);
			this.txtTTSBinaryPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.txtTTSBinaryPath.Name = "txtTTSBinaryPath";
			this.txtTTSBinaryPath.ReadOnly = true;
			this.txtTTSBinaryPath.Size = new System.Drawing.Size(485, 20);
			this.txtTTSBinaryPath.TabIndex = 64;
			this.txtTTSBinaryPath.Text = "Z:\\usr\\bin\\espeak";
			this.txtTTSBinaryPath.Leave += new System.EventHandler(this.TxtBinaryPath_Leave);
			// 
			// btnSelectBinary
			// 
			this.btnSelectBinary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSelectBinary.Location = new System.Drawing.Point(785, 70);
			this.btnSelectBinary.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.btnSelectBinary.Name = "btnSelectBinary";
			this.btnSelectBinary.Size = new System.Drawing.Size(90, 20);
			this.btnSelectBinary.TabIndex = 66;
			this.btnSelectBinary.Text = "Select TTS Binary";
			this.btnSelectBinary.UseVisualStyleBackColor = true;
			this.btnSelectBinary.Click += new System.EventHandler(this.BtnSelectBinary_Click);
			// 
			// lstLogs
			// 
			this.lstLogs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lstLogs.FormattingEnabled = true;
			this.lstLogs.Location = new System.Drawing.Point(227, 102);
			this.lstLogs.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.lstLogs.Name = "lstLogs";
			this.lstLogs.Size = new System.Drawing.Size(648, 147);
			this.lstLogs.TabIndex = 67;
			// 
			// lblPipe
			// 
			this.lblPipe.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblPipe.AutoSize = true;
			this.lblPipe.Location = new System.Drawing.Point(221, 37);
			this.lblPipe.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblPipe.Name = "lblPipe";
			this.lblPipe.Size = new System.Drawing.Size(50, 13);
			this.lblPipe.TabIndex = 68;
			this.lblPipe.Text = "Use Pipe";
			this.lblPipe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// chkUsePipe
			// 
			this.chkUsePipe.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.chkUsePipe.AutoSize = true;
			this.chkUsePipe.Location = new System.Drawing.Point(292, 37);
			this.chkUsePipe.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.chkUsePipe.Name = "chkUsePipe";
			this.chkUsePipe.Size = new System.Drawing.Size(15, 14);
			this.chkUsePipe.TabIndex = 70;
			this.chkUsePipe.UseVisualStyleBackColor = true;
			this.chkUsePipe.CheckedChanged += new System.EventHandler(this.ChkUsePipe_CheckedChanged);
			// 
			// lblUseSocket
			// 
			this.lblUseSocket.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblUseSocket.AutoSize = true;
			this.lblUseSocket.Location = new System.Drawing.Point(221, 54);
			this.lblUseSocket.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblUseSocket.Name = "lblUseSocket";
			this.lblUseSocket.Size = new System.Drawing.Size(63, 13);
			this.lblUseSocket.TabIndex = 69;
			this.lblUseSocket.Text = "Use Socket";
			this.lblUseSocket.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// chkUseSocket
			// 
			this.chkUseSocket.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.chkUseSocket.AutoSize = true;
			this.chkUseSocket.Location = new System.Drawing.Point(292, 53);
			this.chkUseSocket.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.chkUseSocket.Name = "chkUseSocket";
			this.chkUseSocket.Size = new System.Drawing.Size(15, 14);
			this.chkUseSocket.TabIndex = 71;
			this.chkUseSocket.UseVisualStyleBackColor = true;
			this.chkUseSocket.CheckedChanged += new System.EventHandler(this.ChkUseSocket_CheckChanged);
			// 
			// opnTTS
			// 
			this.opnTTS.FileName = "Select TTS Binary";
			this.opnTTS.FileOk += new System.ComponentModel.CancelEventHandler(this.OpnTTS_FileOk);
			// 
			// DiscordPlugin
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.lblPipe);
			this.Controls.Add(this.chkUsePipe);
			this.Controls.Add(this.lblUseSocket);
			this.Controls.Add(this.chkUseSocket);
			this.Controls.Add(this.lstLogs);
			this.Controls.Add(this.lblTTSBinary);
			this.Controls.Add(this.txtTTSBinaryPath);
			this.Controls.Add(this.btnSelectBinary);
			this.Controls.Add(this.lblArguments);
			this.Controls.Add(this.txtArguments);
			this.Controls.Add(this.logList);
			this.Controls.Add(this.chkAutoConnect);
			this.Controls.Add(this.discordConnectbtn);
			this.Controls.Add(this.cmbChan);
			this.Controls.Add(this.lblChan);
			this.Controls.Add(this.cmbServer);
			this.Controls.Add(this.lblServer);
			this.Controls.Add(this.btnLeave);
			this.Controls.Add(this.btnJoin);
			this.Controls.Add(this.lblLog);
			this.Controls.Add(this.txtToken);
			this.Controls.Add(this.lblBotTok);
			this.Name = "DiscordPlugin";
			this.Size = new System.Drawing.Size(899, 395);
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
    private ComboBox cmbChan;
    private Label lblChan;
    private ComboBox cmbServer;
    private Label lblServer;
    private Button btnLeave;
    private Button btnJoin;
    private Label lblLog;
    private TextBox txtToken;
    private ListView logList;
    private ColumnHeader listColTim;
    private ColumnHeader listColMsg;
    private Label lblBotTok;
		#endregion

		private TextBox txtArguments;
		private Label lblArguments;
		private Label lblTTSBinary;
		private TextBox txtTTSBinaryPath;
		private Button btnSelectBinary;
		private ListBox lstLogs;
		private Label lblPipe;
		private CheckBox chkUsePipe;
		private Label lblUseSocket;
		private CheckBox chkUseSocket;
		private TTSHandler ttsHandler;
	public DiscordPlugin() {
      //Load UI Components and Assemblies
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      InitializeComponent();

      //Add installed voices to dropdown
      //var tts = new SpeechSynthesizer();
      //foreach (InstalledVoice v in tts.GetInstalledVoices())
      //  cmbTTS.Items.Add(v.VoiceInfo.Name);
      //cmbTTS.SelectedIndex = 0;
    }

	#region LinuxTTS
	private OpenFileDialog opnTTS;

	void PlayTTS(string text)
	{
		if (chkUsePipe.Checked)
		{
			ttsHandler.Play(text);
		}
		else if (chkUseSocket.Checked)
		{
			ttsHandler.PlaySocket(text);
		}
		else
		{
			ttsHandler.PlaySingle(text);
		}
	}
	private void BtnSelectBinary_Click(object sender, EventArgs e)
	{
		opnTTS.ShowDialog();
	}
    private void OpnTTS_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
    {
        txtTTSBinaryPath.Text = opnTTS.FileName;
        ttsHandler.Command = opnTTS.FileName;
        if (chkUsePipe.Checked)
        {
            ttsHandler.Restart();
        }
    }
    private void ChkUseSocket_CheckChanged(object sender, EventArgs eventArgs)
    {
        if (chkUseSocket.Checked)
        {
            ttsHandler.Close();
            txtTTSBinaryPath.ReadOnly = false;
            txtArguments.ReadOnly = true;
            chkUsePipe.Checked = false;
            lblTTSBinary.Text = "Socket Mode: TTSServer Address (IP Only)";
        } else {
            if(!chkUsePipe.Checked) {
                txtArguments.ReadOnly = false;
                txtTTSBinaryPath.ReadOnly = true;
            }
            lblTTSBinary.Text = "TTS Binary";
        }
    }
	private void ChkUsePipe_CheckedChanged(object sender, EventArgs e)
    {
        if (chkUsePipe.Checked)
        {
            ttsHandler.Restart();
            txtTTSBinaryPath.ReadOnly = true;
            txtArguments.ReadOnly = false;
            chkUseSocket.Checked = false;
            lblTTSBinary.Text = "Pipe mode: TTS Binary";
        } else {
            if(!chkUseSocket.Checked) {
                txtArguments.ReadOnly = false;
                txtTTSBinaryPath.ReadOnly = true;
            }
            ttsHandler.Close();
            lblTTSBinary.Text = "TTS Binary";
        }
    }
	private void TxtArguments_Leave(object sender, EventArgs e)
    {
        ttsHandler.CommandArguments = txtArguments.Text;
        if (chkUsePipe.Checked && !chkUseSocket.Checked)
        {
            ttsHandler.CommandArguments += " --stdin";
            ttsHandler.Restart();
        }
    }
    private void TxtBinaryPath_Leave(object sender, EventArgs e)
    {
        ttsHandler.Command = txtTTSBinaryPath.Text;
    }

	#endregion

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

	  //Linux TTS Stuff
		ttsHandler = new TTSHandler(lstLogs);
		ttsHandler.Command = txtTTSBinaryPath.Text;
		ttsHandler.CommandArguments = txtArguments.Text;
		//oldTTSMethod = ActGlobals.oFormActMain.PlayTtsMethod;
		lblStatus.Text = "Plugin Started";
		if (chkUsePipe.Checked) { ttsHandler.Open(); }
		ActGlobals.oFormActMain.PlayTtsMethod = new FormActMain.PlayTtsDelegate(PlayTTS);
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
		if (chkUsePipe.Checked)
		{
			if (!ttsHandler.Close())
			{
				Console.WriteLine(ttsHandler.LastException.ToString());
				Console.WriteLine("Exception trying to close TTS Process:" + Environment.NewLine + Environment.NewLine + ttsHandler.LastException.ToString());
			}
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
        DiscordClient.Speak(text, "", 0, 0);
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
      DiscordClient.InIt(txtToken.Text);
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
      xmlSettings.AddControlSetting(chkAutoConnect.Name, chkAutoConnect);
	  xmlSettings.AddControlSetting(txtArguments.Name, txtArguments);
	  xmlSettings.AddControlSetting(chkUsePipe.Name, chkUsePipe);
	  xmlSettings.AddControlSetting(txtTTSBinaryPath.Name, txtTTSBinaryPath);
	  xmlSettings.AddControlSetting(chkUseSocket.Name, chkUseSocket);
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
	//ttsHandler.CommandArguments = txtArguments.Text;
	//ttsHandler.Command = txtTTSBinaryPath.Text;
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