using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.Rendering;

namespace UIEffects
{
    [AddComponentMenu("UI/Effects/Digital Glitch Volume Effect")]
    public class DigitalGlitchUIVolume : MonoBehaviour
    {
        [Range(0, 1)]
        public float intensity = 0;
        
        [Header("Textures")]
        [SerializeField] private Shader glitchShader;
        [SerializeField] private bool autoGenerateNoiseTexture = true;
        [SerializeField] private Texture2D noiseTexture;
        [SerializeField] private Texture2D trashTexture1;
        [SerializeField] private Texture2D trashTexture2;
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.05f;
        [SerializeField] private float noiseUpdateProbability = 0.5f;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool applyToAllChildUI = true;
        [SerializeField] private bool captureScreenForTrash = false;
        [SerializeField] private int trashTextureSize = 256;
        [SerializeField] private float displacementStrength = 1.0f;
        [SerializeField] private float colorShiftStrength = 1.0f;
        [SerializeField] private float glitchOpacity = 1.0f;
        [SerializeField] private bool alwaysVisible = false;
        
        [Header("Animation Settings")]
        [SerializeField] private float glitchInDuration = 0.3f; 
        [SerializeField] private float peakGlitchDuration = 0.5f;
        [SerializeField] private float glitchOutDuration = 0.7f; 
        [SerializeField] private AnimationCurve glitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Glitch Colors")]
        [SerializeField] private Color glitchColor = new Color(0f, 1f, 0.8f, 1f); 
        [SerializeField] private Color glitchColor2 = new Color(1f, 0f, 1f, 1f); 
        [SerializeField] private Color glitchColor3 = new Color(0.1f, 0.2f, 0.5f, 1f); 
        [SerializeField] private float colorBalance = 0.5f;
        
        // Cached properties
        private Material _material;
        private System.Random _random;
        private float _lastUpdateTime;
        private bool _useTrash1 = true;
        private int _frameCount = 0;
        private Camera _mainCamera;
        private RenderTexture _screenCapture;
        private CancellationTokenSource _effectCts;
        private bool _isEffectPlaying = false;
        private Image _myImage = null;
        
        // Shader property IDs
        private static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
        private static readonly int TrashTexID = Shader.PropertyToID("_TrashTex");
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int DisplacementID = Shader.PropertyToID("_DisplacementStrength");
        private static readonly int ColorShiftID = Shader.PropertyToID("_ColorShiftStrength");
        private static readonly int GlitchOpacityID = Shader.PropertyToID("_GlitchOpacity");
        private static readonly int GlitchColorID = Shader.PropertyToID("_GlitchColor");
        private static readonly int AlwaysVisibleID = Shader.PropertyToID("_AlwaysVisible");
        
        // Add these static property IDs after the existing ones:
        private static readonly int GlitchColor2ID = Shader.PropertyToID("_GlitchColor2");
        private static readonly int GlitchColor3ID = Shader.PropertyToID("_GlitchColor3");
        private static readonly int ColorBalanceID = Shader.PropertyToID("_ColorBalance");
        
        private void Awake()
        {
            _random = 
                new System.Random();
            _mainCamera = 
                Camera.main;
            _myImage = 
                this.gameObject.GetComponent<Image>();

            SetImageAlpha(0);
            
            if (glitchShader == null)
            {
                glitchShader = Shader.Find("UI/DigitalGlitchUI");
                if (glitchShader == null)
                {
                    Debug.LogError("DigitalGlitchUIExtended shader not found! Please add it to your project.");
                    enabled = false;
                    return;
                }
            }
            
            _material = new Material(glitchShader);
            
            if (autoGenerateNoiseTexture || noiseTexture == null)
            {
                noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
                GenerateNoiseTexture();
            }
            
            if (captureScreenForTrash)
            {
                _screenCapture = new RenderTexture(trashTextureSize, trashTextureSize, 0);
                _screenCapture.antiAliasing = 1;
            }
            
            InitializeTrashTextures();
            
            if (applyToAllChildUI)
            {
                ApplyMaterialToUI();
            }
            
            intensity = 0;
            
            
            UpdateMaterialProperties();
        }
        
