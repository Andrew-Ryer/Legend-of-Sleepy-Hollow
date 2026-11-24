using UnityEngine;

// Script for the glowing/pulsing behaviour that appears on the player
[RequireComponent(typeof(Renderer))]
public class SpringPlayerGlow : MonoBehaviour
{
    static readonly int OutlineSizeId = Shader.PropertyToID("_Outline_Size");
    static readonly int GlowAlphaId = Shader.PropertyToID("_Glow_Alpha");

    [SerializeField, Min(0f)] float _maximumOutlineSize = 0.1f;
    [SerializeField, Min(0.01f)] float _secondsToMaxGlow = 0.5f;
    [SerializeField, Min(0.01f)] float _fadeSeconds = 0.5f;
    [SerializeField, Range(0f, 1f)] float _maximumOpacity = 0.7f;

    Renderer _outlineRenderer;
    MaterialPropertyBlock _propertyBlock;
    SpringPlayerMovementInput _movement;
    float _elapsedTime;
    float _glowAlpha;
    bool _wasBoostReady;

    void Awake()
    {
        _outlineRenderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _movement = GetComponentInParent<SpringPlayerMovementInput>();
    }

    void OnEnable()
    {
        _elapsedTime = 0f;
        _glowAlpha = 0f;
        _wasBoostReady = false;
        ApplyGlow(0f, 0f);
    }

    void Update()
    {
        bool boostReady = _movement != null && _movement.HasNextBounceBoost;
        if (boostReady && !_wasBoostReady)
            _elapsedTime = 0f;
        _wasBoostReady = boostReady;

        float deltaTime = Time.deltaTime;
        if (boostReady)
            _elapsedTime += deltaTime;

        float pulse = boostReady ? 0.5f - 0.5f * Mathf.Cos(Mathf.PI * _elapsedTime / _secondsToMaxGlow) : 0f;
        float targetAlpha = boostReady ? _maximumOpacity : 0f;
        _glowAlpha = Mathf.MoveTowards(_glowAlpha, targetAlpha, deltaTime / Mathf.Max(0.01f, _fadeSeconds));

        ApplyGlow(_maximumOutlineSize * Mathf.Clamp01(pulse), _glowAlpha);
    }

    void ApplyGlow(float outlineSize, float alpha)
    {
        if (_outlineRenderer == null)
            return;

        _outlineRenderer.enabled = alpha > 0.001f;
        _outlineRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(OutlineSizeId, outlineSize);
        _propertyBlock.SetFloat(GlowAlphaId, Mathf.Clamp01(alpha));
        _outlineRenderer.SetPropertyBlock(_propertyBlock);
    }
}
