# DCLVideoPlayerUnity

Unity Video Player using FFMPEG for Windows/Linux/Mac

![Preview](preview.png)

It uses the C video player library:

https://github.com/decentraland/DCLVideoPlayer

The CI generates the necessary binaries for Windows/Linux/Mac.

## TODO:

- Improve: Remove YUV->RGB transformation inside DCLVideoPlayer and create a shader in Unity to make the process in the GPU