        private void UpdateMaterialProperties()
        {
            if (_material == null) return;
            
            _material.SetTexture(NoiseTexID, noiseTexture);
            _material.SetTexture(TrashTexID, _useTrash1 ? trashTexture1 : trashTexture2);
            _material.SetFloat(IntensityID, intensity);
            
            if (_material.HasProperty(DisplacementID))
                _material.SetFloat(DisplacementID, displacementStrength);
            
            if (_material.HasProperty(ColorShiftID))
                _material.SetFloat(ColorShiftID, colorShiftStrength);
                
            if (_material.HasProperty(GlitchOpacityID))
                _material.SetFloat(GlitchOpacityID, glitchOpacity);
                
            if (_material.HasProperty(GlitchColorID))
                _material.SetColor(GlitchColorID, glitchColor);
                
            if (_material.HasProperty(AlwaysVisibleID))
                _material.SetFloat(AlwaysVisibleID, alwaysVisible ? 1.0f : 0.0f);
            
            if (_material.HasProperty(GlitchColor2ID))
                _material.SetColor(GlitchColor2ID, glitchColor2);

            if (_material.HasProperty(GlitchColor3ID))
                _material.SetColor(GlitchColor3ID, glitchColor3);

            if (_material.HasProperty(ColorBalanceID))
                _material.SetFloat(ColorBalanceID, colorBalance);

        }
        
        private void InitializeTrashTextures()
        {
            int texSize = captureScreenForTrash ? trashTextureSize : 128;
            
            if (trashTexture1 == null)
            {
                trashTexture1 = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                GenerateTrashTexture(trashTexture1);
            }
            
            if (trashTexture2 == null)
            {
                trashTexture2 = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                GenerateTrashTexture(trashTexture2);
            }
        }
        
        private void ApplyMaterialToUI()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                img.material = _material;
            }
            
