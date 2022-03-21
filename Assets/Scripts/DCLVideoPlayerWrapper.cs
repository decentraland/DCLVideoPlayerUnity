using System;
using System.Runtime.InteropServices;

public class DCLVideoPlayerWrapper
{
    private const string NATIVE_LIBRARY_NAME = "libdclvideoplayer";
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_join_threads();
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern IntPtr player_create(string url);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_destroy(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_play(IntPtr vpc);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_stop(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern int player_is_playing(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern int player_is_buffering(IntPtr vpc);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern int player_get_state(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_set_paused(IntPtr vpc, int paused);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_set_loop(IntPtr vpc, int loop);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern int player_has_loop(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern float player_get_length(IntPtr vpc);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern float player_get_playback_position(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_set_playback_rate(IntPtr vpc, double rate);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_seek(IntPtr vpc, float time);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern double player_grab_video_frame(IntPtr vpc, ref IntPtr releasePtr, ref IntPtr data);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern double player_grab_audio_frame(IntPtr vpc, ref IntPtr releasePtr, ref IntPtr data, ref int frameSize);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_release_frame(IntPtr vpc, IntPtr data);
    
    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_get_video_format(IntPtr vpc, ref int width, ref int height);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_get_audio_format(IntPtr vpc, ref int frequency, ref int channels);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern double player_get_start_time(IntPtr vpc);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern void player_set_start_time(IntPtr vpc, double time);

    [DllImport(NATIVE_LIBRARY_NAME)]
    public static extern double player_get_global_time(IntPtr vpc);
}