using System.Runtime.InteropServices;

namespace VibeVault;

internal static class AudioPlayerFactory
{
    public static IAudioPlayer Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsNaudioAudioPlayer naudio = new WindowsNaudioAudioPlayer();
            if (naudio.IsAvailable) return naudio;
            naudio.Dispose();

            WindowsMciAudioPlayer winmm = new WindowsMciAudioPlayer();
            if (winmm.IsAvailable) return winmm;
            winmm.Dispose();
        }

        return new ExternalAudioPlayer();
    }
}