            RawImage[] rawImages = GetComponentsInChildren<RawImage>(true);
            foreach (RawImage rawImg in rawImages)
            {
                rawImg.material = _material;
            }
            
            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text txt in texts)
            {
                txt.material = _material;
            }
            
            var tmp = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmp != null)
            {
                var tmps = GetComponentsInChildren(tmp, true);
                foreach (var t in tmps)
                {
                    var method = tmp.GetMethod("set_fontMaterial");
                    if (method != null)
                    {
                        method.Invoke(t, new object[] { _material });
                    }
                }
            }
        }
        
        private void Update()
        {
            if (!_isEffectPlaying)
                return;
            
            _material.SetFloat(IntensityID, intensity);
            
            if (intensity <= 0)
                return;
            
            _frameCount++;
            
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                _lastUpdateTime = Time.time;
                
                float updateChance = Mathf.Lerp(0.1f, noiseUpdateProbability, intensity);
                if ((float)_random.NextDouble() < updateChance)
                {
                    GenerateNoiseTexture();
                }
                
                _useTrash1 = !_useTrash1;
                _material.SetTexture(TrashTexID, _useTrash1 ? trashTexture1 : trashTexture2);
            }
            
            if (_frameCount % 13 == 0)
            {
                UpdateTrashTexture(trashTexture1);
            }
            
            if (_frameCount % 73 == 0)
            {
                UpdateTrashTexture(trashTexture2);
            }
        }
        
        private void GenerateNoiseTexture()
        {
            var color = RandomColor;
            
            for (var y = 0; y < noiseTexture.height; y++)
            {
                for (var x = 0; x < noiseTexture.width; x++)
                {
                    if (_random.NextDouble() > 0.89f)
                    {
                        color = RandomColor;
                    }
                    
                    noiseTexture.SetPixel(x, y, color);
                }
            }
            
            noiseTexture.Apply();
            _material.SetTexture(NoiseTexID, noiseTexture);
        }
        
        private bool captureScheduled = false;

        private void UpdateTrashTexture(Texture2D texture)
        {
            if (captureScreenForTrash && _mainCamera != null)
            {
                if (!captureScheduled)
                {
                    captureScheduled = true;
                    CaptureScreenAsync(texture).Forget();
                }
                else
                {
                    GenerateTrashTexture(texture);
                }
            }
            else
            {
                GenerateTrashTexture(texture);
            }
        }

        private async UniTask CaptureScreenAsync(
            Texture2D in_texture)
        {
            await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);
    
            if (_mainCamera == null || _screenCapture == null)
            {
                GenerateTrashTexture(in_texture);
                return;
            }
            
            var currentRT = _mainCamera.targetTexture;
    
            
            _mainCamera.targetTexture = _screenCapture;
            _mainCamera.Render();
            
            _mainCamera.targetTexture = currentRT;
            
            RenderTexture.active = _screenCapture;
            in_texture.ReadPixels(new Rect(0, 0, _screenCapture.width, _screenCapture.height), 0, 0);
            in_texture.Apply();
            RenderTexture.active = null;
            
            CorruptTexture(in_texture);
        }
        
        private void CorruptTexture(
            Texture2D in_texture)
        {
            int corruptionLines = Mathf.FloorToInt(intensity * 10);
            
            for (int i = 0; i < corruptionLines; i++)
            {
                int y = Mathf.FloorToInt((float)_random.NextDouble() * in_texture.height);
                Color lineColor = RandomColor;
                
                for (int x = 0; x < in_texture.width; x++)
                {
                    in_texture.SetPixel(x, y, lineColor);
                }
                
                if (_random.NextDouble() > 0.7f)
                {
                    int x = Mathf.FloorToInt((float)_random.NextDouble() * in_texture.width);
                    lineColor = RandomColor;
                    
                    for (int j = 0; j < in_texture.height; j++)
                    {
                        in_texture.SetPixel(x, j, lineColor);
                    }
                }
            }
            
            in_texture.Apply();
        }
        
        private void GenerateTrashTexture(
            Texture2D in_texture)
        {
            for (int y = 0; y < in_texture.height; y++)
            {
                Color lineColor = RandomColor;
                
                for (int x = 0; x < in_texture.width; x++)
                {
                    if (_random.NextDouble() > 0.97f)
                    {
                        lineColor = RandomColor;
                    }
                    
                    if (_random.NextDouble() > 0.995f)
                    {
                        for (int i = 0; i < in_texture.height; i++)
                        {
                            in_texture.SetPixel(x, i, RandomColor);
                        }
                    }
                    
                    in_texture.SetPixel(x, y, lineColor);
                }
            }
            
            in_texture.Apply();
        }
        
        private Color RandomColor
        {
            get
            {
                var r = (float)_random.NextDouble();
                var g = (float)_random.NextDouble();
                var b = (float)_random.NextDouble();
                var a = (float)_random.NextDouble();
                return new Color(r, g, b, a);
            }
        }

        private void SetImageAlpha(
            float in_alpha)
        {
            if (_myImage != null)
            {
                Color newColor = Color.white;

                newColor.a = in_alpha;

                _myImage.color = newColor;
            }
        }

        //  ----------------------------------------------
        //  PUBLIC 
        //  ----------------------------------------------
        
        /// <summary>
        /// Play the glitch effect animation with default duration
        /// </summary>
        public void PlayEffect()
        {
            SetImageAlpha(1);
            
            PlayEffect(
                glitchInDuration + peakGlitchDuration + glitchOutDuration)
                .Forget();
        }
        
        /// <summary>
        /// Play the glitch effect animation with custom total duration
        /// </summary>
        /// <param name="in_duration">Total duration of the effect in seconds</param>
        public async UniTask PlayEffect(
            float in_duration)
        {
            if (_isEffectPlaying)
            {
                CancelEffect();
            }

            SetImageAlpha(1);
            
            _effectCts = new CancellationTokenSource();
            
            _isEffectPlaying = true;
            
            float totalDefaultDuration = glitchInDuration + peakGlitchDuration + glitchOutDuration;
            float durationScale = in_duration / totalDefaultDuration;
            
            float scaledInDuration = glitchInDuration * durationScale;
            float scaledPeakDuration = peakGlitchDuration * durationScale;
            float scaledOutDuration = glitchOutDuration * durationScale;
            

            await PlayEffectAsync(scaledInDuration, scaledPeakDuration, scaledOutDuration, _effectCts.Token);

            _isEffectPlaying = false;
            _effectCts = null;

            SetImageAlpha(0);
        }
        
        /// <summary>
        /// Cancel any currently playing effect
        /// </summary>
        public void CancelEffect()
        {
            if (_effectCts != null && !_effectCts.IsCancellationRequested)
            {
                _effectCts.Cancel();
                _effectCts.Dispose();
                _effectCts = null;
            }
            
            ResetEffect();
        }
        
        private void ResetEffect()
        {
            intensity = 0;
            SetImageAlpha(0);
            displacementStrength = 1.0f;
            colorShiftStrength = 1.0f;
            _isEffectPlaying = false;
            UpdateMaterialProperties();
        }
        
        private async UniTask PlayEffectAsync(
            float in_inDuration, 
            float in_peakDuration, 
            float in_outDuration, 
            CancellationToken in_cancellationToken
            )
        {
            intensity = 0;
            
            SetAlwaysVisible(false);
            
            float[] phaseIntensities = { 0.0f, 0.1f, 0.25f, 0.5f, 0.8f, 1.0f, 0.8f, 0.5f, 0.25f, 0.1f, 0.0f };
            int phaseCount = phaseIntensities.Length;
            
            float inStep = in_inDuration / 3f;
            float peakStep = in_peakDuration / 2f; 
            float outStep = in_outDuration / 4f; 
            
            for (int phase = 0; phase < 3; phase++)
            {
                float startTime = Time.time;
                float startIntensity = phaseIntensities[phase];
                float endIntensity = phaseIntensities[phase + 1];
                float elapsedTime = 0f;
                
                while (elapsedTime < inStep)
                {
                    in_cancellationToken.ThrowIfCancellationRequested();
                    
                    elapsedTime = Time.time - startTime;
                    float t = Mathf.Clamp01(elapsedTime / inStep);
                    t = glitchCurve.Evaluate(t);
                    
                    intensity = Mathf.Lerp(startIntensity, endIntensity, t);
                    
                    displacementStrength = Mathf.Lerp(1.0f, 1.5f, t * intensity);
                    colorShiftStrength = Mathf.Lerp(1.0f, 1.5f, t * intensity);
                    
                    if (UnityEngine.Random.value < 0.1f * intensity)
                    {
                        GenerateNoiseTexture();
                        if (UnityEngine.Random.value < 0.3f)
                        {
                            UpdateTrashTexture(_useTrash1 ? trashTexture1 : trashTexture2);
                        }
                    }
                    
                    UpdateMaterialProperties();
                    
                    await UniTask.Yield(in_cancellationToken);
                }
                
                intensity = endIntensity;
                UpdateMaterialProperties();
            }
            
            float peakStartTime = Time.time;
            float peakElapsedTime = 0f;
            
            while (peakElapsedTime < peakStep)
            {
                in_cancellationToken.ThrowIfCancellationRequested();
                
                peakElapsedTime = Time.time - peakStartTime;
                float t = Mathf.Clamp01(peakElapsedTime / peakStep);
                t = glitchCurve.Evaluate(t);
                
                intensity = Mathf.Lerp(phaseIntensities[3], phaseIntensities[4], t);
                
                displacementStrength = Mathf.Lerp(1.5f, 2.5f, t);
                colorShiftStrength = Mathf.Lerp(1.5f, 2.5f, t);
                
                if (UnityEngine.Random.value < 0.3f)
                {
                    GenerateNoiseTexture();
                    
                    _useTrash1 = !_useTrash1;
                    _material.SetTexture(TrashTexID, _useTrash1 ? trashTexture1 : trashTexture2);
                }
                
                UpdateMaterialProperties();
                
                await UniTask.Yield(in_cancellationToken);
            }
            
            float holdStartTime = Time.time;
            float holdElapsedTime = 0f;
            while (holdElapsedTime < peakStep)
            {
                in_cancellationToken.ThrowIfCancellationRequested();
                
                holdElapsedTime = Time.time - holdStartTime;
                
                displacementStrength = 2.5f + UnityEngine.Random.Range(-0.5f, 0.5f);
                colorShiftStrength = 2.5f + UnityEngine.Random.Range(-0.5f, 0.5f);

                if (UnityEngine.Random.value < 0.4f)
                {
                    GenerateNoiseTexture();
                    
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        _useTrash1 = !_useTrash1;
                        _material.SetTexture(TrashTexID, _useTrash1 ? trashTexture1 : trashTexture2);
                    }
                }
                
                UpdateMaterialProperties();
                
                await UniTask.Yield(in_cancellationToken);
            }
            for (int phase = 5; phase < phaseCount - 1; phase++)
            {
                in_cancellationToken.ThrowIfCancellationRequested();
                
                float startTime = Time.time;
                float startIntensity = phaseIntensities[phase];
                float endIntensity = phaseIntensities[phase + 1];
                float elapsedTime = 0f;
                
                while (elapsedTime < outStep)
                {
                    in_cancellationToken.ThrowIfCancellationRequested();
                    
                    elapsedTime = Time.time - startTime;
                    float t = Mathf.Clamp01(elapsedTime / outStep);
                    t = glitchCurve.Evaluate(t);
                    
                    intensity = Mathf.Lerp(startIntensity, endIntensity, t);

                    float phaseProgress = (float)(phase - 5) / 5f;  
                    displacementStrength = Mathf.Lerp(2.0f, 1.0f, phaseProgress + t * 0.2f);
                    colorShiftStrength = Mathf.Lerp(2.0f, 1.0f, phaseProgress + t * 0.2f);

                    if (UnityEngine.Random.value < 0.05f * intensity)
                    {
                        GenerateNoiseTexture();
                    }
                    
                    UpdateMaterialProperties();
                    
                    await UniTask.Yield(in_cancellationToken);
                }
                
                intensity = endIntensity;
                UpdateMaterialProperties();
            }
            
            intensity = 0;
            displacementStrength = 1.0f;
            colorShiftStrength = 1.0f;
            UpdateMaterialProperties();
            
            _isEffectPlaying = false;
        }
        
        /// <summary>
        /// Play the glitch effect animation with custom phase durations
        /// </summary>
        /// <param name="in_inDuration">Duration of the glitch-in phase</param>
        /// <param name="in_peakDuration">Duration of the peak glitch phase</param>
        /// <param name="in_outDuration">Duration of the glitch-out phase</param>
        public void PlayEffect(
            float in_inDuration, 
            float in_peakDuration, 
            float in_outDuration)
        {
            PlayEffectAsync(in_inDuration, in_peakDuration, in_outDuration, 
                _effectCts?.Token ?? new CancellationToken()).Forget();
        }
        
        /// <summary>
        /// Set the intensity of the glitch effect
        /// </summary>
        public void SetIntensity(
            float in_value)
        {
            intensity = Mathf.Clamp01(in_value);
            UpdateMaterialProperties();
        }
        
        /// <summary>
        /// Set the opacity of the glitch effect (separate from image's alpha)
        /// </summary>
        public void SetGlitchOpacity(
            float in_value)
        {
            glitchOpacity = Mathf.Clamp01(in_value);
            if (_material.HasProperty(GlitchOpacityID))
                _material.SetFloat(GlitchOpacityID, glitchOpacity);
        }
        
        /// <summary>
        /// Set the color of the glitch effect
        /// </summary>
        public void SetGlitchColor(
            Color in_color)
        {
            glitchColor = in_color;
            if (_material.HasProperty(GlitchColorID))
                _material.SetColor(GlitchColorID, glitchColor);
        }
        
        /// <summary>
        /// Set if the entire image should be visible or just glitched portions
        /// </summary>
        public void SetAlwaysVisible(
            bool in_visible)
        {
            alwaysVisible = in_visible;
            if (_material.HasProperty(AlwaysVisibleID))
                _material.SetFloat(AlwaysVisibleID, alwaysVisible ? 1.0f : 0.0f);
        }
        
        /// <summary>
        /// Set the secondary glitch color
        /// </summary>
        public void SetGlitchColor2(
            Color in_color)
        {
            glitchColor2 = in_color;
            if (_material.HasProperty(GlitchColor2ID))
                _material.SetColor(GlitchColor2ID, glitchColor2);
        }

        /// <summary>
        /// Set the tertiary glitch color
        /// </summary>
        public void SetGlitchColor3(
            Color in_color)
        {
            glitchColor3 = in_color;
            if (_material.HasProperty(GlitchColor3ID))
                _material.SetColor(GlitchColor3ID, glitchColor3);
        }

        /// <summary>
        /// Set the color balance between primary and secondary glitch colors
        /// </summary>
        public void SetColorBalance(
            float in_balance)
        {
            colorBalance = Mathf.Clamp01(in_balance);
            if (_material.HasProperty(ColorBalanceID))
                _material.SetFloat(ColorBalanceID, colorBalance);
        }
        
        /// <summary>
        /// Generate a new noise pattern immediately
        /// </summary>
        public void RefreshNoise()
        {
            GenerateNoiseTexture();
        }
        
        /// <summary>
        /// Apply the glitch material to an additional UI element
        /// </summary>
        public void ApplyToUIElement(
            Graphic in_uiElement)
        {
            if (in_uiElement != null && _material != null)
            {
                in_uiElement.material = _material;
            }
        }
        
        private void OnDestroy()
        {
            CancelEffect();

            if (autoGenerateNoiseTexture && noiseTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(noiseTexture);
                else
                    DestroyImmediate(noiseTexture);
            }
            
            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
            }
            
            if (_screenCapture != null)
            {
                if (Application.isPlaying)
                    Destroy(_screenCapture);
                else
                    DestroyImmediate(_screenCapture);
            }
        }
    }
}