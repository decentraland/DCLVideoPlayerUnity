using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DCLVideoPlayerExample : MonoBehaviour
{
    public string videoPath = "";

    private DCLVideoPlayer videoPlayer;

    public InputField inputField = null;

    private void Awake()
    {
        inputField.text = videoPath;
        videoPlayer = new DCLVideoPlayer(videoPath);
    }

    private void OnDestroy()
    {
        videoPlayer.Dispose();
        DCLVideoPlayer.StopAllThreads();
    }

    private void Update()
    {
        if (videoPlayer != null) {
            switch (videoPlayer.GetState())
            {
                case DCLVideoPlayer.VideoPlayerState.Loading:
                    break;
                case DCLVideoPlayer.VideoPlayerState.Error:
                    Debug.LogError("Decoder error");
                    videoPlayer.Dispose();
                    break;
                case DCLVideoPlayer.VideoPlayerState.Ready:
                    if (videoPlayer.GetTexture() == null)
                    {
                        videoPlayer.PrepareTexture();
                        var material = GetComponent<MeshRenderer>().sharedMaterial;
                        var texture = videoPlayer.GetTexture();
                        material.mainTexture = texture;
                        videoPlayer.SetLoop(true);
                        videoPlayer.Play();
                    }
                    else
                    {
                        videoPlayer.UpdateVideoTexture();
                    }
                    break;
            }
        }
    }

    public void PlayPause()
    {
        if (!videoPlayer.IsPlaying()) {
            videoPlayer.Play();
        } else {
            videoPlayer.Pause();
        }
    }

    public void Forward10Seconds()
    {
        float currentTime = videoPlayer.GetPlaybackPosition();
        videoPlayer.SetSeekTime(currentTime + 10.0f);
    }

    public void Backward10Seconds()
    {
        float currentTime = videoPlayer.GetPlaybackPosition();
        videoPlayer.SetSeekTime(currentTime - 10.0f);
    }

    public void MoreVolume()
    {
        videoPlayer.SetVolume(videoPlayer.GetVolume() + 0.1f);
    }

    public void LessVolume()
    {
        videoPlayer.SetVolume(videoPlayer.GetVolume() - 0.1f);
    }
    
    public void MoreSpeed()
    {
        videoPlayer.SetPlaybackRate(videoPlayer.GetPlaybackRate() + 0.25);
    }

    public void LessSpeed()
    {
        videoPlayer.SetPlaybackRate(videoPlayer.GetPlaybackRate() - 0.25);
    }

    public void PlayURL()
    {
        videoPlayer.Dispose();
        videoPath = inputField.text;
        videoPlayer = new DCLVideoPlayer(videoPath);
    }
}
