using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DCLVideoPlayerExample : MonoBehaviour
{
    public string videoPath = "";

    private DCLVideoPlayer videoPlayer;

    public void QuitGame()
    {
        // save any game data here
        #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void Awake()
    {
        videoPlayer = new DCLVideoPlayer(videoPath);
    }

    private void OnDestroy()
    {
        videoPlayer.Dispose();
        DCLVideoPlayer.WaitAllThreads();
    }

    private void Update()
    {
        switch (videoPlayer.GetState())
        {
            case DCLVideoPlayer.VideoPlayerState.Loading:
                break;
            case DCLVideoPlayer.VideoPlayerState.Error:
                Debug.LogError("Decoder error");
                QuitGame();
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
}
