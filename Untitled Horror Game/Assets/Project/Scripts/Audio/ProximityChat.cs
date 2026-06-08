using UnityEngine;
using Mirror;
using Steamworks;

[RequireComponent(typeof(AudioSource))]
public class ProximityChat : NetworkBehaviour
{
    private AudioSource _audioSrc;
    private bool _isRecording;

    private float _sendInterval = 0.05f; //send data 20 times a second
    private float _timer;

    private uint _sampleRate;

    private void Awake()
    {
        _audioSrc = GetComponent<AudioSource>();
        _audioSrc.spatialBlend = 1f;
        _audioSrc.rolloffMode = AudioRolloffMode.Linear;
        _audioSrc.minDistance = 2;
        _audioSrc.maxDistance = 15;
    }

    public override void OnStartLocalPlayer()
    {
        _sampleRate = SteamUser.GetVoiceOptimalSampleRate();
        SteamUser.StartVoiceRecording();
        _isRecording = true;
    }

    private void Update()
    {
        if (!isLocalPlayer || !_isRecording) return;
        
        _timer += Time.deltaTime;
        if (_timer < _sendInterval) return;
        _timer = 0f;

        var result = SteamUser.GetAvailableVoice(out var bytesAvailable);
        
        if(result != EVoiceResult.k_EVoiceResultOK || bytesAvailable == 0) return;
        
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
    private void CmdSendVoice(byte[] compressedData) //send to all other clients
        => RpcReceiveVoice(compressedData);

    [ClientRpc]
    private void RpcReceiveVoice(byte[] compressedData)
    {
        if (isLocalPlayer) return; //dont let player hear their own voice (bad)

        var sampleRateLocal = SteamUser.GetVoiceOptimalSampleRate();
        var decompressed = new byte[sampleRateLocal];

        var result = SteamUser.DecompressVoice
        (
            compressedData, (uint)compressedData.Length,
            decompressed, (uint)decompressed.Length,
            out var bytesWritten, sampleRateLocal
        );
        
        if(result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0) return;
        
        var sampleCount = (int)(bytesWritten / 2);
        var samples = new float[sampleCount];
        
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)(decompressed[i * 2] | (decompressed[i * 2 + 1] << 8));
            samples[i] = sample / 32768f;
        }
        
        var clip = AudioClip.Create("voice", sampleCount,1 ,(int)sampleRateLocal, false);
        clip.SetData(samples, 0);
        
        _audioSrc.PlayOneShot(clip);
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