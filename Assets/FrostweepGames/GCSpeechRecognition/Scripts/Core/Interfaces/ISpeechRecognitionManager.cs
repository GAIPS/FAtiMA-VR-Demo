using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition
{
    public interface ISpeechRecognitionManager
    {
        event Action<RecognitionResponse, long> RecognitionSuccessEvent;
        event Action<OperationLongRecognizeResponse, long> LongRecognitionSuccessEvent;
        event Action<OperationResponse, long> GetOperationDataSuccessEvent;

        event Action<string, long> NetworkRequestFailedEvent;

        Config CurrentConfig { get; }
        Dictionary<OperationLongRecognizeResponse, Enumerators.NetworkRequestStatus> OperationNames { get; set; }

        void SetConfig(Config config);
        void Recognize(AudioClip clip, List<string[]> contexts, Enumerators.LanguageCode language);
        void GetOperation(string name);
    }
}