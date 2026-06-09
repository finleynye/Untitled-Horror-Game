using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ProximityChat : NetworkBehaviour
{
    public bool hearYourself;
    [Range(5f, 15f)] public float volume; //increase/decrease everyone else's volume (idk how to boost your own volume yet cut me some slack)
    //eventually maybe fin can make a ui with everyones volume sliders so we can have separate volumes per person
    //like in content warning or smth
    
    [Header("Proximity Range")]
    [SerializeField] private float minDistance = 5; //full volume within this range
    [SerializeField] private float maxDistance = 15; //silence beyond this
    
    private AudioSource _audioSrc;
    private bool _isRecording;

    private readonly float SendInterval = 0.05f; //20x per second
    private float _timer;

    private Queue<float[]> _jitterBuffer = new();
    private const int JitterPackets = 2; //higher = cleaner audio but more delay
    private float[] _currentPacket;
    private int _currentPacketPos;
    
    private const float SilenceThreshold = 0.01f; //packets below this amplitude gonna be ignored
    private static uint SampleRate => SteamUser.GetVoiceOptimalSampleRate(); //dynamically changes between like 11k and 48k to reduce cpu usage during decomp

    private void Awake()
    {
        _audioSrc = GetComponent<AudioSource>();
        
        _audioSrc.spatialBlend = 1f; //0 = 2D, 1 = 3D
        _audioSrc.rolloffMode = AudioRolloffMode.Linear;
        _audioSrc.minDistance = minDistance;
        _audioSrc.maxDistance = maxDistance;
        _audioSrc.loop = true;
        _audioSrc.volume = 1f;
        _audioSrc.dopplerLevel = 0f;
    }

    public override void OnStartClient()
    {
        var rate = (int)SampleRate;
        var streamClip = AudioClip.Create("voice", rate * 2, 1, rate, true, OnAudioRead);
        _audioSrc.clip = streamClip;
        _audioSrc.Play();
    }

    public override void OnStartLocalPlayer()
    {
        SteamUser.StartVoiceRecording();
        _isRecording = true;
        _audioSrc.mute = !hearYourself;
    }

    private void OnAudioRead(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            //make sure jitter buffer has enough data before starting playback x
            if(_currentPacket == null && _jitterBuffer.Count >= JitterPackets)
                _currentPacket = _jitterBuffer.Dequeue();

            if (_currentPacket != null)
            {
                data[i] = _currentPacket[_currentPacketPos++];
                if (_currentPacketPos >= _currentPacket.Length)
                {
                    //move onto next packet
                    _currentPacket = _jitterBuffer.Count > 0 ? _jitterBuffer.Dequeue() : null;
                    _currentPacketPos = 0;
                }
            }
            else
                data[i] = 0f;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || !_isRecording) return;
        
        _timer += Time.deltaTime;
        if (_timer < SendInterval) return;
        _timer = 0f;

        var result = SteamUser.GetAvailableVoice(out var bytesAvailable);
        if (result != EVoiceResult.k_EVoiceResultOK || bytesAvailable == 0) return;
        
        var buffer = new byte[bytesAvailable];
        result = SteamUser.GetVoice(true, buffer, bytesAvailable, out var bytesWritten);

        if (result == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
        {
            var trimmed = new byte[bytesWritten];
            System.Array.Copy(buffer, trimmed, bytesWritten);
            CmdSendVoice(trimmed);
        }
    }
    
    [Command(requiresAuthority = true)]
    private void CmdSendVoice(byte[] compressedData)
        => RpcReceiveVoice(compressedData);

    [ClientRpc]
    private void RpcReceiveVoice(byte[] compressedData)
    {
        if (isLocalPlayer && !hearYourself) return;
        
        var sampleRate = SampleRate;
        var decompressed = new byte[sampleRate * 4];
        var result = SteamUser.DecompressVoice
        (
            compressedData, (uint)compressedData.Length,
            decompressed, (uint)decompressed.Length,
            out var bytesWritten, sampleRate
        );
        
        if (result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0) return;
        
        var sampleCount = (int)(bytesWritten / 2);
        var samples = new float[sampleCount];
        var peakAmplitude = 0f; //the peak

        for (var i = 0; i < sampleCount; i++)
        {
            var raw = (short)(decompressed[i * 2] | (decompressed[i * 2 + 1] << 8));
            var sample = raw / 32768f * volume;
            samples[i] = Mathf.Clamp(sample, -1f, 1f);
            peakAmplitude = Mathf.Max(peakAmplitude, Mathf.Abs(samples[i]));
        }
        
        //drop packets containing silence
        if (peakAmplitude < SilenceThreshold) return;
        
        _jitterBuffer.Enqueue(samples);
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
