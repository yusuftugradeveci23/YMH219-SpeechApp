using System;
using System.IO;
using System.Text.Json;
using NAudio.Wave;
using Vosk;

namespace YMH219_SpeechApp
{
    public class SpeechToTextService : IDisposable
    {
        private Model? _model;
        private VoskRecognizer? _recognizer;
        private WaveInEvent? _waveIn;
        private bool _isListening;

        public event Action<string>? OnSpeechRecognized;
        public event Action<string>? OnSpeechHypothesized;
        public event Action<string>? OnStatusChanged;

        public bool IsListening => _isListening;

        public void StartListening(string? ignoredCulture = null)
        {
            if (_isListening) return;

            const string modelFolderName = "vosk-model-small-tr-0.3";
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelFolderName);
            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException(
                    $"Türkçe konuşma tanıma modeli bulunamadı:\n{modelPath}\n\nLütfen 'vosk-model-small-tr-0.3' klasörünü proje dizinine ekleyin.");
            }

            if (WaveInEvent.DeviceCount == 0)
            {
                throw new InvalidOperationException("Sistemde kullanılabilir bir mikrofon cihazı bulunamadı.");
            }

            Vosk.Vosk.SetLogLevel(-1);
            _model ??= new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };

            _waveIn.DataAvailable += (s, e) =>
            {
                if (!_isListening || _recognizer == null) return;

                if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    string text = ExtractText(_recognizer.Result(), "text");
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        OnSpeechRecognized?.Invoke(text);
                    }
                }
                else
                {
                    string partial = ExtractText(_recognizer.PartialResult(), "partial");
                    if (!string.IsNullOrWhiteSpace(partial))
                    {
                        OnSpeechHypothesized?.Invoke(partial);
                    }
                }
            };

            _waveIn.RecordingStopped += (s, e) =>
            {
                _isListening = false;
                OnStatusChanged?.Invoke("Dinleme durduruldu.");
            };

            _waveIn.StartRecording();
            _isListening = true;
            OnStatusChanged?.Invoke("Mikrofon dinleniyor (Türkçe - Vosk Offline AI)...");
        }

        public void StopListening()
        {
            if (!_isListening) return;

            _isListening = false;
            _waveIn?.StopRecording();

            if (_recognizer != null)
            {
                string finalText = ExtractText(_recognizer.FinalResult(), "text");
                if (!string.IsNullOrWhiteSpace(finalText))
                {
                    OnSpeechRecognized?.Invoke(finalText);
                }
            }

            OnStatusChanged?.Invoke("Dinleme durduruldu.");
        }

        private static string ExtractText(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(propertyName, out JsonElement element))
                {
                    return element.GetString()?.Trim() ?? string.Empty;
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        public void Dispose()
        {
            StopListening();
            _waveIn?.Dispose();
            _recognizer?.Dispose();
            _model?.Dispose();
        }
    }
}