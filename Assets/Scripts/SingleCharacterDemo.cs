using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssetManagerPackage;
using Assets.Scripts;
using IntegratedAuthoringTool;
using IntegratedAuthoringTool.DTOs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WellFormedNames;
using RolePlayCharacter;
using UnityEngine.SceneManagement;
using Utilities;
using System.Text;
using ActionLibrary;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;
using WorldModel;

public class SingleCharacterDemo : MonoBehaviour
{
    public struct ScenarioData
    {
        public readonly string ScenarioPath;
        public readonly string TTSFolder;
       
        private IntegratedAuthoringToolAsset _iat;
      
        

        public IntegratedAuthoringToolAsset IAT { get { return _iat; } }

        public ScenarioData(string path, string tts)
        {
            ScenarioPath = path;
            TTSFolder = tts;
          
            _iat = IntegratedAuthoringToolAsset.LoadFromFile(ScenarioPath);
        }
    }


    [Serializable]
    private struct BodyType
    {
        public string BodyName;
        public UnityBodyImplement CharaterArchtype;
    }

    [SerializeField]
    private Transform m_characterAnchor;

    [SerializeField]
    private DialogController m_dialogController;

    [SerializeField]
    private BodyType[] m_bodies;

    private Dictionary<string, float> keyValuePairs;

    private Dictionary<string, float> alternativeKeyValuePairs;

    [Space]
    [SerializeField]
    private Button m_dialogButtonArchetype = null;
    [SerializeField]
    private Transform m_dialogButtonZone = null;


    [SerializeField]
    private Transform m_scoreZone = null;

    private GameObject score;

    [Space]
    [SerializeField]
    [Range(1, 60)]
    private float m_agentProblemReminderRepeatTime = 3;

    [Space]
    [SerializeField]
    private RectTransform m_menuButtonHolder = null;
    [SerializeField]
    private Button m_menuButtonArchetype = null;

    public GameObject VersionMenu;
    public GameObject ScoreTextPrefab;
    public bool PJScenario;

    [Header("Intro")]
    [SerializeField]
    private GameObject _introPanel;
    [SerializeField]
    private Text _introText;

    private ScenarioData[] m_scenarios;
    private List<Button> m_currentMenuButtons = new List<Button>();
    private List<Button> m_buttonList = new List<Button>();
    private IntegratedAuthoringToolAsset _iat;
    private AgentControler _agentController;
    public GameObject _finalScore;
    public Dictionary<string,string> alreadyUsedDialogs;
    private bool Initialized;
    private bool waitingForReply;
    List<string> addedDialogs;
    private string previousState;
    private List<GameObject> _paperList;
    private List<string> currentOptions;
    private bool vr;
    public List<GameObject> cameras;
    private System.Random rand;

    private RolePlayCharacterAsset Player;


    private List<String> sentencesToMatch;
    public DateTime startingTime;

    StringBuilder MyStringBuilder;

    private Dictionary<string, string> _questionsAnswers;
    WaitForSeconds nextframe = new WaitForSeconds(0);

    private WorldModelAsset _wm;

    // Use this for initialization
    private void Start()


    {
        startingTime = DateTime.Now;

        sentencesToMatch = new List<string>();

            rand = new System.Random(10);
        if (UnityEngine.XR.XRDevice.isPresent == false)
        {
         //   Debug.Log("No VIVE detected");

            foreach (var cam in cameras)
            {

                if (cam.tag == "MainCamera")
                {
                    Debug.Log("no vr device detected");

                    SteamVR.SafeDispose();
                    UnityEngine.XR.XRSettings.enabled = false;

                    GameObject.Find("[CameraRig]").SetActive(false);
                    cam.SetActive(false);
                   
                    
                }
                if (cam.name == "2DCamera")
                {
                    cam.SetActive(true);
                    cam.tag = "MainCamera";
                }
            }

            vr = false;
        }
        else
        {
            vr = true;

            UnityEngine.XR.XRSettings.enabled = true;
            SteamVR.enabled = true;
            
        }

        Initialized = false;
      //  _finalScore = GameObject.FindGameObjectWithTag("FinalScore");
        _finalScore.SetActive(false);
        AssetManager.Instance.Bridge = new AssetManagerBridge();
        
        m_dialogController.AddDialogLine("Loading...");

        alreadyUsedDialogs = new Dictionary<string, string>();
        _questionsAnswers = new Dictionary<string, string>();

        var streamingAssetsPath = Application.streamingAssetsPath;
#if UNITY_EDITOR || UNITY_STANDALONE
        streamingAssetsPath = "file://" + streamingAssetsPath;
#endif

        var www = new WWW(streamingAssetsPath + "/scenarioList.txt");
        // yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            m_dialogController.AddDialogLine("Error: " + www.error);
           // yield break;
        }

