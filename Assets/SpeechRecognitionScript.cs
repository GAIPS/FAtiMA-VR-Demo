using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;

public class SpeechRecognitionScript : MonoBehaviour {

    private GCSpeechRecognition _speechRecognition;

    private Button _startRecordButton,
                   _stopRecordButton;

    private Image _speechRecognitionState;

    private Text _speechRecognitionResult;

    private Toggle _isRuntimeDetectionToggle;

    private Dropdown _languageDropdown;

    private InputField _contextPhrases;

    private string speechState = "notRecording";

    public float fetchingResultsTimer;

    private float realTimer;

    private void Start()
    {
        _speechRecognition = GCSpeechRecognition.Instance;
        _speechRecognition.RecognitionSuccessEvent += RecognitionSuccessEventHandler;
        _speechRecognition.NetworkRequestFailedEvent += SpeechRecognizedFailedEventHandler;
        _speechRecognition.LongRecognitionSuccessEvent += LongRecognitionSuccessEventHandler;

        _startRecordButton = GameObject.Find("Button_StartRecord").GetComponent<Button>();
  //      _stopRecordButton = GameObject.Find("Button_StopRecord").GetComponent<Button>();

    //    _speechRecognitionState = GameObject.Find("Image_RecordState").GetComponent<Image>();

        _speechRecognitionResult = GameObject.Find("Text_Result").GetComponent<Text>();

        //   _isRuntimeDetectionToggle = GameObject.Find("Canvas/Toggle_IsRuntime").GetComponent<Toggle>();

        //   _languageDropdown = transform.Find("Canvas/Dropdown_Language").GetComponent<Dropdown>();

        //        _contextPhrases = transform.Find("Canvas/InputField_SpeechContext").GetComponent<InputField>();

     
        _startRecordButton.onClick.AddListener(StartRecordButtonOnClickHandler);
    //    _stopRecordButton.onClick.AddListener(StopRecordButtonOnClickHandler);

        _speechRecognitionState.color = Color.white;
        _startRecordButton.interactable = true;
        // _stopRecordButton.interactable = false;

        realTimer = fetchingResultsTimer;

    
    }

    private void OnDestroy()
    {
        _speechRecognition.RecognitionSuccessEvent -= RecognitionSuccessEventHandler;
        _speechRecognition.NetworkRequestFailedEvent -= SpeechRecognizedFailedEventHandler;
        _speechRecognition.LongRecognitionSuccessEvent -= LongRecognitionSuccessEventHandler;
    }



    public void StartRecordButtonOnClickHandler()
    {

        if (speechState == "notRecording")
        {

            //     _startRecordButton.interactable = false;

            _startRecordButton.GetComponent<Image>().color = Color.red;

            _speechRecognitionResult.text = string.Empty;

            _speechRecognition.StartRecord(true);

            speechState = "Recording";

            _startRecordButton.GetComponentInChildren<Text>().text = speechState;


        }

        else
        {

            _startRecordButton.GetComponent<Image>().color = Color.yellow;
            _speechRecognition.StopRecord();

            _startRecordButton.GetComponentInChildren<Text>().text = "Fetching results";
            speechState = "FetchingResults";

            realTimer = fetchingResultsTimer;
        }

        
    }


    void Update()
    {
        realTimer -= Time.deltaTime;

        if((realTimer < 0.0f) && (speechState == "FetchingResults"))
        {
            _startRecordButton.GetComponentInChildren<Text>().text = "Start Recording";
            speechState = "notRecording";
            _startRecordButton.GetComponent<Image>().color = Color.white;

        }
    }

    public void StopRecordButtonOnClickHandler()
    {
    //    ApplySpeechContextPhrases();

        _stopRecordButton.interactable = false;
        _speechRecognitionState.color = Color.yellow;
        _speechRecognition.StopRecord();
        _startRecordButton.interactable = true;
    }

    private void LanguageDropdownOnValueChanged(int value)
    {
        _speechRecognition.SetLanguage((Enumerators.LanguageCode)value);
    }

    private void ApplySpeechContextPhrases()
    {
        string[] phrases = _contextPhrases.text.Trim().Split(","[0]);

        if (phrases.Length > 0)
            _speechRecognition.SetContext(new List<string[]>() { phrases });
    }

    private void SpeechRecognizedFailedEventHandler(string obj, long requestIndex)
    {

        _speechRecognitionResult.text = "Speech Recognition failed with error: " + obj;
        Debug.Log("Network Request Failed " + obj.ToString());
     /*   _startRecordButton.interactable = true;
      
            _startRecordButton.GetComponentInChildren<Text>().text = "Start Recording";
            speechState = "notRecording";

            _startRecordButton.GetComponent<Image>().color = Color.white;*/
        }
    

    private void RecognitionSuccessEventHandler(RecognitionResponse obj, long requestIndex)
    {
   /*     if (true)
        {
            _startRecordButton.interactable = true;

            _startRecordButton.GetComponentInChildren<Text>().text = "Start Recording";
            speechState = "notRecording";

            _startRecordButton.GetComponent<Image>().color = Color.white;

        }
        */
        if (obj != null && obj.results.Length > 0)
        {
            Debug.Log("Speech successful");
            _speechRecognitionResult.text = obj.results[0].alternatives[0].transcript;

            List<String> otherS = new List<string>(); 

            foreach (var result in obj.results)
            {
                foreach (var alternative in result.alternatives)
                {
                    otherS.Add(alternative.transcript);
                }
            }

            GameObject.Find("Manager").GetComponent<SingleCharacterDemo>().HandleSpeech(_speechRecognitionResult.text, otherS);

          //  _speechRecognitionResult.text += other;
        }
        else
        {
            Debug.Log("No words detected");
            _speechRecognitionResult.text = "Speech Recognition succeeded! Words are no detected.";
        }
    }

    private void LongRecognitionSuccessEventHandler(OperationResponse operation, long index)
    {
        if (!_isRuntimeDetectionToggle.isOn)
        {
    //        _startRecordButton.interactable = true;
      //      _speechRecognitionState.color = Color.green;

        //    _startRecordButton.GetComponentInChildren<Text>().text = "Start Recording";
//            speechState = "notRecording";

  //          _startRecordButton.GetComponent<Image>().color = Color.white;

        }

        if (operation != null && operation.response.results.Length > 0)
        {
            Debug.Log("Speech successful");
            _speechRecognitionResult.text = operation.response.results[0].alternatives[0].transcript;



            List<String> otherS = new List<string>();

            foreach (var result in operation.response.results)
            {
                foreach (var alternative in result.alternatives)
                {
                    otherS.Add(alternative.transcript);
                }
            }

            GameObject.Find("Manager").GetComponent<SingleCharacterDemo>().HandleSpeech(_speechRecognitionResult.text, otherS);
        }        else
        {

            Debug.Log("No words detected");

            _speechRecognitionResult.text = "Speech Recognition succeeded! Words are no detected.";
        }
    }

}
