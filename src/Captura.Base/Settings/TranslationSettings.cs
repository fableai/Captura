using System;

namespace Captura.Video
{
    public class TranslationSettings : PropertyStore
    {
        public bool Enabled
        {
            get => Get(false);
            set => Set(value);
        }

        public string ApiKey
        {
            get => Get("");
            set => Set(value);
        }
    }
}
