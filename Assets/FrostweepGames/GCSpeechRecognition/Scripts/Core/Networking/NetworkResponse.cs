namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition
{
    public class NetworkResponse
    {
        public long netPacketIndex;
        public Enumerators.GoogleNetworkType recognitionType;

        public string response;
        public string error;

        public NetworkResponse(string resp, string err, long index, Enumerators.GoogleNetworkType type)
        {
            recognitionType = type;
            netPacketIndex = index;
            response = resp;
            error = err;
        }
    }
}