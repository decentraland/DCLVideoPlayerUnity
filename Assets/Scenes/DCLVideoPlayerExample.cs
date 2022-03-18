using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DCLVideoPlayerExample : MonoBehaviour
{
    public string videoPath = "";

    private DCLVideoPlayer videoPlayer;

    private void Awake()
    {
        videoPlayer = new DCLVideoPlayer(videoPath);
        var material = GetComponent<MeshRenderer>().sharedMaterial;
        var texture = videoPlayer.GetTexture();
        material.mainTexture = texture;
        videoPlayer.SetLoop(true);
        videoPlayer.Play();
    }

    private void Update()
    {
        videoPlayer.UpdateVideoTexture();
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
}
