using System;
using System.Drawing;
using System.Windows.Forms;
using WebSocketSharp;
using Newtonsoft.Json;
using Captura.Models;
using Captura.Video;

namespace Captura.Models
{
    public class TranslationOverlay : TextOverlay
    {
        private string _currentTranslation = "";
        private readonly WebSocket _ws;
        private bool _disposed;

        public TranslationOverlay(TextOverlaySettings Settings) : base(Settings)
        {
            _ws = new WebSocket("ws://localhost:8765");

            _ws.OnMessage += (s, e) =>
            {
                try
                {
                    var update = JsonConvert.DeserializeObject<dynamic>(e.Data);
                    _currentTranslation = update.translation?.ToString() ?? "";
                }
                catch (Exception)
                {
                    _currentTranslation = "";
                }
            };

            _ws.OnError += (s, e) => _currentTranslation = "";

            try
            {
                _ws.Connect();
            }
            catch (Exception)
            {
                // Ignore connection errors
            }
        }

        protected override string GetText() => _currentTranslation;

        public override void Dispose()
        {
            if (!_disposed)
            {
                _ws?.Close();
                base.Dispose();
                _disposed = true;
            }
        }
    }
}
