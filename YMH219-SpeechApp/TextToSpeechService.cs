using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;

namespace YMH219_SpeechApp
{
    public class TextToSpeechService : IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;

        public event Action<string>? OnStatusChanged;
        public event Action? OnSpeakCompleted;

        public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;
        public bool IsPaused => _synthesizer.State == SynthesizerState.Paused;

        public TextToSpeechService()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();

            _synthesizer.SpeakStarted += (s, e) => OnStatusChanged?.Invoke("Metin seslendiriliyor...");
            _synthesizer.SpeakCompleted += (s, e) =>
            {
                OnStatusChanged?.Invoke(e.Cancelled ? "Seslendirme durduruldu." : "Seslendirme tamamlandı.");
                OnSpeakCompleted?.Invoke();
            };
        }

        public List<string> GetInstalledVoices()
        {
            return _synthesizer.GetInstalledVoices()
                .Where(v => v.Enabled)
                .Select(v => v.VoiceInfo.Name)
                .ToList();
        }

        public void SelectVoice(string voiceName)
        {
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                _synthesizer.SelectVoice(voiceName);
            }
        }

        public void Configure(int rate, int volume)
        {
            _synthesizer.Rate = Math.Clamp(rate, -10, 10);
            _synthesizer.Volume = Math.Clamp(volume, 0, 100);
        }

        public void SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Seslendirilecek metin boş olamaz.");

            Stop();
            _synthesizer.SpeakAsync(text);
        }

        public void PauseOrResume()
        {
            if (_synthesizer.State == SynthesizerState.Speaking)
            {
                _synthesizer.Pause();
                OnStatusChanged?.Invoke("Seslendirme duraklatıldı.");
            }
            else if (_synthesizer.State == SynthesizerState.Paused)
            {
                _synthesizer.Resume();
                OnStatusChanged?.Invoke("Seslendirme devam ediyor...");
            }
        }

        public void Stop()
        {
            if (_synthesizer.State != SynthesizerState.Ready)
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
        }

        public void ExportToWav(string text, string filePath)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Kaydedilecek metin boş olamaz.");

            Stop();
            _synthesizer.SetOutputToWaveFile(filePath);
            _synthesizer.Speak(text);
            _synthesizer.SetOutputToDefaultAudioDevice();
            OnStatusChanged?.Invoke("Ses dosyası (.wav) olarak kaydedildi.");
        }

        public void Dispose()
        {
            Stop();
            _synthesizer.Dispose();
        }
    }
}