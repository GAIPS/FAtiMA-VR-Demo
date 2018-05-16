using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition
{
    public class GCSpeechRecognition : MonoBehaviour
    {
        public event Action<RecognitionResponse, long> RecognitionSuccessEvent;
        public event Action<OperationResponse, long> LongRecognitionSuccessEvent;
        public event Action<string, long> NetworkRequestFailedEvent;

        public event Action StartedRecordEvent;
        public event Action<AudioClip> FinishedRecordEvent;
        public event Action RecordFailedEvent;

        public event Action BeginTalkigEvent;
        public event Action<AudioClip> EndTalkigEvent;


        private static GCSpeechRecognition _Instance;
        public static GCSpeechRecognition Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new GameObject("[Singleton]GCSpeechRecognition").AddComponent<GCSpeechRecognition>();

                return _Instance;
            }
        }


        private ServiceLocator _serviceLocator;

        private ISpeechRecognitionManager _speechRecognitionManager;
        private IMediaManager _mediaManager;

        private List<string[]> _currentSpeechContexts;

        public ServiceLocator ServiceLocator { get { return _serviceLocator; } }

        [Header("Prefab Config Settings")]
        public int currentConfigIndex = 0;
        public List<Config> configs;

        [Header("Prefab Object Settings")]
        public bool isDontDestroyOnLoad = false;
        public bool isFullDebugLogIfError = false;
        public bool isUseAPIKeyFromPrefab = false;

        [Header("Prefab Fields")]
        public string apiKey;

        private void Awake()
        {
            if (_Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (isDontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            _Instance = this;

            _serviceLocator = new ServiceLocator();
            _serviceLocator.InitServices();

            _mediaManager = _serviceLocator.Get<IMediaManager>();
            _speechRecognitionManager = _serviceLocator.Get<ISpeechRecognitionManager>();

            _mediaManager.StartedRecordEvent += StartedRecordEventHandler;
            _mediaManager.FinishedRecordEvent += FinishedRecordEventHandler;
            _mediaManager.RecordFailedEvent += RecordFailedEventHandler;
            _mediaManager.BeginTalkigEvent += BeginTalkigEventHandler;
            _mediaManager.EndTalkigEvent += EndTalkigEventHandler;

            _speechRecognitionManager.SetConfig(configs[currentConfigIndex]);

            _speechRecognitionManager.RecognitionSuccessEvent += RecognitionSuccessEventHandler;
            _speechRecognitionManager.NetworkRequestFailedEvent += RecognitionFailedEventHandler;
            _speechRecognitionManager.LongRecognitionSuccessEvent += LongRecognitionSuccessEventHandler;
            _speechRecognitionManager.GetOperationDataSuccessEvent += GetOperationDataSuccessEventHandler;
        }

        private void Update()
        {
            if (_Instance == this)
            {
                _serviceLocator.Update();
            }
        }

        private void OnDestroy()
        {
            if (_Instance == this)
            {
                _mediaManager.StartedRecordEvent -= StartedRecordEventHandler;
                _mediaManager.FinishedRecordEvent -= FinishedRecordEventHandler;
                _mediaManager.RecordFailedEvent -= RecordFailedEventHandler;
                _mediaManager.BeginTalkigEvent -= BeginTalkigEventHandler;
                _mediaManager.EndTalkigEvent -= EndTalkigEventHandler;

                _speechRecognitionManager.RecognitionSuccessEvent -= RecognitionSuccessEventHandler;
                _speechRecognitionManager.NetworkRequestFailedEvent -= RecognitionFailedEventHandler;
                _speechRecognitionManager.LongRecognitionSuccessEvent -= LongRecognitionSuccessEventHandler;
                _speechRecognitionManager.GetOperationDataSuccessEvent -= GetOperationDataSuccessEventHandler;


                _Instance = null;
                _serviceLocator.Dispose();
            }
        }

        public void StartRecord(bool isEnabledVoiceDetection)
        {
            _mediaManager.IsEnabledVoiceDetection = isEnabledVoiceDetection;
            _mediaManager.StartRecord();
        }

        public void StopRecord()
        {
            _mediaManager.StopRecord();
        }

        public void Recognize(AudioClip clip, List<string[]> contexts, Enumerators.LanguageCode language)
        {
            _speechRecognitionManager.Recognize(clip, contexts, language);
        }

        public void SetLanguage(Enumerators.LanguageCode language)
        {
            _speechRecognitionManager.CurrentConfig.defaultLanguage = language;
        }

        public void SetContext(List<string[]> contexts)
        {
            _currentSpeechContexts = contexts;
        }

        private void RecognitionSuccessEventHandler(RecognitionResponse arg1, long arg2)
        {
            if (RecognitionSuccessEvent != null)
                RecognitionSuccessEvent(arg1, arg2);
        }

        private void RecognitionFailedEventHandler(string arg1, long arg2)
        {
            if (NetworkRequestFailedEvent != null)
            {

                NetworkRequestFailedEvent(arg1, arg2);
            }
        }

        private void RecordFailedEventHandler()
        {
            if (RecordFailedEvent != null)
                RecordFailedEvent();
        }

        private void BeginTalkigEventHandler()
        {
            if (BeginTalkigEvent != null)
                BeginTalkigEvent();
        }

        private void EndTalkigEventHandler(AudioClip clip)
        {
            if (EndTalkigEvent != null)
                EndTalkigEvent(clip);

            _speechRecognitionManager.Recognize(clip, _currentSpeechContexts, _speechRecognitionManager.CurrentConfig.defaultLanguage);
        }

        private void StartedRecordEventHandler()
        {
            if (StartedRecordEvent != null)
                StartedRecordEvent();
        }

        private void FinishedRecordEventHandler(AudioClip clip)
        {
            if (FinishedRecordEvent != null)
                FinishedRecordEvent(clip);

            if (!_mediaManager.IsEnabledVoiceDetection)
                _speechRecognitionManager.Recognize(clip, _currentSpeechContexts, _speechRecognitionManager.CurrentConfig.defaultLanguage);
        }

        private void LongRecognitionSuccessEventHandler(OperationLongRecognizeResponse response, long index)
        {
            _speechRecognitionManager.OperationNames.Add(response, Enumerators.NetworkRequestStatus.WAITING_FOR_SEND);
        }

        private void GetOperationDataSuccessEventHandler(OperationResponse response, long index)
        {
            if (response.done)
            {
                var item = _speechRecognitionManager.OperationNames.First(x => x.Key.name == response.name);
                _speechRecognitionManager.OperationNames[item.Key] = Enumerators.NetworkRequestStatus.DONE;

                if (LongRecognitionSuccessEvent != null)
                    LongRecognitionSuccessEvent(response, index);
            }
            else
            {
                var item = _speechRecognitionManager.OperationNames.First(x => x.Key.name == response.name);
                _speechRecognitionManager.OperationNames[item.Key] = Enumerators.NetworkRequestStatus.WAITING_FOR_SEND;
            }
        }
    }
}