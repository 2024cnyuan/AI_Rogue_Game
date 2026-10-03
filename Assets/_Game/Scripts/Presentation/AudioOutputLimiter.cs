using UnityEngine;
namespace Starfall
{
    // Runs on Unity's audio thread, after all mixer groups. No allocations or Unity API calls.
    public sealed class AudioOutputLimiter : MonoBehaviour
    {
        public volatile float peak;
        public static float Limit(float sample) {
            float magnitude=sample<0?-sample:sample;
            if(magnitude<=.65f) return sample;
            float excess=magnitude-.65f;
            float result=.65f+excess/(1+excess/.3f);
            return sample<0?-result:result;
        }
        void OnAudioFilterRead(float[] data,int channels) {
            float maximum=0;
            for(int i=0;i<data.Length;i++) { float value=Limit(data[i]); data[i]=value; float absolute=value<0?-value:value; if(absolute>maximum) maximum=absolute; }
            peak=maximum;
        }
    }
}
