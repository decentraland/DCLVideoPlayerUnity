using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class DCLVideoPlayer : IDisposable
{
    private GameObject localObject;

    private IntPtr vpc;
    
    // Video
    private bool newFrame = false;
    private Texture2D videoTexture;
    private int videoWidth = 0;
    private int videoHeight = 0;
    private int videoSize = 0;
    
    // Audio
    private const int SWAP_BUFFER_NUM = 4; //	How many audio source to swap.
    private readonly AudioSource[] audioSource = new AudioSource[SWAP_BUFFER_NUM];
    private const int AUDIO_FRAME_SIZE = 2048; //  Audio clip data size. Packed from audioDataBuff.
    private List<float> audioDataBuff; //  Buffer to keep audio data decoded from native.
    private int audioFrequency;
    private int audioChannels;
    private const double OVERLAP_TIME = 0.02; //  Our audio clip is defined as: [overlay][audio data][overlap].
    private int audioOverlapLength; //  OVERLAP_TIME * audioFrequency.
    private int audioDataLength; //  (AUDIO_FRAME_SIZE + 2 * audioOverlapLength) * audioChannel.
    private float volume = 1.0f;
    private float[] tempBuff = new float[0]; //	Buffer to copy audio data from dataPtr to audioDataBuff.

    private int swapIndex = 0; //	Swap between audio sources.
    private double audioDataTime = 0.0;
    private int playedAudioDataLength = 0; //  Data length exclude the overlap length.

    // Time control
    private double firstAudioFrameTime = -1.0;
    private double lastVideoFrameTime = -1.0;
    private double audioProgressTime = -1.0f; //	Avoid to schedule the same audio data set.
    private double globalNativeCreateTime = 0.0f;
    private double globalDSPCreateTime = 0.0f;
    private bool cleanAudio = false;

    private void initAudioSource()
    {
        DCLVideoPlayerWrapper.player_get_audio_format(vpc, ref audioFrequency, ref audioChannels);
        audioOverlapLength = (int) (OVERLAP_TIME * audioFrequency + 0.5f);

        audioDataLength = (AUDIO_FRAME_SIZE + 2 * audioOverlapLength) * audioChannels;
        for (var i = 0; i < SWAP_BUFFER_NUM; i++)
        {
            if (audioSource[i] == null) audioSource[i] = localObject.AddComponent<AudioSource>();
            audioSource[i].clip =
                AudioClip.Create("testSound" + i, audioDataLength, audioChannels, audioFrequency, false);
            audioSource[i].playOnAwake = false;
            audioSource[i].volume = volume;
            audioSource[i].minDistance = audioSource[i].maxDistance;
            audioSource[i].spatialize = false;
            audioSource[i].SetSpatializerFloat(0, 0.0f);
        }
        audioDataBuff = new List<float>();

        swapIndex = 0; //	Swap between audio sources.
        audioDataTime = (double) AUDIO_FRAME_SIZE / audioFrequency;
        playedAudioDataLength = AUDIO_FRAME_SIZE * audioChannels; //  Data length exclude the overlap length.
    }

    private double dspTime2nativeTime(double dspTime)
    {
        return (dspTime - globalDSPCreateTime) + globalNativeCreateTime;
    }
 
    private double nativeTime2dspTime(double nativeTime)
    {
        return (nativeTime - globalNativeCreateTime) + globalDSPCreateTime;
    }

    public DCLVideoPlayer(string videoPath)
    {
        localObject = new GameObject("_VideoPlayer");

        vpc = DCLVideoPlayerWrapper.player_create(videoPath);
        globalNativeCreateTime = DCLVideoPlayerWrapper.player_get_global_time(vpc);
        globalDSPCreateTime = AudioSettings.dspTime;

        DCLVideoPlayerWrapper.player_get_video_format(vpc, ref videoWidth, ref videoHeight);
        videoTexture = new Texture2D(videoWidth, videoHeight, TextureFormat.RGB24, false, false);
        videoSize = videoWidth * videoHeight * 3;

        initAudioSource();
    }

    public void Dispose()
    {
        DCLVideoPlayerWrapper.player_destroy(vpc);
    }

    private void GrabVideoFrame()
    {
        var videoReleasePtr = IntPtr.Zero;
        var videoDataPtr = IntPtr.Zero;
        do {
            videoReleasePtr = IntPtr.Zero;
            videoDataPtr = IntPtr.Zero;
            double videoNativeTime = DCLVideoPlayerWrapper.player_grab_video_frame(vpc, ref videoReleasePtr, ref videoDataPtr);

            if (videoDataPtr != IntPtr.Zero)
            {
                if (lastVideoFrameTime > videoNativeTime && firstAudioFrameTime != -1.0) {
                    // LOOP DETECTED
                    double videoDuration = DCLVideoPlayerWrapper.player_get_length(vpc);
                    firstAudioFrameTime -= videoDuration;
                    audioProgressTime -= videoDuration;
                }
                lastVideoFrameTime = videoNativeTime;

                // Main thread
                if (videoDataPtr != IntPtr.Zero)
                {
                    videoTexture.LoadRawTextureData(videoDataPtr, videoSize);
                    newFrame = true;
                }

                DCLVideoPlayerWrapper.player_release_frame(vpc, videoReleasePtr);
            }
        } while (videoDataPtr != IntPtr.Zero);
    }

    private void GrabAudioFrame()
    {
        if (cleanAudio) {
            CleanAudio();
            cleanAudio = false;
        }
        var audioReleasePtr = IntPtr.Zero;
        var audioDataPtr = IntPtr.Zero;
        int audioFrameLength = 0;
        double audioNativeTime = DCLVideoPlayerWrapper.player_grab_audio_frame(vpc, ref audioReleasePtr, ref audioDataPtr, ref audioFrameLength);

        if (audioDataPtr != IntPtr.Zero)
        {   
            if (firstAudioFrameTime == -1.0) firstAudioFrameTime = audioNativeTime;

            audioFrameLength *= audioChannels;
            if (tempBuff.Length !=
                audioFrameLength) //  For dynamic audio data length, reallocate the memory if needed.
                tempBuff = new float[audioFrameLength];
            Marshal.Copy(audioDataPtr, tempBuff, 0, audioFrameLength);
            audioDataBuff.AddRange(tempBuff);

            DCLVideoPlayerWrapper.player_release_frame(vpc, audioReleasePtr);
        }

        if (DCLVideoPlayerWrapper.player_is_playing(vpc) == 1 && DCLVideoPlayerWrapper.player_is_buffering(vpc) == 0 && firstAudioFrameTime != -1.0)
        {
            if (audioDataBuff != null && audioDataBuff.Count >= audioDataLength)
            {
                double nativeStartTime = DCLVideoPlayerWrapper.player_get_start_time(vpc);
                double nativeCurrentTime = DCLVideoPlayerWrapper.player_get_global_time(vpc);
                if (audioProgressTime == -1.0)
                {
                    //  To simplify, the first overlap data would not be played.
                    //  Correct the audio progress time by adding OVERLAP_TIME.
                    audioProgressTime = firstAudioFrameTime + OVERLAP_TIME;
                    nativeStartTime -= audioProgressTime;
                }

                //  Re-check data length if audioDataBuff is cleared by seek.
                if (audioDataBuff.Count >= audioDataLength && !audioSource[swapIndex].isPlaying)
                {
                    var playTime = audioProgressTime + nativeStartTime;
                    var endTime = playTime + audioDataTime;

                    if (nativeTime2dspTime(playTime) > AudioSettings.dspTime)
                    {

                        audioSource[swapIndex].clip
                            .SetData(audioDataBuff.GetRange(0, audioDataLength).ToArray(), 0);

                        double playTimeDSP = nativeTime2dspTime(playTime);
                        double endTimeDSP = nativeTime2dspTime(endTime);

                        audioSource[swapIndex].PlayScheduled(nativeTime2dspTime(playTime));
                        audioSource[swapIndex].SetScheduledEndTime(nativeTime2dspTime(endTime));
                        audioSource[swapIndex].time = (float) OVERLAP_TIME;
                        swapIndex = (swapIndex + 1) % SWAP_BUFFER_NUM;
                    }
                    audioProgressTime += audioDataTime;
                    audioDataBuff.RemoveRange(0, playedAudioDataLength);
                }
            }
        }
    }

    private void CleanAudio()
    {
        if (audioDataBuff != null)
            audioDataBuff.Clear();

        audioProgressTime = firstAudioFrameTime = -1.0;
        foreach (var src in audioSource) src.Stop();
    }

    public void Update()
    {
        DCLVideoPlayerWrapper.player_process(vpc); // Thread please
            
        GrabVideoFrame();
        GrabAudioFrame();
    }

    public void Play()
    {
        if (DCLVideoPlayerWrapper.player_is_playing(vpc) == 0)
        {
            DCLVideoPlayerWrapper.player_play(vpc);
        }
    }

    public void Pause()
    {
        if (DCLVideoPlayerWrapper.player_is_playing(vpc) == 1)
        {
            DCLVideoPlayerWrapper.player_stop(vpc);
        }
    }

    public void UpdateVideoTexture()
    {
        Update();
        if (newFrame && videoTexture != null)
        {
            videoTexture.Apply();
            newFrame = false;
        }
    }

    public Texture2D GetTexture()
    {
        return videoTexture;
    }
    
    private void PlayerSeek(float time)
    {
        cleanAudio = true;
        time = Mathf.Clamp(time, 0.0f, GetDuration());
        DCLVideoPlayerWrapper.player_seek(vpc, time);
    }

    public void SetSeekTime(float seekTime)
    {
        PlayerSeek(seekTime);
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp(vol, 0.0f, 1.0f);
        foreach (var src in audioSource)
            if (src != null)
                src.volume = volume;
    }

    public float GetVolume()
    {
        return volume;
    }

    public void Mute()
    {
        var temp = volume;
        SetVolume(0.0f);
        volume = temp;
    }

    public void Unmute()
    {
        SetVolume(volume);
    }

    public float GetPlaybackPosition()
    {
        return DCLVideoPlayerWrapper.player_get_playback_position(vpc);
    }

    public float GetDuration()
    {
        return DCLVideoPlayerWrapper.player_get_length(vpc);
    }

    public bool IsPlaying()
    {
        return DCLVideoPlayerWrapper.player_is_playing(vpc) == 1;
    }

    public bool IsBuffering()
    {
        return DCLVideoPlayerWrapper.player_is_buffering(vpc) == 1;
    }

    public void SetLoop(bool loop)
    {
        DCLVideoPlayerWrapper.player_set_loop(vpc, loop ? 1 : 0);
    }

    public bool HasLoop()
    {
        return DCLVideoPlayerWrapper.player_has_loop(vpc) == 1;
    }
}