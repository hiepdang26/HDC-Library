using System;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCPlayerPrefsStore : IKeyValueStore
    {
        public bool TryGetInt(string key, out int value)
        {
            try
            {
                value = PlayerPrefs.GetInt(key, 0);
                return true;
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
        }

        public void SetInt(string key, int value)
        {
            try
            {
                PlayerPrefs.SetInt(key, value);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[HDCAds] cannot save {key}: {exception.Message}");
            }
        }

        public void Save()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot save: " + exception.Message);
            }
        }
    }
}
