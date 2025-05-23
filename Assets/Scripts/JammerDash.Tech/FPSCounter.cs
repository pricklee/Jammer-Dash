using System;
using UnityEngine;
using UnityEngine.UI;

namespace JammerDash.Tech
{
    public class FPSCounter : MonoBehaviour
    {
        public Text Text;
        public GameObject panel;
        private int[] _frameRateSamples;
        private int _cacheNumbersAmount = 300;
        private int _averageFromAmount = 60;
        private int _averageCounter = 0;
        private int _currentAveraged;
        private Color _currentColor;
        private Color _targetColor;
        private float _smoothTime = 0.20f;
        float ms;
        SettingsData data;
        void Awake()
        {
            data = SettingsFileHandler.LoadSettingsFromFile();
            if (data.isShowingFPS)
            {
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
            }
            DontDestroyOnLoad(gameObject);
            _frameRateSamples = new int[_averageFromAmount];
            _currentColor = GetColorForFPS(0);
            _targetColor = _currentColor;
            InvokeRepeating(nameof(GetData), 0, 2);
        }
        void GetData() {
            data = SettingsFileHandler.LoadSettingsFromFile();
        }
        void Update()
        {
            // Sample FPS
            {
                var currentFrame = (int)Math.Round(1f / Time.unscaledDeltaTime);
                _frameRateSamples[_averageCounter] = currentFrame;
            }

            // Average FPS
            {
                var average = 0f;

                foreach (var frameRate in _frameRateSamples)
                {
                    average += frameRate;
                }

                _currentAveraged = (int)Math.Round(average / _averageFromAmount);
                _averageCounter = (_averageCounter + 1) % _averageFromAmount;
            }

            // Update color smoothly
            _targetColor = GetColorForFPS(_currentAveraged);
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime / _smoothTime);

            // Calculate drawing time in milliseconds
            float drawingTimeMs = 1000f / Mathf.Max(_currentAveraged, 0.00001f); // Avoid division by zero
            ms = drawingTimeMs;
            
            // Assign to UI
            {
                
                    Text.text = $"FPS: <color=#{ColorUtility.ToHtmlStringRGBA(_currentColor)}>{_currentAveraged}</color>\n{ms:F2} ms";
                

            }
        }

        void FixedUpdate()
        {
           
                if (data.isShowingFPS)
                {
                    panel.SetActive(true);
                }
                else
                {
                    panel.SetActive(false);
                }
        }


        Color GetColorForFPS(int fps)
        {
            if (fps < 50)
                return Color.red;
            else if (fps < 120)
                return Color.yellow;
            else
                return Color.green;
        }
    }
}