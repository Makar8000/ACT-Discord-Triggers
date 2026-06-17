using System;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Xml;
using System.Speech.Synthesis;
using System.Reflection;
using DiscordAPI;
using System.Threading.Tasks;
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
      // --- Settings controls (carried over; reparented into the new layout) ---
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
      this.chkRandomFx = new System.Windows.Forms.CheckBox();
      this.lblFxChance = new System.Windows.Forms.Label();
      this.sliderFxChance = new System.Windows.Forms.TrackBar();
      this.chkNormalize = new System.Windows.Forms.CheckBox();
      this.lblNormalizeTarget = new System.Windows.Forms.Label();
      this.sliderNormalizeTarget = new System.Windows.Forms.TrackBar();
      // --- New layout containers ---
      this.lstNav = new System.Windows.Forms.ListBox();
      this.pnlContent = new System.Windows.Forms.Panel();
      this.pagGeneral = new System.Windows.Forms.Panel();
      this.pagSound = new System.Windows.Forms.Panel();
      this.pagInfo = new System.Windows.Forms.Panel();
      this.pnlLog = new System.Windows.Forms.Panel();
      this.grpConnection = new System.Windows.Forms.GroupBox();
      this.grpChannel = new System.Windows.Forms.GroupBox();
      this.grpTTS = new System.Windows.Forms.GroupBox();
      this.grpFx = new System.Windows.Forms.GroupBox();
      this.rtfInfo = new System.Windows.Forms.RichTextBox();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderFxChance)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderNormalizeTarget)).BeginInit();
      this.pnlContent.SuspendLayout();
      this.pagGeneral.SuspendLayout();
      this.pagSound.SuspendLayout();
      this.pagInfo.SuspendLayout();
      this.pnlLog.SuspendLayout();
      this.grpConnection.SuspendLayout();
      this.grpChannel.SuspendLayout();
      this.grpTTS.SuspendLayout();
      this.grpFx.SuspendLayout();
      this.SuspendLayout();
      //
      // lblBotTok
      //
      this.lblBotTok.AutoSize = true;
      this.lblBotTok.Location = new System.Drawing.Point(15, 28);
      this.lblBotTok.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblBotTok.Name = "lblBotTok";
      this.lblBotTok.Size = new System.Drawing.Size(140, 20);
      this.lblBotTok.TabIndex = 0;
      this.lblBotTok.Text = "Discord Bot Token";
      //
      // txtToken
      //
      this.txtToken.Location = new System.Drawing.Point(18, 50);
      this.txtToken.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.txtToken.Name = "txtToken";
      this.txtToken.Size = new System.Drawing.Size(320, 26);
      this.txtToken.TabIndex = 1;
      this.txtToken.UseSystemPasswordChar = true;
      //
      // discordConnectbtn
      //
      this.discordConnectbtn.Location = new System.Drawing.Point(18, 90);
      this.discordConnectbtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.discordConnectbtn.Name = "discordConnectbtn";
      this.discordConnectbtn.Size = new System.Drawing.Size(140, 35);
      this.discordConnectbtn.TabIndex = 2;
      this.discordConnectbtn.Text = "Connect";
      this.discordConnectbtn.UseVisualStyleBackColor = true;
      this.discordConnectbtn.Click += new System.EventHandler(this.discordConnectbtn_Click);
      //
      // chkAutoConnect
      //
      this.chkAutoConnect.AutoSize = true;
      this.chkAutoConnect.Location = new System.Drawing.Point(170, 96);
      this.chkAutoConnect.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.chkAutoConnect.Name = "chkAutoConnect";
      this.chkAutoConnect.Size = new System.Drawing.Size(133, 24);
      this.chkAutoConnect.TabIndex = 3;
      this.chkAutoConnect.Text = "Auto Connect";
      this.chkAutoConnect.UseVisualStyleBackColor = true;
      //
      // lblBotStatus
      //
      this.lblBotStatus.AutoSize = true;
      this.lblBotStatus.Location = new System.Drawing.Point(15, 140);
      this.lblBotStatus.Name = "lblBotStatus";
      this.lblBotStatus.Size = new System.Drawing.Size(85, 20);
      this.lblBotStatus.TabIndex = 4;
      this.lblBotStatus.Text = "Bot Status";
      //
      // txtBotStatus
      //
      this.txtBotStatus.Location = new System.Drawing.Point(18, 162);
      this.txtBotStatus.Name = "txtBotStatus";
      this.txtBotStatus.Size = new System.Drawing.Size(320, 26);
      this.txtBotStatus.TabIndex = 5;
      this.txtBotStatus.Text = "Playing with ACT Triggers";
      this.txtBotStatus.TextChanged += new System.EventHandler(this.txtBotStatus_TextChanged);
      //
      // grpConnection
      //
      this.grpConnection.Controls.Add(this.lblBotTok);
      this.grpConnection.Controls.Add(this.txtToken);
      this.grpConnection.Controls.Add(this.discordConnectbtn);
      this.grpConnection.Controls.Add(this.chkAutoConnect);
      this.grpConnection.Controls.Add(this.lblBotStatus);
      this.grpConnection.Controls.Add(this.txtBotStatus);
      this.grpConnection.Location = new System.Drawing.Point(10, 10);
      this.grpConnection.Name = "grpConnection";
      this.grpConnection.Size = new System.Drawing.Size(560, 205);
      this.grpConnection.TabIndex = 0;
      this.grpConnection.TabStop = false;
      this.grpConnection.Text = "Discord Connection";
      //
      // lblServer
      //
      this.lblServer.AutoSize = true;
      this.lblServer.Location = new System.Drawing.Point(15, 28);
      this.lblServer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblServer.Name = "lblServer";
      this.lblServer.Size = new System.Drawing.Size(55, 20);
      this.lblServer.TabIndex = 0;
      this.lblServer.Text = "Server";
      //
      // cmbServer
      //
      this.cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbServer.FormattingEnabled = true;
      this.cmbServer.Location = new System.Drawing.Point(18, 50);
      this.cmbServer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbServer.Name = "cmbServer";
      this.cmbServer.Size = new System.Drawing.Size(320, 28);
      this.cmbServer.TabIndex = 1;
      this.cmbServer.SelectedValueChanged += new System.EventHandler(this.cmbServer_SelectedIndexChanged);
      //
      // lblChan
      //
      this.lblChan.AutoSize = true;
      this.lblChan.Location = new System.Drawing.Point(15, 92);
      this.lblChan.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblChan.Name = "lblChan";
      this.lblChan.Size = new System.Drawing.Size(68, 20);
      this.lblChan.TabIndex = 2;
      this.lblChan.Text = "Channel";
      //
      // cmbChan
      //
      this.cmbChan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbChan.FormattingEnabled = true;
      this.cmbChan.Location = new System.Drawing.Point(18, 114);
      this.cmbChan.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbChan.Name = "cmbChan";
      this.cmbChan.Size = new System.Drawing.Size(320, 28);
      this.cmbChan.TabIndex = 3;
      //
      // btnJoin
      //
      this.btnJoin.Enabled = false;
      this.btnJoin.Location = new System.Drawing.Point(18, 152);
      this.btnJoin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.btnJoin.Name = "btnJoin";
      this.btnJoin.Size = new System.Drawing.Size(140, 35);
      this.btnJoin.TabIndex = 4;
      this.btnJoin.Text = "Join Channel";
      this.btnJoin.UseVisualStyleBackColor = true;
      this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
      //
      // btnLeave
      //
      this.btnLeave.Enabled = false;
      this.btnLeave.Location = new System.Drawing.Point(170, 152);
      this.btnLeave.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.btnLeave.Name = "btnLeave";
      this.btnLeave.Size = new System.Drawing.Size(141, 35);
      this.btnLeave.TabIndex = 5;
      this.btnLeave.Text = "Leave Channel";
      this.btnLeave.UseVisualStyleBackColor = true;
      this.btnLeave.Click += new System.EventHandler(this.btnLeave_Click);
      //
      // grpChannel
      //
      this.grpChannel.Controls.Add(this.lblServer);
      this.grpChannel.Controls.Add(this.cmbServer);
      this.grpChannel.Controls.Add(this.lblChan);
      this.grpChannel.Controls.Add(this.cmbChan);
      this.grpChannel.Controls.Add(this.btnJoin);
      this.grpChannel.Controls.Add(this.btnLeave);
      this.grpChannel.Location = new System.Drawing.Point(10, 225);
      this.grpChannel.Name = "grpChannel";
      this.grpChannel.Size = new System.Drawing.Size(560, 205);
      this.grpChannel.TabIndex = 1;
      this.grpChannel.TabStop = false;
      this.grpChannel.Text = "Voice Channel";
      //
      // pagGeneral
      //
      this.pagGeneral.AutoScroll = true;
      this.pagGeneral.Controls.Add(this.grpConnection);
      this.pagGeneral.Controls.Add(this.grpChannel);
      this.pagGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
      this.pagGeneral.Name = "pagGeneral";
      this.pagGeneral.TabIndex = 0;
      //
      // lblTTS
      //
      this.lblTTS.AutoSize = true;
      this.lblTTS.Location = new System.Drawing.Point(15, 28);
      this.lblTTS.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTS.Name = "lblTTS";
      this.lblTTS.Size = new System.Drawing.Size(82, 20);
      this.lblTTS.TabIndex = 0;
      this.lblTTS.Text = "TTS Voice";
      //
      // cmbTTS
      //
      this.cmbTTS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbTTS.FormattingEnabled = true;
      this.cmbTTS.Location = new System.Drawing.Point(18, 50);
      this.cmbTTS.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.cmbTTS.Name = "cmbTTS";
      this.cmbTTS.Size = new System.Drawing.Size(320, 28);
      this.cmbTTS.TabIndex = 1;
      //
      // lblTTSVol
      //
      this.lblTTSVol.AutoSize = true;
      this.lblTTSVol.Location = new System.Drawing.Point(15, 90);
      this.lblTTSVol.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTSVol.Name = "lblTTSVol";
      this.lblTTSVol.Size = new System.Drawing.Size(96, 20);
      this.lblTTSVol.TabIndex = 2;
      this.lblTTSVol.Text = "TTS Volume";
      //
      // sliderTTSVol
      //
      this.sliderTTSVol.AutoSize = false;
      this.sliderTTSVol.Location = new System.Drawing.Point(18, 112);
      this.sliderTTSVol.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.sliderTTSVol.Maximum = 20;
      this.sliderTTSVol.Name = "sliderTTSVol";
      this.sliderTTSVol.Size = new System.Drawing.Size(320, 35);
      this.sliderTTSVol.TabIndex = 3;
      this.sliderTTSVol.TickStyle = System.Windows.Forms.TickStyle.None;
      this.sliderTTSVol.Value = 10;
      //
      // lblTTSSpeed
      //
      this.lblTTSSpeed.AutoSize = true;
      this.lblTTSSpeed.Location = new System.Drawing.Point(15, 165);
      this.lblTTSSpeed.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblTTSSpeed.Name = "lblTTSSpeed";
      this.lblTTSSpeed.Size = new System.Drawing.Size(89, 20);
      this.lblTTSSpeed.TabIndex = 4;
      this.lblTTSSpeed.Text = "TTS Speed";
      //
      // sliderTTSSpeed
      //
      this.sliderTTSSpeed.AutoSize = false;
      this.sliderTTSSpeed.Location = new System.Drawing.Point(18, 187);
      this.sliderTTSSpeed.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.sliderTTSSpeed.Maximum = 20;
      this.sliderTTSSpeed.Name = "sliderTTSSpeed";
      this.sliderTTSSpeed.Size = new System.Drawing.Size(320, 35);
      this.sliderTTSSpeed.TabIndex = 5;
      this.sliderTTSSpeed.TickStyle = System.Windows.Forms.TickStyle.None;
      this.sliderTTSSpeed.Value = 10;
      //
      // grpTTS
      //
      this.grpTTS.Controls.Add(this.lblTTS);
      this.grpTTS.Controls.Add(this.cmbTTS);
      this.grpTTS.Controls.Add(this.lblTTSVol);
      this.grpTTS.Controls.Add(this.sliderTTSVol);
      this.grpTTS.Controls.Add(this.lblTTSSpeed);
      this.grpTTS.Controls.Add(this.sliderTTSSpeed);
      this.grpTTS.Location = new System.Drawing.Point(10, 10);
      this.grpTTS.Name = "grpTTS";
      this.grpTTS.Size = new System.Drawing.Size(560, 240);
      this.grpTTS.TabIndex = 0;
      this.grpTTS.TabStop = false;
      this.grpTTS.Text = "Text-to-Speech";
      //
      // chkRandomFx
      //
      this.chkRandomFx.AutoSize = true;
      this.chkRandomFx.Location = new System.Drawing.Point(18, 25);
      this.chkRandomFx.Name = "chkRandomFx";
      this.chkRandomFx.Size = new System.Drawing.Size(160, 24);
      this.chkRandomFx.TabIndex = 0;
      this.chkRandomFx.Text = "Random Sound FX";
      this.chkRandomFx.UseVisualStyleBackColor = true;
      this.chkRandomFx.CheckedChanged += new System.EventHandler(this.fxSettings_Changed);
      //
      // lblFxChance
      //
      this.lblFxChance.AutoSize = true;
      this.lblFxChance.Location = new System.Drawing.Point(15, 58);
      this.lblFxChance.Name = "lblFxChance";
      this.lblFxChance.Size = new System.Drawing.Size(85, 20);
      this.lblFxChance.TabIndex = 1;
      this.lblFxChance.Text = "FX Chance";
      //
      // sliderFxChance
      //
      this.sliderFxChance.AutoSize = false;
      this.sliderFxChance.Location = new System.Drawing.Point(18, 80);
      this.sliderFxChance.Maximum = 100;
      this.sliderFxChance.Name = "sliderFxChance";
      this.sliderFxChance.Size = new System.Drawing.Size(320, 35);
      this.sliderFxChance.TabIndex = 2;
      this.sliderFxChance.TickStyle = System.Windows.Forms.TickStyle.None;
      this.sliderFxChance.Value = 25;
      this.sliderFxChance.Scroll += new System.EventHandler(this.fxSettings_Changed);
      //
      // chkNormalize
      //
      this.chkNormalize.AutoSize = true;
      this.chkNormalize.Checked = true;
      this.chkNormalize.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkNormalize.Location = new System.Drawing.Point(18, 133);
      this.chkNormalize.Name = "chkNormalize";
      this.chkNormalize.Size = new System.Drawing.Size(180, 24);
      this.chkNormalize.TabIndex = 3;
      this.chkNormalize.Text = "Auto-level Volume";
      this.chkNormalize.UseVisualStyleBackColor = true;
      this.chkNormalize.CheckedChanged += new System.EventHandler(this.normalizeSettings_Changed);
      //
      // lblNormalizeTarget
      //
      this.lblNormalizeTarget.AutoSize = true;
      this.lblNormalizeTarget.Location = new System.Drawing.Point(15, 166);
      this.lblNormalizeTarget.Name = "lblNormalizeTarget";
      this.lblNormalizeTarget.Size = new System.Drawing.Size(120, 20);
      this.lblNormalizeTarget.TabIndex = 4;
      this.lblNormalizeTarget.Text = "Auto-level Target";
      //
      // sliderNormalizeTarget
      //
      this.sliderNormalizeTarget.AutoSize = false;
      this.sliderNormalizeTarget.Location = new System.Drawing.Point(18, 188);
      this.sliderNormalizeTarget.Minimum = 12;
      this.sliderNormalizeTarget.Maximum = 30;
      this.sliderNormalizeTarget.Name = "sliderNormalizeTarget";
      this.sliderNormalizeTarget.Size = new System.Drawing.Size(320, 35);
      this.sliderNormalizeTarget.TabIndex = 5;
      this.sliderNormalizeTarget.TickStyle = System.Windows.Forms.TickStyle.None;
      this.sliderNormalizeTarget.Value = 20;
      this.sliderNormalizeTarget.Scroll += new System.EventHandler(this.normalizeSettings_Changed);
      //
      // grpFx
      //
      this.grpFx.Controls.Add(this.chkRandomFx);
      this.grpFx.Controls.Add(this.lblFxChance);
      this.grpFx.Controls.Add(this.sliderFxChance);
      this.grpFx.Controls.Add(this.chkNormalize);
      this.grpFx.Controls.Add(this.lblNormalizeTarget);
      this.grpFx.Controls.Add(this.sliderNormalizeTarget);
      this.grpFx.Location = new System.Drawing.Point(10, 260);
      this.grpFx.Name = "grpFx";
      this.grpFx.Size = new System.Drawing.Size(560, 240);
      this.grpFx.TabIndex = 1;
      this.grpFx.TabStop = false;
      this.grpFx.Text = "Effects && Leveling";
      //
      // pagSound
      //
      this.pagSound.AutoScroll = true;
      this.pagSound.Controls.Add(this.grpTTS);
      this.pagSound.Controls.Add(this.grpFx);
      this.pagSound.Dock = System.Windows.Forms.DockStyle.Fill;
      this.pagSound.Name = "pagSound";
      this.pagSound.TabIndex = 0;
      this.pagSound.Visible = false;
      //
      // rtfInfo
      //
      this.rtfInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.rtfInfo.Dock = System.Windows.Forms.DockStyle.Fill;
      this.rtfInfo.Font = new System.Drawing.Font("Consolas", 9.75F);
      this.rtfInfo.Name = "rtfInfo";
      this.rtfInfo.ReadOnly = true;
      this.rtfInfo.TabIndex = 0;
      this.rtfInfo.TabStop = false;
      //
      // pagInfo
      //
      this.pagInfo.AutoScroll = true;
      this.pagInfo.Controls.Add(this.rtfInfo);
      this.pagInfo.Dock = System.Windows.Forms.DockStyle.Fill;
      this.pagInfo.Name = "pagInfo";
      this.pagInfo.Padding = new System.Windows.Forms.Padding(10);
      this.pagInfo.TabIndex = 0;
      this.pagInfo.Visible = false;
      //
      // pnlContent
      //
      this.pnlContent.Controls.Add(this.pagGeneral);
      this.pnlContent.Controls.Add(this.pagSound);
      this.pnlContent.Controls.Add(this.pagInfo);
      this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
      this.pnlContent.Name = "pnlContent";
      this.pnlContent.TabIndex = 1;
      //
      // lstNav
      //
      this.lstNav.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.lstNav.Dock = System.Windows.Forms.DockStyle.Left;
      this.lstNav.Font = new System.Drawing.Font("Segoe UI", 11F);
      this.lstNav.FormattingEnabled = true;
      this.lstNav.IntegralHeight = false;
      this.lstNav.ItemHeight = 28;
      this.lstNav.Items.AddRange(new object[] { "General", "Sound", "Information" });
      this.lstNav.Name = "lstNav";
      this.lstNav.Size = new System.Drawing.Size(150, 520);
      this.lstNav.TabIndex = 0;
      this.lstNav.SelectedIndexChanged += new System.EventHandler(this.nav_SelectedIndexChanged);
      //
      // lblLog
      //
      this.lblLog.AutoSize = true;
      this.lblLog.Dock = System.Windows.Forms.DockStyle.Top;
      this.lblLog.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.lblLog.Name = "lblLog";
      this.lblLog.Padding = new System.Windows.Forms.Padding(4, 4, 0, 2);
      this.lblLog.Size = new System.Drawing.Size(88, 26);
      this.lblLog.TabIndex = 0;
      this.lblLog.Text = "Debug Log";
      //
      // logList
      //
      this.logList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.listColTim,
            this.listColMsg});
      this.logList.Dock = System.Windows.Forms.DockStyle.Fill;
      this.logList.FullRowSelect = true;
      this.logList.HideSelection = false;
      this.logList.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.logList.Name = "logList";
      this.logList.Size = new System.Drawing.Size(607, 184);
      this.logList.TabIndex = 1;
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
      // pnlLog
      //
      this.pnlLog.Controls.Add(this.logList);
      this.pnlLog.Controls.Add(this.lblLog);
      this.pnlLog.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.pnlLog.Name = "pnlLog";
      this.pnlLog.Size = new System.Drawing.Size(759, 210);
      this.pnlLog.TabIndex = 2;
      //
      // DiscordPlugin
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      // Docking is laid out in reverse z-order: add Fill first (laid out last,
      // fills the leftover), then the left nav, then the full-width bottom log.
      this.Controls.Add(this.pnlContent);
      this.Controls.Add(this.lstNav);
      this.Controls.Add(this.pnlLog);
      this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
      this.Name = "DiscordPlugin";
      this.Size = new System.Drawing.Size(759, 730);
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSSpeed)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderTTSVol)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderFxChance)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.sliderNormalizeTarget)).EndInit();
      this.grpConnection.ResumeLayout(false);
      this.grpConnection.PerformLayout();
      this.grpChannel.ResumeLayout(false);
      this.grpChannel.PerformLayout();
      this.grpTTS.ResumeLayout(false);
      this.grpTTS.PerformLayout();
      this.grpFx.ResumeLayout(false);
      this.grpFx.PerformLayout();
      this.pagGeneral.ResumeLayout(false);
      this.pagSound.ResumeLayout(false);
      this.pagInfo.ResumeLayout(false);
      this.pnlContent.ResumeLayout(false);
      this.pnlLog.ResumeLayout(false);
      this.pnlLog.PerformLayout();
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
    private CheckBox chkRandomFx;
    private Label lblFxChance;
    private TrackBar sliderFxChance;
    private CheckBox chkNormalize;
    private Label lblNormalizeTarget;
    private TrackBar sliderNormalizeTarget;
    private Label lblBotTok;
    // Layout containers for the categorized (nav + paged) settings UI.
    private ListBox lstNav;
    private Panel pnlContent;
    private Panel pagGeneral;
    private Panel pagSound;
    private Panel pagInfo;
    private Panel pnlLog;
    private GroupBox grpConnection;
    private GroupBox grpChannel;
    private GroupBox grpTTS;
    private GroupBox grpFx;
    private RichTextBox rtfInfo;
    private readonly DispatcherTimer statusDebounceTimer = new DispatcherTimer();
    #endregion

    public DiscordPlugin() {
      //Load UI Components and Assemblies
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      InitializeComponent();
      InitializeDebounceTimer();
      PopulateInfoPage();

      //Add installed voices to dropdown
      var tts = new SpeechSynthesizer();
      foreach (InstalledVoice v in tts.GetInstalledVoices())
        cmbTTS.Items.Add(v.VoiceInfo.Name);
      cmbTTS.SelectedIndex = 0;

      //Show the first page (also makes the nav selection visible).
      lstNav.SelectedIndex = 0;
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
        if (pluginName.Length > 0)
          configName = pluginName;
      } catch (Exception) { }

      //Load Settings file
      settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, $"Config\\{configName}.config.xml");
      xmlSettings = new SettingsSerializer(this);
      LoadSettings();
      ApplyFxSettings();
      ApplyNormalizationSettings();

      //Locate the out-of-process Discord bridge so DiscordClient knows where to spawn it
      string bridgeDir = FindBridgeDir();
      DiscordClient.SetBridgePath(bridgeDir);

      //Always-on diagnostics: capture both plugin- and bridge-side logs into one
      //unified file the user can simply email. Encapsulated — no UI, no toggle.
      try {
        DiagnosticsLog.Init(ActGlobals.oFormActMain.AppDataFolder.FullName, bridgeDir, PluginVersion());
        Log("Diagnostics log: " + DiagnosticsLog.UnifiedPath);
      } catch { }

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
      // Unhook static event subscriptions so the next init doesn't pile up
      // duplicate callbacks against a disposed UserControl.
      DiscordClient.BotReady -= BotReady;
      DiscordClient.Log -= Log;
      SaveSettings();
      try {
        await DiscordClient.DeinitAsync();
      } catch (Exception ex) {
        ActGlobals.oFormActMain.WriteExceptionLog(ex, "Error with DeInit of Discord Plugin.");
      }
      // Flush + regenerate the unified diagnostics file one last time so it reflects
      // this whole session before ACT tears the plugin down.
      try { DiagnosticsLog.Shutdown(); } catch { }
      lblStatus.Text = "Plugin Exited";
    }

    private string FindBridgeDir() {
      // The bridge ships as node.exe + bundle.js + node_modules/ next to the
      // plugin DLL. We return the directory; BridgeProcess derives the two
      // file paths from it.
      try {
        var plugin = ActGlobals.oFormActMain.PluginGetSelfData(this);
        if (plugin != null) {
          string dir = plugin.pluginFile.DirectoryName;
          if (File.Exists(Path.Combine(dir, "node.exe")) && File.Exists(Path.Combine(dir, "bundle.js"))) {
            return dir;
          }
        }
      } catch { }
      return Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Plugins\\Discord");
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
      // PipeClient delivers notifications on a thread-pool thread (see PipeClient.DispatchFrame).
      // Marshal back to the UI thread before touching controls.
      if (InvokeRequired) { BeginInvoke(new Action(BotReady)); return; }
      btnJoin.Enabled = true;
      _ = populateServers();
    }


    private async Task populateServers() {
      try {
        string[] servers = await DiscordClient.GetServersAsync();
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

    private async Task populateChannels(string server) {
      try {
        cmbChan.Items.Clear();
        string[] channels = await DiscordClient.GetChannelsAsync(server);
        cmbChan.Items.AddRange(channels);
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
    private void nav_SelectedIndexChanged(object sender, EventArgs e) {
      ShowPage(lstNav.SelectedIndex);
    }

    // Swap the visible content page. Guarded because the ListBox selection can
    // change before the page panels exist during construction.
    private void ShowPage(int index) {
      if (pagGeneral == null) return;
      pagGeneral.Visible = index == 0;
      pagSound.Visible = index == 1;
      pagInfo.Visible = index == 2;
    }

    private async void btnJoin_Click(object sender, EventArgs e) {
      btnJoin.Enabled = false;
      if (await DiscordClient.JoinChannel(cmbServer.SelectedItem.ToString(), cmbChan.SelectedItem.ToString())) {
        btnLeave.Enabled = true;
        ActGlobals.oFormActMain.PlayTtsMethod = speak;
        ActGlobals.oFormActMain.PlaySoundMethod = speakFile;
      } else {
        Log("Unable to join channel. Does your bot have permission to join this channel?");
        btnJoin.Enabled = true;
        await populateServers();
      }
    }

    private async void btnLeave_Click(object sender, EventArgs e) {
      btnLeave.Enabled = false;
      try {
        await DiscordClient.LeaveChannelAsync();
        btnJoin.Enabled = true;
        Log("Left channel.");
        ActGlobals.oFormActMain.PlayTtsMethod = oldTTS;
        ActGlobals.oFormActMain.PlaySoundMethod = oldSound;
      } catch (Exception ex) {
        Log("Error leaving channel. Possible connection issue.");
        btnLeave.Enabled = true;
        Log(ex.Message);
      }
    }

    private async void cmbServer_SelectedIndexChanged(object sender, EventArgs e) {
      // populateServers() does cmbServer.Items.Clear(), which fires this handler
      // with SelectedItem == null. Bail before we NRE.
      if (cmbServer.SelectedItem == null) return;
      try {
        await populateChannels(cmbServer.SelectedItem.ToString());
      } catch (Exception ex) {
        Log("populateChannels failed: " + ex.Message);
      }
    }

    private async void discordConnectbtn_Click(object sender, EventArgs e) {
      try {
        if (await DiscordClient.IsConnectedAsync()) {
          Log("Already connected to Discord.");
          return;
        }
        await DiscordClient.InitAsync(txtToken.Text, txtBotStatus.Text);
      } catch (Exception ex) {
        Log("Connect failed: " + ex.Message);
      }
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

    // Mirror the random-effects UI into DiscordClient. DiscordClient rolls the dice
    // per trigger on a background thread, so it reads these plain fields rather than
    // touching the controls cross-thread.
    private void fxSettings_Changed(object sender, EventArgs e) {
      ApplyFxSettings();
    }

    private void ApplyFxSettings() {
      DiscordClient.RandomEffectsEnabled = chkRandomFx.Checked;
      DiscordClient.RandomEffectChance = sliderFxChance.Value;
      lblFxChance.Text = "FX Chance: " + sliderFxChance.Value + "%";
    }

    // Mirror the auto-leveling UI into DiscordClient, then push it to the bridge.
    // Unlike FX (rolled per trigger), normalization is global bridge config, so a
    // change here sends a SetNormalization op (a no-op while disconnected — connect
    // re-pushes the current values). The slider holds the target magnitude in dB;
    // the bridge wants a negative dBFS value, hence the sign flip.
    private void normalizeSettings_Changed(object sender, EventArgs e) {
      ApplyNormalizationSettings();
      _ = DiscordClient.SetNormalizationAsync();
    }

    private void ApplyNormalizationSettings() {
      DiscordClient.NormalizeEnabled = chkNormalize.Checked;
      DiscordClient.NormalizeTargetDb = -sliderNormalizeTarget.Value;
      lblNormalizeTarget.Text = "Auto-level Target: -" + sliderNormalizeTarget.Value + " dBFS";
    }
    #endregion

    #region Information page
    // Static, read-only explanation of how a trigger turns into Discord audio.
    // Mirrors the bridge's actual pipeline order; when the compressor stage lands
    // it slots in between [1] and [2].
    private void PopulateInfoPage() {
      rtfInfo.Text =
@"How a trigger becomes Discord audio
───────────────────────────────────
Everything is 48 kHz · 16-bit · stereo PCM end to end.

  Trigger (TTS text  or  sound file)
        │
  [1] Source render ─ TTS synthesized in-process (System.Speech);
        │             files decoded + resampled with NAudio
        ▼
  [2] Random Sound FX (optional) ─ a random effect is rolled per
        │                          trigger when enabled
        ▼
  [3] Auto-level / normalization ─ RMS scaled toward your target
        │   dBFS, then peak-limited (no clipping) and boost-capped
        ▼
  [4] Mixer ─ concurrent triggers blended in 20 ms chunks
        │
        ▼
  node.exe bridge ─ encrypted Discord voice (DAVE E2EE)

───────────────────────────────────
Plugin version: " + PluginVersion() + @"
Diagnostics log: " + (DiagnosticsLog.UnifiedPath ?? "(not initialized)");
    }

    private static string PluginVersion() {
      try { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
      catch { return "?"; }
    }
    #endregion

    #region Settings
    public void Log(string text) {
      // Capture to the diagnostics file first, off the UI thread, so a busy/frozen
      // UI never delays or drops a log line (and so we never double-log across the
      // marshal hop below). UI display is a separate, best-effort concern.
      DiagnosticsLog.Append(text);
      UiLog(text);
    }

    private void UiLog(string text) {
      // Bridge log/disconnect/exit callbacks all funnel here from a thread-pool thread.
      if (InvokeRequired) { BeginInvoke(new Action<string>(UiLog), text); return; }
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
      xmlSettings.AddControlSetting(chkRandomFx.Name, chkRandomFx);
      xmlSettings.AddControlSetting(sliderFxChance.Name, sliderFxChance);
      xmlSettings.AddControlSetting(chkNormalize.Name, chkNormalize);
      xmlSettings.AddControlSetting(sliderNormalizeTarget.Name, sliderNormalizeTarget);
      if (File.Exists(settingsFile)) {
        try {
          using (var fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
          using (var xReader = new XmlTextReader(fs)) {
            while (xReader.Read())
              if (xReader.NodeType == XmlNodeType.Element)
                if (xReader.LocalName == "SettingsSerializer")
                  xmlSettings.ImportFromXml(xReader);
          }
        } catch (Exception ex) {
          lblStatus.Text = "Error loading settings: " + ex.Message;
        }
      }
    }

    public bool SaveSettings() {
      try {
        using (var fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        using (var xWriter = new XmlTextWriter(fs, Encoding.UTF8)) {
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
        }
      } catch (Exception ex) {
        if (lblStatus != null) lblStatus.Text = "Error saving settings: " + ex.Message;
        return false;
      }
      return true;
    }
    #endregion
  }
}
