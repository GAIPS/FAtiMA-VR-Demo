using UnityEngine;
using System;
using System.Collections.Generic;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition
{
    public class SpeechRecognitionManager : IService, IDisposable, ISpeechRecognitionManager
    {
        private object _lock = new object();

        public event Action<RecognitionResponse, long> RecognitionSuccessEvent;
        public event Action<OperationLongRecognizeResponse, long> LongRecognitionSuccessEvent;
        public event Action<OperationResponse, long> GetOperationDataSuccessEvent;

        public event Action<string, long> NetworkRequestFailedEvent;

        private float _checkOperationTimer = 0f,
                      _checkOperationDelay = 0.1f;



        private Config _currentConfig;

        private Networking _networking;
        private GCSpeechRecognition _gcSpeechRecognition;

        public Config CurrentConfig { get { return _currentConfig; } }

        public Dictionary<OperationLongRecognizeResponse, Enumerators.NetworkRequestStatus> OperationNames { get; set; }

        public void Init()
        {
            _gcSpeechRecognition = GCSpeechRecognition.Instance;

            OperationNames = new Dictionary<OperationLongRecognizeResponse, Enumerators.NetworkRequestStatus>();

            _networking = new Networking();
            _networking.NetworkResponseEvent += NetworkResponseEventHandler;
        }

        public void Update()
        {
            _networking.Update();

            CheckOperations();
        }

        public void Dispose()
        {
            _networking.NetworkResponseEvent -= NetworkResponseEventHandler;
            _networking.Dispose();
        }

        public void SetConfig(Config config)
        {
            _currentConfig = config;
        }

        public void Recognize(AudioClip clip, List<string[]> contexts, Enumerators.LanguageCode language)
        {
            if (_currentConfig == null)
                throw new NotImplementedException("Config isn't seted! Use SetConfig method!");

            if (clip == null)
                throw new NotImplementedException("AudioClip isn't seted!");

            string postData = string.Empty;
            string uri = string.Empty;

            switch (_currentConfig.recognitionType)
            {
                case Enumerators.GoogleNetworkType.SPEECH_RECOGNIZE:
                    {
                        if (!_gcSpeechRecognition.isUseAPIKeyFromPrefab)
                            uri = Constants.RECOGNIZE_REQUEST_URL + Constants.API_KEY_PARAM + Constants.GC_API_KEY;
                        else
                            uri = Constants.RECOGNIZE_REQUEST_URL + Constants.API_KEY_PARAM + _gcSpeechRecognition.apiKey;

                        postData = JsonUtility.ToJson(GenerateRecognizeRequest(
                                   AudioConvert.Convert(clip, _currentConfig.audioEncoding,
                                                              _currentConfig.useVolumeMultiplier,
                                                              _currentConfig.audioVolumeMultiplier), contexts, language));
                    }
                    break;
                case Enumerators.GoogleNetworkType.SPEECH_LONGRECOGNIZE:
                    {
                        if (!_gcSpeechRecognition.isUseAPIKeyFromPrefab)
                            uri = Constants.LONG_RECOGNIZE_REQUEST_URL + Constants.API_KEY_PARAM + Constants.GC_API_KEY;
                        else
                            uri = Constants.LONG_RECOGNIZE_REQUEST_URL + Constants.API_KEY_PARAM + _gcSpeechRecognition.apiKey;

                        postData = JsonUtility.ToJson(GenerateRecognizeRequest(
                                   AudioConvert.Convert(clip, _currentConfig.audioEncoding,
                                                               _currentConfig.useVolumeMultiplier,
                                                               _currentConfig.audioVolumeMultiplier), contexts, language));
                    }
                    break;
                default:
                    throw new NotSupportedException(_currentConfig.recognitionType + " doesn't supported!");
            }

            _networking.SendRequest(uri, postData, _currentConfig.recognitionType);
        }

        public void GetOperation(string name)
        {
            string uri = string.Empty;

            if (!_gcSpeechRecognition.isUseAPIKeyFromPrefab)
                uri = Constants.OPERATIONS_REQUEST_URL + name + Constants.API_KEY_PARAM + Constants.GC_API_KEY;
            else
                uri = Constants.OPERATIONS_REQUEST_URL + name + Constants.API_KEY_PARAM + _gcSpeechRecognition.apiKey;

            _networking.SendRequest(uri, string.Empty, Enumerators.GoogleNetworkType.GET_OPERATION);
        }

        private void NetworkResponseEventHandler(NetworkResponse response)
        {
            if (GCSpeechRecognition.Instance.isFullDebugLogIfError)
                Debug.Log(response.error + "\n" + response.response);

            switch (response.recognitionType)
            {
                case Enumerators.GoogleNetworkType.SPEECH_RECOGNIZE:
                    {
                        if (response.response.Contains("results"))
                        {
                            if (RecognitionSuccessEvent != null)
                                RecognitionSuccessEvent(JsonUtility.FromJson<RecognitionResponse>(response.response), response.netPacketIndex);
                        }
                        else
                        {
                            if (NetworkRequestFailedEvent != null)
                            {
                                Debug.Log("NetworkRequestFailedEvent " + response.response + " and this " + response.netPacketIndex);
                                NetworkRequestFailedEvent(response.response, response.netPacketIndex);
                            }
                        }
                    }
                    break;
                case Enumerators.GoogleNetworkType.SPEECH_LONGRECOGNIZE:
                    {
                        if (response.response.Contains("name"))
                        {
                            if (LongRecognitionSuccessEvent != null)
                                LongRecognitionSuccessEvent(Newtonsoft.Json.JsonConvert.DeserializeObject<OperationLongRecognizeResponse>(response.response), response.netPacketIndex);
                        }
                        else
                        {
                            if (NetworkRequestFailedEvent != null)
                                NetworkRequestFailedEvent(response.response, response.netPacketIndex);
                        }
                    }
                    break;
                case Enumerators.GoogleNetworkType.GET_OPERATION:
                    {
                        if (response.response.Contains("name"))
                        {
                            if (GetOperationDataSuccessEvent != null)
                                GetOperationDataSuccessEvent(Newtonsoft.Json.JsonConvert.DeserializeObject<OperationResponse>(response.response), response.netPacketIndex);
                        }
                        else
                        {
                            if (NetworkRequestFailedEvent != null)
                            {
                                Debug.Log("NetworkRequestFailedEvent " + response.response + " and this " + response.netPacketIndex);
                                NetworkRequestFailedEvent(response.response, response.netPacketIndex);
                            }
                        }

                    }
                    break;
                default: break;
            }
        }

        private RecognitionRequest GenerateRecognizeRequest(string content, List<string[]> contexts, Enumerators.LanguageCode language)
        {
            RecognitionRequest request = new RecognitionRequest();
            request.config.encoding = _currentConfig.audioEncoding.ToString();
            request.config.languageCode = language.ToString().Replace("_", "-");
            request.config.sampleRateHertz = _currentConfig.sampleRate;
            request.config.maxAlternatives = _currentConfig.maxAlternatives;
            request.config.profanityFilter = _currentConfig.isEnabledProfanityFilter;

            if (contexts != null)
            {
                request.config.speechContexts = new SpeechContext[contexts.Count];

                for (int i = 0; i < contexts.Count; i++)
                {
                    request.config.speechContexts[i] = new SpeechContext();
                    request.config.speechContexts[i].phrases = contexts[i];
                }
            }

            request.audio.content = content;

            return request;
        }

        private void CheckOperations()
        {
            lock (_lock)
            {
                if (OperationNames.Count > 0)
                {
                    if (_checkOperationTimer <= 0f)
                    {
                        var list = new List<OperationLongRecognizeResponse>();
                        foreach (var item in OperationNames)
                        {
                            if (item.Value == Enumerators.NetworkRequestStatus.WAITING_FOR_SEND)
                            {
                                GetOperation(item.Key.name);
                                list.Add(item.Key);
                               
                            }
                        }

                        if (list.Count > 0)
                        {
                            foreach (var item in list)
                                OperationNames[item] = Enumerators.NetworkRequestStatus.WAITING_FOR_REQUEST;
                            list.Clear();
                        }

                        _checkOperationTimer = _checkOperationDelay;
                    }

                    _checkOperationTimer -= Time.deltaTime;
                }
            }
        }
    }
}