        var entries = www.text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        if ((entries.Length % 2) != 0)
        {
            m_dialogController.AddDialogLine("Error: Scenario entries must in groups of 2, to identify the scenario file, and TTS directory");
         //   yield break;
        }

        {
            List<ScenarioData> data = new List<ScenarioData>();

            for (int i = 0; i < entries.Length; i += 2)
            {
                var path = entries[i].Trim();
                var tts = entries[i + 1].Trim();
                data.Add(new ScenarioData(path, tts));
            }

            m_scenarios = data.ToArray();
        }

        var unorganizedPaperList = GameObject.FindGameObjectsWithTag("DialogHistory").ToList();
        _paperList = unorganizedPaperList.OrderByDescending(p => p.transform.position.y).ToList();

        // _paperList = GameObject.FindGameObjectsWithTag("DialogHistory").ToList();
        currentOptions = new List<string>();
        keyValuePairs = new Dictionary<string, float>();
        alternativeKeyValuePairs = new Dictionary<string, float>();
        MyStringBuilder = new StringBuilder();
        m_dialogController.Clear();
        LoadScenarioMenu();
       

    }

    private void LoadScenarioMenu()
    {
        ClearButtons();
        addedDialogs = new List<string>();

        if(m_scenarios.Length == 1)
        {
            StartCoroutine(LoadScenario(m_scenarios.First()));
        }
        else
        foreach (var s in m_scenarios)
        {
            var data = s;
            AddButton(s.IAT.ScenarioName, () =>
            {
                StartCoroutine(LoadScenario(data));
            });
        }
    }

    private void AddButton(string label, UnityAction action)
    {
        var button = Instantiate(m_menuButtonArchetype);
        var t = button.transform;
        t.SetParent(m_menuButtonHolder);
        t.localScale = Vector3.one;
        button.image.color = new Color(0, 0, 0, 0);
        button.image.color = new Color(200, 200, 200, 0);

        var buttonLabel = button.GetComponentInChildren<Text>();
        buttonLabel.text = label;
        buttonLabel.color = Color.white;
        button.onClick.AddListener(action);
        m_currentMenuButtons.Add(button);
    }

    private void ClearButtons()
    {
        foreach (var b in m_currentMenuButtons)
        {
            Destroy(b.gameObject);
        }
        m_currentMenuButtons.Clear();
    }

    private IEnumerator LoadScenario(ScenarioData data)
    {
        ClearButtons();

        _iat = data.IAT;

        _introPanel.SetActive(true);
        _introText.text = string.Format("<b>{0}</b>\n\n\n{1}", _iat.ScenarioName, _iat.ScenarioDescription);
        previousState = "";
        var characterSources = _iat.GetAllCharacterSources().ToList();
        var addedDialogs = new List<string>();
        
        _wm = WorldModelAsset.LoadFromFile(_iat.GetWorldModelSource().Source);
        foreach (var source in characterSources)
        {
            
            var rpc = RolePlayCharacterAsset.LoadFromFile(source.Source);
            rpc.LoadAssociatedAssets();
            if (rpc.CharacterName.ToString() == "Player")
            {
                Player = rpc;
                _iat.BindToRegistry(Player.DynamicPropertiesRegistry);
                continue;
            }
            _iat.BindToRegistry(rpc.DynamicPropertiesRegistry);
            AddButton(characterSources.Count <= 2 ? "Start" : rpc.CharacterName.ToString(), 
                () =>
                {
                    Debug.Log("Interacted with the start button");
                   var body = m_bodies.FirstOrDefault(b => b.BodyName == rpc.BodyName);
                   _agentController = new AgentControler(data, rpc, _iat, body.CharaterArchtype, m_characterAnchor, m_dialogController);
                StopAllCoroutines();
                _agentController.storeFinalScore(_finalScore);
                _agentController.Start(this, VersionMenu);
                    _agentController.startingTime = this.startingTime;

                InstantiateScore();
            });
        }
        if (m_scenarios.Length > 1)
        {
           

            AddButton("Back to Scenario Selection Menu", () =>
            {
                _iat = null;
                LoadScenarioMenu();
            });
        }
        yield return nextframe;
       
    }

    public void SaveState()
    {
     //   _agentController.SaveOutput();
        WriteToFile();
    }

    private void UpdateButtonTexts(bool hide, IEnumerable<DialogueStateActionDTO> dialogOptions)
    {
        if (hide)
        {
            if (!m_buttonList.Any())
                return;
            foreach (var b in m_buttonList)
            {
                Destroy(b.gameObject);
            }
            m_buttonList.Clear();
        }
        else
        {
         //   Debug.Log("dialog options size" + dialogOptions.Count());
            if (dialogOptions.Count() < 6)
            {
                if (m_buttonList.Count == dialogOptions.Count())
                    return;

                foreach (var d in dialogOptions)
                {
                    if (isInButtonList(d.Utterance)) continue;
                    var b = Instantiate(m_dialogButtonArchetype);
                    var t = b.transform;
                    t.SetParent(m_dialogButtonZone, false);
                    b.GetComponentInChildren<Text>().text = d.Utterance;
                    currentOptions.Add(d.Utterance);
                    var id = d.Id;
                    b.onClick.AddListener((() => Reply(id)));
                    m_buttonList.Add(b);
                }

            }
        }
    }

    private void AddDialogButtons(IEnumerable<DialogueStateActionDTO> dialogOptions)
    {

        sentencesToMatch.Clear();

        if (m_buttonList.Count == dialogOptions.Count())
                return;

        int index = 0;
        foreach (var d in dialogOptions)
            {
              if (addedDialogs != null)
            //     if (addedDialogs.Contains(d.Utterance))
            //    {
                //   Debug.Log("contains the utterance" + d.Utterance);

            //      continue;
            // }
            //   else addedDialogs = new List<string>();

            //                else
            //              {
           if (isInButtonList(d.Utterance)) continue;
            var b = Instantiate(m_dialogButtonArchetype);
            var t = b.transform;
            t.SetParent(m_dialogButtonZone, false);
            index++;
            MyStringBuilder.Remove(0, MyStringBuilder.Length);
            b.GetComponentInChildren<Text>().text =     MyStringBuilder.Append(index).Append(". ").Append(d.Utterance).ToString();
            var id = d.Id;
            b.onClick.AddListener((() => Reply(id)));
            m_buttonList.Add(b);
            addedDialogs.Add(d.Utterance);

            sentencesToMatch.Add(d.Utterance);
             //   }
             
            }





    }

   public List<string> getCurrentDialogOptions()
    {

        return currentOptions;
    }




    public void ClearDialogOptions()
    {
        if (!m_buttonList.Any())
            return;
        foreach (var b in m_buttonList)
        {
            Destroy(b.gameObject);
        }
        m_buttonList.Clear();

        addedDialogs.Clear();
        currentOptions.Clear();
    }


    public void Reply(Guid dialogId)
    {
        ClearDialogOptions();
        var state = _agentController.RPC.GetBeliefValue("DialogState(Player)");
        if (state == IATConsts.TERMINAL_DIALOGUE_STATE)
        {
            return;
        }
        var reply = _iat.GetDialogActionById(dialogId);
        var actionFormat = string.Format("Speak({0},{1},{2},{3})", reply.CurrentState, reply.NextState, reply.Meaning, reply.Style);

        GameObject.FindObjectOfType<HeadLookController>().LookAtPlayer();

        //   StartCoroutine(PlayerReplyAction(actionFormat, reply.NextState));

        PlayerReplyAction(actionFormat, reply.NextState);

        UpdateScore(reply);

        alreadyUsedDialogs.Add(reply.Utterance,reply.UtteranceId);
     
            UpdateScore(reply);
        //    UpdateHistory(reply.Utterance);

        
    }

    private void PlayerReplyAction(string replyActionName, string nextState)
    {

        var events = new List<Name>();

        events.Add(EventHelper.ActionEnd(IATConsts.PLAYER, replyActionName, _agentController.RPC.CharacterName.ToString()));

        var effects = _wm.Simulate(events);
        foreach (var eff in effects )
        {

            var ef = eff.ToPropertyChangeEvent();

            if(eff.ObserverAgent == _agentController.RPC.CharacterName)
                _agentController.AddEvent(ef);
            else if(eff.ObserverAgent == Player.CharacterName)
            Player.Perceive(ef);
            else
            {
                _agentController.AddEvent(ef);
                Player.Perceive(ef);
            }
        }

        _agentController.canSpeak = true;
        waitingForReply = true;


      


    }

    // Update is called once per frame
    void Update()
    {
        if (_agentController == null)
            return;

        if (!_agentController.IsRunning)
            return;

        if (_agentController.getJustReplied())
        {
            var reply = _agentController.getReply();
            waitingForReply = false;
            Initialized = true;
            UpdateHistory(reply.Utterance);
            ClearDialogOptions();

            var cs = reply.CurrentState;
            var ns = reply.NextState;
            var m = reply.Meaning;
            var sty = reply.Style;

            List<Name> events = new List<Name>();

            events.Add(EventHelper.ActionEnd(_agentController.RPC.CharacterName.ToString(),
                "Speak(" + cs + "," + ns + ", " + m + "," + sty + ")", "Player"));
            events.Add(EventHelper.PropertyChange("DialogueState(" + _agentController.RPC.CharacterName + ")", ns,
                _agentController.RPC.CharacterName.ToString()));
       
            events.Add(EventHelper.PropertyChange("Has(Floor)", "Player",
                _agentController.RPC.CharacterName.ToString()));

            Player.Perceive(events);

        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (Time.timeScale > 0)
                Time.timeScale = 0;
            else
                Time.timeScale = 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[0].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[1].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[2].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[3].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[4].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[5].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[0].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[1].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[2].onClick.Invoke();

            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[3].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[4].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            if (!m_buttonList.IsEmpty())
            {
                m_buttonList[5].onClick.Invoke();

            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            this.SaveState();
        }
        if (_agentController.IsRunning)
        {
            _agentController.UpdateEmotionExpression();

            if (waitingForReply == false)
            {
                var action = Player.Decide().FirstOrDefault();

                if (action == null) return;

         //       Debug.Log("Action Key: " + action.Key);

                var currentState = action.Parameters[0].ToString();
              
                
            
                    var possibleOptions = _iat.GetDialogueActionsByState(currentState).ToList();
                    var originalPossibleActions = possibleOptions;


              
                    if (PJScenario)
                    {
                        if (!Initialized)
                        {
                            var newOptions =
                                possibleOptions.Shuffle(rand).Where(x => x.CurrentState == "Start").Take(3).ToList();


                            newOptions.AddRange(_iat.GetDialogueActionsByState("Introduction"));
                            possibleOptions = newOptions;
                        }
                        else
                        {

                            // var uhm = rand.Next(0, 10);


                            // Debug.Log(" rand " + uhm );


                            var newOptions = possibleOptions.Where(x => !alreadyUsedDialogs.ContainsKey(x.Utterance)).Shuffle(rand).Take(3).ToList();



                        var additionalOptions =
                            _iat.GetDialogueActionsByState("Start").Shuffle(rand).Take(2).ToList();


                            possibleOptions = newOptions.Concat(additionalOptions).ToList();


                            if (alreadyUsedDialogs.Count() > 12 && possibleOptions.Count() < 6)
                            {

                                var ClosureOptions = _iat.GetDialogueActionsByState("Closure").Shuffle(rand).Take(1).ToList();

                                possibleOptions = newOptions.Concat(additionalOptions).Concat(ClosureOptions).ToList();
                            }

                        }


                    }
                    //   UpdatePapers();

                    waitingForReply = true;
                    AddDialogButtons(possibleOptions);

                }
            


        }
    }
        

   
    private void InstantiateScore()
    {

        score = Instantiate(ScoreTextPrefab);
       
        var t = score.transform;
        t.SetParent(m_scoreZone, false);

        if (PJScenario)
        {
            var obj = GameObject.FindGameObjectWithTag("Score");
            obj.GetComponent<ScoreManager>().SetPJ(true);
            obj.GetComponent<ScoreManager>().Refresh();
          
        }
    }

    public void UpdateScore(DialogueStateActionDTO reply)
    {

     
        foreach (var meaning in reply.Meaning)
        {

            HandleKeywords("" + meaning);
        }

        foreach (var style in reply.Style)
        {

            HandleKeywords("" + style);
        }
    }

   


    private void HandleKeywords(string s)
    {

        char[] delimitedChars = { '(', ')' };

        string[] result = s.Split(delimitedChars);

    

        if (result.Length > 1)

            if (PJScenario)
            {
                switch (result[0])
                {
                    case "Aggression":
                        score.GetComponent<ScoreManager>().addAggression(Int32.Parse(result[1]));
                        break;

                    case "Information":
                        score.GetComponent<ScoreManager>().addInformation(Int32.Parse(result[1]));
                        break;

                    case "Truth":
                        score.GetComponent<ScoreManager>().addTruth(Int32.Parse(result[1]));
                        break;

                   }
            }
           else  switch (result[0])
            {
                case "Inquire":
                    score.GetComponent<ScoreManager>().AddI(Int32.Parse(result[1]));
                    break;

                case "FAQ":
                    score.GetComponent<ScoreManager>().AddF(Int32.Parse(result[1]));
                    break;

                case "Closure":
                    score.GetComponent<ScoreManager>().AddC(Int32.Parse(result[1]));
                    break;

                case "Empathy":
                    score.GetComponent<ScoreManager>().AddE(Int32.Parse(result[1]));
                    break;

                case "Polite":
                    score.GetComponent<ScoreManager>().AddP(Int32.Parse(result[1]));
                    break;


            }

    }

    public void UpdateHistory(string answer)
    {
        var question = alreadyUsedDialogs.LastOrDefault().Key;
        if(!_questionsAnswers.ContainsKey(question))
        _questionsAnswers.Add(question, answer);
        UpdatePapers();
    }

    public void UpdatePapers() { 
        string[] text = new string[4];
        
        int questionNumber = 0;
        var paperNumber = 0;
        foreach(var d in _questionsAnswers)
        {
            questionNumber += 1;
          
            text[paperNumber] += "Q" + questionNumber + ": " + d.Key + "\n" + "A" + questionNumber + ": " + d.Value + "\n" + "\n";
            if (questionNumber % 5 == 0)
                paperNumber += 1;


        }
        questionNumber = 0;
        var _paperIndex = 0;
        foreach (var d in text)
        {
            if (_paperList[_paperIndex] != null)
            {
                _paperList[_paperIndex].GetComponent<Text>().text = d;
               
            }
            else return;
         //   Debug.Log("wrote " + d + " on paper " + _paperIndex + "question number " + questionNumber);
            _paperIndex += 1;

        }

   

    }


    public void ClearScore()
    {

        Destroy(score);
    }

    public void End()
    {

       
       SceneManager.LoadScene(0);
     
    }

    public bool isInButtonList(string utterance)
    {
        foreach(var button in m_buttonList)
        {
            if (button.GetComponentInChildren<Text>().text.Contains(utterance))
            {
       //         Debug.Log("true phrase already exists");
                return true;
            }
            }
        //Debug.Log("false phrase does not exist");
        return false;

    }


    public void HandleSpeech(string mostCertain, List<string> alternatives)
    {
        bool clicked = false;
        foreach (var c in m_currentMenuButtons)
        {
            
            if (WordMatchingAlgorithm(c.GetComponentInChildren<Text>().text, mostCertain) > 0.5f && !clicked)
            {
                c.onClick.Invoke();
                clicked = true;
                return;
            }
        }


        if (clicked == false)
        {

            keyValuePairs.Clear();
            sentencesToMatch.Add("Can you please repeat that");
            foreach(var b in sentencesToMatch)
            {
                keyValuePairs.Add(b, MatchingAlgorithm(b, mostCertain));
            }


            var maxKeyforValue = keyValuePairs.FirstOrDefault(x => x.Value == keyValuePairs.Values.Max()).Key;

            Debug.Log("Most likely sentence: " + maxKeyforValue);

            if(maxKeyforValue == null)
            {


                alternativeKeyValuePairs.Clear();

                foreach (var b in sentencesToMatch)
                {
                
                    keyValuePairs.Add(b, MatchingAlgorithm(b, alternatives[0]));
                }

                maxKeyforValue = keyValuePairs.FirstOrDefault(x => x.Value == alternativeKeyValuePairs.Values.Max()).Key;

             //   Debug.Log("Most likely alternative sentence: " + maxKeyforValue);

            }

            if (maxKeyforValue != null)
                if (maxKeyforValue != "Can you please repeat that")
                    m_buttonList.Find(x => x.GetComponentInChildren<Text>().text.Contains(maxKeyforValue)).onClick.Invoke();
                else {
                    Debug.Log("Repeating");
                    var action = _agentController.lastAction;

                   _agentController.m_activeController.StartCoroutine(_agentController.HandleSpeak(action));


                }

        }

    }
    

    private float MatchingAlgorithm(string a, string b)
    {
        float result = 0.0f;



        var noSpaceA = a.Split(' ');
        var noSpaceB = b.Split(' ');


        var max = noSpaceA.Length;

        if(noSpaceB.Length > max)
            max = noSpaceB.Length;

    //    Debug.Log("Successful detection, getting Matching Rate " + a + " with " + b + " length " + noSpaceA.Length + " lengthb " + noSpaceB.Length);

        var matchingScore = 0.0f;

        max = max - 1;

        foreach(var wordA in noSpaceA)
        {
            foreach(var wordB in noSpaceB)
            {
                    matchingScore += WordMatchingAlgorithm(wordA, wordB);

            }
        }

      
        result = matchingScore / max;
   //     Debug.Log("Matching Score: " + a + " with " + b + " was: " + result);
        return result;
    }



    private float WordMatchingAlgorithm(string wordA, string wordB)
    {
        float result = 0.0f;



        var CharArrayA = wordA.ToCharArray();

        var CharArrayB = wordB.ToCharArray();


        float max = CharArrayA.Length;

        if (CharArrayB.Length > max)
            max = CharArrayB.Length;


        var matchingScore = 0.0f;

        max = max - 1;


        foreach (var charA in CharArrayA)
        {
            foreach (var charB in CharArrayB)
            {
                if (charA == charB)
                    matchingScore += 1.0f;
            }
        }

        if (max == 0)
            max = 1.0f;
        result = matchingScore / max;
        return result;
    }

    public void WriteToFile()
    {
        String myDocumentPath =Application.dataPath;

        var filteredTime = startingTime.Year + "-" + startingTime.Month + "-" + startingTime.Day + "-" +
                           startingTime.Hour + "-" + startingTime.Minute;

        System.IO.StreamWriter file2 = new System.IO.StreamWriter(myDocumentPath + "/Logs" + "/" + filteredTime +".txt");

        if(vr)
        file2.Write(" Using VR "  + " \n");

        if(_agentController != null)
            file2.Write(" EA Source: " + _agentController.RPC.EmotionalAppraisalAssetSource  + " \n");

       Debug.Log("Writing to file " + myDocumentPath);
        foreach(string i in  this.alreadyUsedDialogs.Keys)
        {
            file2.Write(i.ToString() + " \n");
            
        }
        file2.Close();
    }
}