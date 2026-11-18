using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace YMH219_SpeechApp
{
    public partial class Form1 : Form
    {
        private readonly TextToSpeechService _ttsService;
        private readonly SpeechToTextService _sttService;

        // Text-to-Speech (TTS) Kontrolleri
        private ComboBox _cmbVoices = null!;
        private TrackBar _trkRate = null!;
        private Label _lblRateVal = null!;
        private TrackBar _trkVolume = null!;
        private Label _lblVolumeVal = null!;
        private RichTextBox _rtbTtsInput = null!;
        private Button _btnSpeak = null!;
        private Button _btnPauseResume = null!;
        private Button _btnStopSpeak = null!;
        private Button _btnExportWav = null!;
        private Label _lblTtsStatus = null!;

        // Speech-to-Text (STT) Kontrolleri
        private Button _btnStartListen = null!;
        private Button _btnStopListen = null!;
        private Button _btnClearStt = null!;
        private Button _btnSaveSttTxt = null!;
        private Label _lblSttStatus = null!;
        private Label _lblHypothesized = null!;
        private RichTextBox _rtbSttOutput = null!;

        public Form1()
        {
            InitializeComponent();
            _ttsService = new TextToSpeechService();
            _sttService = new SpeechToTextService();

            SetupCustomUI();
            LoadInstalledVoices();
            BindEvents();
        }

        private void SetupCustomUI()
        {
            this.Text = "YMH 219 - Speech-to-Text & Text-to-Speech Uygulaması";
            this.Size = new Size(940, 680);
            this.MinimumSize = new Size(880, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // 1. ÜST PANEL: TEXT-TO-SPEECH (METİNDEN SESE)
            var grpTts = new GroupBox
            {
                Text = "  1. Metinden Sese Dönüştürücü (Text-to-Speech)  ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            var ttsTopBar = new Panel { Dock = DockStyle.Top, Height = 46 };

            var lblVoice = new Label { Text = "Ses Modeli:", Location = new Point(4, 12), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _cmbVoices = new ComboBox
            {
                Location = new Point(92, 9),
                Size = new Size(250, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            var lblRate = new Label { Text = "Hız:", Location = new Point(360, 12), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _trkRate = new TrackBar { Location = new Point(395, 6), Size = new Size(125, 35), Minimum = -10, Maximum = 10, Value = 0, TickFrequency = 2 };
            _lblRateVal = new Label { Text = "0", Location = new Point(522, 12), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblVol = new Label { Text = "Ses Düzeyi:", Location = new Point(560, 12), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _trkVolume = new TrackBar { Location = new Point(645, 6), Size = new Size(135, 35), Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 20 };
            _lblVolumeVal = new Label { Text = "%100", Location = new Point(782, 12), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            ttsTopBar.Controls.AddRange(new Control[] { lblVoice, _cmbVoices, lblRate, _trkRate, _lblRateVal, lblVol, _trkVolume, _lblVolumeVal });

            var ttsBottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48 };

            _btnSpeak = CreateButton("Seslendir", new Point(4, 8), new Size(130, 34), Color.FromArgb(22, 163, 74));
            _btnPauseResume = CreateButton("Duraklat / Devam", new Point(144, 8), new Size(165, 34), Color.FromArgb(234, 88, 12));
            _btnPauseResume.Enabled = false;
            _btnStopSpeak = CreateButton("Durdur", new Point(319, 8), new Size(110, 34), Color.FromArgb(220, 38, 38));
            _btnStopSpeak.Enabled = false;
            _btnExportWav = CreateButton("WAV Olarak Kaydet", new Point(439, 8), new Size(180, 34), Color.FromArgb(37, 99, 235));

            _lblTtsStatus = new Label
            {
                Text = "● Hazır",
                Location = new Point(635, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            ttsBottomBar.Controls.AddRange(new Control[] { _btnSpeak, _btnPauseResume, _btnStopSpeak, _btnExportWav, _lblTtsStatus });

            _rtbTtsInput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Merhaba, bu uygulama Fırat Üniversitesi Yazılım Mühendisliği Nesne Tabanlı Programlama dersi kapsamında geliştirilmiştir."
            };

            grpTts.Controls.Add(_rtbTtsInput);
            grpTts.Controls.Add(ttsBottomBar);
            grpTts.Controls.Add(ttsTopBar);

            // 2. ALT PANEL: SPEECH-TO-TEXT (SESTEN METNE)
            var grpStt = new GroupBox
            {
                Text = "  2. Sesten Metne Dönüştürücü (Speech-to-Text)  ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            var sttTopBar = new Panel { Dock = DockStyle.Top, Height = 48 };

            _btnStartListen = CreateButton("Dinlemeyi Başlat", new Point(4, 7), new Size(165, 34), Color.FromArgb(13, 148, 136));
            _btnStopListen = CreateButton("Dinlemeyi Durdur", new Point(179, 7), new Size(165, 34), Color.FromArgb(220, 38, 38));
            _btnStopListen.Enabled = false;
            _btnClearStt = CreateButton("Temizle", new Point(354, 7), new Size(105, 34), Color.FromArgb(100, 116, 139));
            _btnSaveSttTxt = CreateButton("Metni Kaydet (.txt)", new Point(469, 7), new Size(175, 34), Color.FromArgb(37, 99, 235));

            _lblSttStatus = new Label
            {
                Text = "● Mikrofon Kapalı",
                Location = new Point(658, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            sttTopBar.Controls.AddRange(new Control[] { _btnStartListen, _btnStopListen, _btnClearStt, _btnSaveSttTxt, _lblSttStatus });

            var sttHypoBar = new Panel { Dock = DockStyle.Bottom, Height = 28 };
            _lblHypothesized = new Label
            {
                Text = "Anlık Algılama: -",
                Location = new Point(4, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            sttHypoBar.Controls.Add(_lblHypothesized);

            _rtbSttOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Consolas", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };

            grpStt.Controls.Add(_rtbSttOutput);
            grpStt.Controls.Add(sttHypoBar);
            grpStt.Controls.Add(sttTopBar);

            mainLayout.Controls.Add(grpTts, 0, 0);
            mainLayout.Controls.Add(grpStt, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private Button CreateButton(string text, Point location, Size size, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void LoadInstalledVoices()
        {
            var voices = _ttsService.GetInstalledVoices();
            _cmbVoices.Items.Clear();
            foreach (var v in voices)
            {
                _cmbVoices.Items.Add(v);
            }
            if (_cmbVoices.Items.Count > 0)
            {
                _cmbVoices.SelectedIndex = 0;
            }
        }

        private void BindEvents()
        {
            // TTS Ayar Kontrolleri
            _trkRate.Scroll += (s, e) => _lblRateVal.Text = _trkRate.Value.ToString();
            _trkVolume.Scroll += (s, e) => _lblVolumeVal.Text = $"%{_trkVolume.Value}";

            _btnSpeak.Click += (s, e) =>
            {
                try
                {
                    if (_cmbVoices.SelectedItem != null)
                        _ttsService.SelectVoice(_cmbVoices.SelectedItem.ToString()!);

                    _ttsService.Configure(_trkRate.Value, _trkVolume.Value);
                    _ttsService.SpeakAsync(_rtbTtsInput.Text);

                    _btnSpeak.Enabled = false;
                    _btnPauseResume.Enabled = true;
                    _btnStopSpeak.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Seslendirme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            _btnPauseResume.Click += (s, e) => _ttsService.PauseOrResume();
            _btnStopSpeak.Click += (s, e) => _ttsService.Stop();

            _btnExportWav.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_rtbTtsInput.Text))
                {
                    MessageBox.Show("Kaydedilecek metin boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "WAV Ses Dosyası (*.wav)|*.wav",
                    FileName = $"SesKaydi_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
                    Title = "Ses Dosyasını Kaydet"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_cmbVoices.SelectedItem != null)
                            _ttsService.SelectVoice(_cmbVoices.SelectedItem.ToString()!);

                        _ttsService.Configure(_trkRate.Value, _trkVolume.Value);
                        _ttsService.ExportToWav(_rtbTtsInput.Text, sfd.FileName);
                        MessageBox.Show("Ses dosyası başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"WAV kaydedilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            _ttsService.OnStatusChanged += status =>
            {
                BeginInvoke(() =>
                {
                    _lblTtsStatus.Text = $"● {status}";
                    _lblTtsStatus.ForeColor = Color.FromArgb(22, 163, 74);
                });
            };

            _ttsService.OnSpeakCompleted += () =>
            {
                BeginInvoke(() =>
                {
                    _btnSpeak.Enabled = true;
                    _btnPauseResume.Enabled = false;
                    _btnStopSpeak.Enabled = false;
                });
            };

            // STT Kontrolleri
            _btnStartListen.Click += (s, e) =>
            {
                try
                {
                    _sttService.StartListening();
                    _btnStartListen.Enabled = false;
                    _btnStopListen.Enabled = true;
                    _lblSttStatus.ForeColor = Color.FromArgb(22, 163, 74);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Mikrofon / Tanıma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _btnStopListen.Click += (s, e) =>
            {
                _sttService.StopListening();
                _btnStartListen.Enabled = true;
                _btnStopListen.Enabled = false;
                _lblSttStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblHypothesized.Text = "Anlık Algılama: -";
            };

            _btnClearStt.Click += (s, e) =>
            {
                _rtbSttOutput.Clear();
                _lblHypothesized.Text = "Anlık Algılama: -";
            };

            _btnSaveSttTxt.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_rtbSttOutput.Text))
                {
                    MessageBox.Show("Kaydedilecek metin bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "Metin Dosyası (*.txt)|*.txt",
                    FileName = $"KonusmaMetni_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Konuşma Metnini Kaydet"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, _rtbSttOutput.Text, Encoding.UTF8);
                    MessageBox.Show("Metin başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _sttService.OnStatusChanged += status =>
            {
                BeginInvoke(() => _lblSttStatus.Text = $"● {status}");
            };

            _sttService.OnSpeechHypothesized += text =>
            {
                BeginInvoke(() => _lblHypothesized.Text = $"Anlık Algılama: {text}...");
            };

            _sttService.OnSpeechRecognized += text =>
            {
                BeginInvoke(() =>
                {
                    _lblHypothesized.Text = "Anlık Algılama: -";
                    _rtbSttOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
                    _rtbSttOutput.ScrollToCaret();
                });
            };

            this.FormClosing += (s, e) =>
            {
                _ttsService.Dispose();
                _sttService.Dispose();
            };
        }
    }
}