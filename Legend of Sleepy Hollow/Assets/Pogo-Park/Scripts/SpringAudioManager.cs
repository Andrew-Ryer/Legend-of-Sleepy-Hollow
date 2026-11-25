using System.Collections.Generic;
using UnityEngine;

public class SpringAudioManager : MonoBehaviour
{
    public static SpringAudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] AudioSource _musicSource;
    [SerializeField] AudioClip _backgroundMusic;
    [SerializeField, Range(0f, 100f)] float _musicVolume = 100f;

    [Header("Sound Effects")]
    [SerializeField, Range(0f, 100f)] float _effectsVolume = 100f;
    [SerializeField] AudioClip _springChargeLoop;
    [SerializeField] AudioClip _jump;
    [SerializeField] AudioClip _poweredJump;
    [SerializeField] AudioClip _boostReady;
    [SerializeField] AudioClip _land;
    [SerializeField] AudioClip _collision;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] float _spatialBlend;
    [SerializeField, Min(0f)] float _minimumCollisionSpeed = 1.5f;

    readonly Dictionary<ulong, AudioSource> _chargeLoops = new Dictionary<ulong, AudioSource>();
    readonly List<AudioSource> _oneShots = new List<AudioSource>();
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A SpringAudioManager already exists in this scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        if (_musicSource == null)
            _musicSource = GetComponent<AudioSource>();

        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();

        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.volume = _musicVolume / 100f;
    }

    void Start()
    {
        if (_backgroundMusic != null)
        {
            _musicSource.clip = _backgroundMusic;
            _musicSource.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_musicSource != null)
            _musicSource.volume = _musicVolume / 100f;

        foreach (AudioSource source in _chargeLoops.Values)
        {
            if (source != null)
                source.volume = _effectsVolume / 100f;
        }

        for (int i = _oneShots.Count - 1; i >= 0; i--)
        {
            if (_oneShots[i] == null)
                _oneShots.RemoveAt(i);
            else
                _oneShots[i].volume = _effectsVolume / 100f;
        }
    }

    public void SetChargeLoop(ulong playerId, Transform player, bool play)
    {
        if (!play)
        {
            if (_chargeLoops.TryGetValue(playerId, out AudioSource existing))
            {
                if (existing != null)
                    Destroy(existing.gameObject);
                _chargeLoops.Remove(playerId);
            }
            return;
        }

        if (_springChargeLoop == null || player == null || _chargeLoops.ContainsKey(playerId))
            return;

        AudioSource source = CreateSource("Spring Charge Loop", player, _springChargeLoop, true);
        _chargeLoops[playerId] = source;
        source.Play();
    }

    public void StopChargeLoop(ulong playerId)
    {
        SetChargeLoop(playerId, null, false);
    }

    public void PlayJump(Vector3 position, bool powered)
    {
        PlayOneShot(powered ? _poweredJump : _jump, position, powered ? "Powered Jump" : "Jump");
    }

    public void PlayBoostReady(Vector3 position)
    {
        PlayOneShot(_boostReady, position, "Boost Ready");
    }

    public void PlayLand(Vector3 position)
    {
        PlayOneShot(_land, position, "Land");
    }

    public void PlayCollision(Vector3 position)
    {
        PlayOneShot(_collision, position, "Collision");
    }

    public bool IsCollisionLoudEnough(float relativeSpeed)
    {
        return relativeSpeed >= _minimumCollisionSpeed;
    }

    AudioSource CreateSource(string objectName, Transform parent, AudioClip clip, bool loop)
    {
        GameObject soundObject = new GameObject(objectName);
        if (parent != null)
        {
            soundObject.transform.SetParent(parent, false);
            soundObject.transform.localPosition = Vector3.zero;
        }

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.clip = clip;
        source.volume = _effectsVolume / 100f;
        source.spatialBlend = _spatialBlend;
        source.dopplerLevel = 0f;
        return source;
    }

    void PlayOneShot(AudioClip clip, Vector3 position, string objectName)
    {
        if (clip == null)
            return;

        AudioSource source = CreateSource(objectName, null, clip, false);
        source.transform.position = position;
        _oneShots.Add(source);
        source.Play();
        Destroy(source.gameObject, clip.length + 0.1f);
    }
}
