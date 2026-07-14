using UnityEngine;
using Mirror;
using Steamworks;

[RequireComponent(typeof(AudioSource))]
public class ProximityChat : NetworkBehaviour
{
    [Header("Settings")]
    public bool hearYourself;
    [Range(0f, 2f)] public float volume = 1f;
    [Range(1f, 5f)] public float micGainBoost = 2.0f; 

    [Header("Proximity Range")]
    [SerializeField] private float minDistance = 5;
    [SerializeField] private float maxDistance = 15;
    
    private AudioSource _audioSrc;
    private bool _isRecording;

    private const float SendInterval = 0.035f; 
    private float _timer;
    
    private uint _cachedSampleRate;
    private AudioClip _voiceClip;
    
    private int _writePosition;
    private int _clipLengthSamples;
    
    private int _lastPlayPosition;

    private void Awake()
    {
        _audioSrc = GetComponent<AudioSource>();
        
        _audioSrc.spatialBlend = 1f; 
        _audioSrc.rolloffMode = AudioRolloffMode.Linear;
        _audioSrc.minDistance = minDistance;
        _audioSrc.maxDistance = maxDistance;
        _audioSrc.loop = true;
        _audioSrc.volume = 1f;
        _audioSrc.dopplerLevel = 0f;
        
        _cachedSampleRate = SteamUser.GetVoiceOptimalSampleRate();
        _clipLengthSamples = (int)_cachedSampleRate; 
    }

    public override void OnStartClient()
    {
        _voiceClip = AudioClip.Create("voice", _clipLengthSamples, 1, (int)_cachedSampleRate, false);
        
        var silence = new float[_clipLengthSamples];
        _voiceClip.SetData(silence, 0);
        
        _audioSrc.clip = _voiceClip;
        _audioSrc.Play();
        
        _lastPlayPosition = 0;
    }

    public override void OnStartLocalPlayer()
    {
        SteamUser.StartVoiceRecording();
        _isRecording = true;
        _audioSrc.mute = !hearYourself;
    }

    private void Update()
    {
        if (_audioSrc.isPlaying && _voiceClip != null)
        {
            var currentPlayPosition = _audioSrc.timeSamples;
            if (currentPlayPosition != _lastPlayPosition)
            {
                ClearPlayedAudio(_lastPlayPosition, currentPlayPosition);
                _lastPlayPosition = currentPlayPosition;
            }
        }

        if (!isLocalPlayer || !_isRecording) return;
        
        _timer += Time.deltaTime;
        if (_timer < SendInterval) return;
        _timer -= SendInterval;


        const uint optimalBufferSize = 1024; 
        var buffer = new byte[optimalBufferSize];
        
        var result = SteamUser.GetVoice(true, buffer, optimalBufferSize, out var bytesWritten);

        if (result == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
        {
            var trimmed = new byte[bytesWritten];
            System.Array.Copy(buffer, trimmed, bytesWritten);
            CmdSendVoice(trimmed);
        }
    }

    private void ClearPlayedAudio(int from, int to)
    {
        if (to >= from)
        {
            var length = to - from;
            if (length <= 0) return;
            
            var silence = new float[length];
            _voiceClip.SetData(silence, from);
        }
        else
        {
            var part1 = _clipLengthSamples - from;
            if (part1 > 0)
            {
                var silence1 = new float[part1];
                _voiceClip.SetData(silence1, from);
            }
            
            var part2 = to;
            if (part2 > 0)
            {
                var silence2 = new float[part2];
                _voiceClip.SetData(silence2, 0);
            }
        }
    }

    [Command(requiresAuthority = true, channel = Channels.Unreliable)] 
    private void CmdSendVoice(byte[] compressedData)
        => RpcReceiveVoice(compressedData);
    
    [ClientRpc(channel = Channels.Unreliable)]
    private void RpcReceiveVoice(byte[] compressedData)
    {
        if (isLocalPlayer && !hearYourself) return;
        
        var decompressed = new byte[_cachedSampleRate * 4];
        var result = SteamUser.DecompressVoice
        (
            compressedData, (uint)compressedData.Length,
            decompressed, (uint)decompressed.Length,
            out var bytesWritten, _cachedSampleRate
        );
        
        if (result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0) return;
        
        var sampleCount = (int)(bytesWritten / 2);
        var samples = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var raw = (short)(decompressed[i * 2] | (decompressed[i * 2 + 1] << 8));
            
            var sample = raw / 32768f * volume * micGainBoost;
            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        WriteAudioToClip(samples);
    }
    
    private void WriteAudioToClip(float[] samples)
    {
        if (_voiceClip == null) return;

        var playPosition = _audioSrc.timeSamples;
        
        var distance = (_writePosition - playPosition + _clipLengthSamples) % _clipLengthSamples;
        if (distance > _clipLengthSamples / 2)
        {
            _writePosition = (playPosition + (int)(_cachedSampleRate * 0.07f)) % _clipLengthSamples;
        }
        
        if (_writePosition + samples.Length <= _clipLengthSamples)
        {
            _voiceClip.SetData(samples, _writePosition);
            _writePosition = (_writePosition + samples.Length) % _clipLengthSamples;
        }
        else
        {
            var firstPartLength = _clipLengthSamples - _writePosition;
            var firstPart = new float[firstPartLength];
            System.Array.Copy(samples, 0, firstPart, 0, firstPartLength);
            _voiceClip.SetData(firstPart, _writePosition);

            var secondPartLength = samples.Length - firstPartLength;
            var secondPart = new float[secondPartLength];
            System.Array.Copy(samples, firstPartLength, secondPart, 0, secondPartLength);
            _voiceClip.SetData(secondPart, 0);

            _writePosition = secondPartLength;
        }
    }

    public override void OnStopLocalPlayer()
    {
        if (_isRecording)
        {
            SteamUser.StopVoiceRecording();
            _isRecording = false;
        }
    }

    private void OnDisable()
    {
        if (_isRecording && isLocalPlayer)
        {
            SteamUser.StopVoiceRecording();
            _isRecording = false;
        }
    }
}