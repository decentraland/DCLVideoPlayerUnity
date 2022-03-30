using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DCLVideoPlayerExample : MonoBehaviour
{
    public string videoPath = "";

    private DCLVideoPlayer videoPlayer;

    public InputField inputField = null;
    public Slider slider = null;
    public Text positionText = null;
    public GameObject bufferingUI;
    private bool sliderChangedByUser = true;

    private void Awake()
    {
        inputField.text = videoPath;
        videoPlayer = new DCLVideoPlayer(videoPath);
        
        slider.onValueChanged.AddListener(delegate { OnSliderChanged(); });
    }

    private void OnSliderChanged()
    {
        if (sliderChangedByUser)
        {
            float sliderValue = slider.value;
            Debug.Log($"Slider Changed: {sliderValue}");
            if (videoPlayer != null)
                videoPlayer.SetSeekTime(sliderValue);
        }
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
                    if (videoPlayer.HasVideo())
                    {
                        if (videoPlayer.GetTexture() == null)
                        {
                            videoPlayer.PrepareTexture();
                            var material = GetComponent<MeshRenderer>().sharedMaterial;
                            var texture = videoPlayer.GetTexture();
                            material.mainTexture = texture;
                            videoPlayer.SetLoop(true);
                            videoPlayer.Play();

                            slider.minValue = 0;
                            slider.value = 0;
                            slider.maxValue = videoPlayer.GetDuration();
                        }
                        else
                        {
                            videoPlayer.UpdateVideoTexture();
                            UpdateSlider();

                            bufferingUI.SetActive(videoPlayer.IsBuffering());
                        }
                    }

                    break;
            }
        }
    }

    private string GetPositionText(float time)
    {
        int minutes = (int)(time / 60.0f);
        int seconds = (int)(time % 60.0f);
        return $"{minutes:00}:{seconds:00}";
    }
    private void UpdateSlider()
    {
        if (videoPlayer.IsPlaying())
        {
            sliderChangedByUser = false;
            slider.value = videoPlayer.GetPlaybackPosition();
            sliderChangedByUser = true;

            positionText.text = GetPositionText(videoPlayer.GetPlaybackPosition()) + " / " +
                                GetPositionText(videoPlayer.GetDuration());
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
