using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ProximityChat : NetworkBehaviour
{
    public float voiceVolume = 5f;
    
    private AudioSource _audioSrc;
    private bool _isRecording;

    private float _sendInterval = 0.05f; //send data 20 times a second
    private float _timer;

    private Queue<float[]> _jitterBuffer = new();
    private const int JitterPackets = 2;
    private float[] _currentPacket;
    private int _currentPacketPos;

    private static uint SampleRate => SteamUser.GetVoiceOptimalSampleRate();

    private void Awake()
    {
        _audioSrc = GetComponent<AudioSource>();
        _audioSrc.spatialBlend = 0f; //temp, set to 1f for real proximity
        _audioSrc.rolloffMode = AudioRolloffMode.Logarithmic;
        _audioSrc.minDistance = 8f;
        _audioSrc.maxDistance = 25f;
        _audioSrc.loop = true;
        _audioSrc.volume = 1f;
    }

    public override void OnStartClient()
    {
        var rate = (int)SampleRate;
        var streamClip = AudioClip.Create("VoiceStream", rate * 2, 1, rate, true, OnAudioRead);
        _audioSrc.clip = streamClip;
        _audioSrc.Play();
    }

    public override void OnStartLocalPlayer()
    {
        SteamUser.StartVoiceRecording();
        _isRecording = true;
        _audioSrc.mute = true;
    }
    
    private void OnAudioRead(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            if (_currentPacket == null && _jitterBuffer.Count >= JitterPackets)
                _currentPacket = _jitterBuffer.Dequeue();

            if (_currentPacket != null)
            {
                data[i] = _currentPacket[_currentPacketPos++];

                if (_currentPacketPos >= _currentPacket.Length)
                {
                    _currentPacket = _jitterBuffer.Count > 0 ? _jitterBuffer.Dequeue() : null;
                    _currentPacketPos = 0;
                }
            }
            else
            {
                data[i] = 0f;
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || !_isRecording) return;

        _timer += Time.deltaTime;
        if (_timer < _sendInterval) return;
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
        if (isLocalPlayer) return;

        var sampleRate = SampleRate;
        var decompressed = new byte[sampleRate * 4];

        var result = SteamUser.DecompressVoice
        (
            compressedData, (uint)compressedData.Length,
            decompressed, (uint)decompressed.Length,
            out var bytesWritten, sampleRate
        );

        if (result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0)
        {
            Debug.LogWarning($"decompressVoice failed: {result}, bytesWritten: {bytesWritten}");
            return;
        }

        var sampleCount = (int)(bytesWritten / 2);
        var samples = new float[sampleCount];
        var maxSample = 0f;

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)(decompressed[i * 2] | (decompressed[i * 2 + 1] << 8));
            samples[i] = Mathf.Clamp(sample / 32768f * voiceVolume, -1f, 1f);
            maxSample = Mathf.Max(maxSample, Mathf.Abs(samples[i]));
        }
        
        if (maxSample < 0.01f) return;

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
        if (isLocalPlayer && _isRecording)
        {
            SteamUser.StopVoiceRecording();
            _isRecording = false;
        }
    }
}