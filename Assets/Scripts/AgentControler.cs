using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ActionLibrary;
using AssetManagerPackage;
using IntegratedAuthoringTool;
using IntegratedAuthoringTool.DTOs;
using RolePlayCharacter;
using UnityEngine;
using Utilities;
using WellFormedNames;
using Object = UnityEngine.Object;

namespace Assets.Scripts
{
	public class AgentControler
	{
		private RolePlayCharacterAsset m_rpc;
		private DialogController m_dialogController;
		private IntegratedAuthoringToolAsset m_iat;
		private UnityBodyImplement _body;

		private List<Name> _events = new List<Name>();
		private string lastEmotionRPC;
		private float _previousMood;

		private float _moodThresold = 0.001f;
	    private GameObject _finalScore;
		private SingleCharacterDemo.ScenarioData m_scenarioData;
		public MonoBehaviour m_activeController;
		private GameObject m_versionMenu;
		private Coroutine _currentCoroutine = null;
	    private DialogueStateActionDTO reply;
	    private bool just_talked;

        public IAction lastAction;

        public bool canSpeak;

        public DateTime startingTime;
        

		public RolePlayCharacterAsset RPC { get { return m_rpc; } }

        WaitForSeconds nextframe = new WaitForSeconds(0);


        public AgentControler(SingleCharacterDemo.ScenarioData scenarioData, RolePlayCharacterAsset rpc,
			IntegratedAuthoringToolAsset iat, UnityBodyImplement archetype, Transform anchor, DialogController dialogCrt)
		{
			m_scenarioData = scenarioData;
            m_iat = iat;
            m_rpc = rpc;
            m_dialogController = dialogCrt;
			_body = GameObject.Instantiate(archetype);
		    just_talked = false;
            

            var t = _body.transform;
			t.SetParent(anchor, false);
			t.localPosition = Vector3.zero;
            t.localPosition = new Vector3(0.0f, -1.0f, 0.0f);
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			m_dialogController.SetCharacterLabel(m_rpc.CharacterName.ToString());
           
            m_rpc.Perceive(new Name[] { EventHelper.PropertyChange("DialogueState(Player)", "Start", "world")});
		}

		public void AddEvent(string eventName)
		{
			_events.Add((Name)eventName);
		}

	    public void AddEvent(Name eventName)
	    {
	        _events.Add(eventName);
	    }

		public void SetExpression(string emotion, float amount)
		{
			_body.SetExpression(emotion, amount);
		}

		public void SaveOutput()
		{
			const string datePattern = "dd-MM-yyyy-H-mm-ss";
			m_rpc.SaveToFile(Application.streamingAssetsPath + "\\Output\\" + m_rpc.CharacterName + "-" + DateTime.Now.ToString(datePattern) + ".ea");
            
		}

		public bool IsRunning
		{
			get { return _currentCoroutine != null; }
		}

		public void Start(MonoBehaviour controller, GameObject versionMenu)
		{
			m_activeController = controller;
			m_versionMenu = versionMenu;
			m_versionMenu.SetActive(false);
			_currentCoroutine = controller.StartCoroutine(UpdateCoroutine());
            canSpeak = false;

        }

		public void UpdateFields()
		{
			m_dialogController.UpdateFields(m_rpc);
		}

		public void UpdateEmotionExpression()
		{
			var emotion = m_rpc.GetStrongestActiveEmotion();
			if (emotion == null)
				return;
            if (emotion.EmotionType == "Shame")
            {
                
                var emot = new EmotionalAppraisal.DTOs.EmotionDTO();
                emot.Type = "Reproach";
                emot.Intensity = emotion.Intensity;
                emot.CauseEventId = emotion.CauseId;
                emot.CauseEventName = emotion.EventName.ToString();
                _body.SetExpression(emot.Type, emot.Intensity / 10f);
            }

            else
                _body.SetExpression(emotion.EmotionType, emotion.Intensity / 10f);
            }
        
                

			
		

		private IEnumerator UpdateCoroutine()
		{
			_events.Clear();
			while (m_rpc.GetBeliefValue(string.Format(IATConsts.DIALOGUE_STATE_PROPERTY,IATConsts.PLAYER)) != IATConsts.TERMINAL_DIALOGUE_STATE)
			{
				yield return new WaitForSeconds(0.1f);
                if (_events.Count == 0)
                {
                    m_rpc.Update();
                    continue;
                }
            


                m_rpc.Perceive(_events);

              

                var action = m_rpc.Decide().FirstOrDefault();

				_events.Clear(); 
				m_rpc.Update();

				if (action == null)
					continue;

				Debug.Log("Action Key: " + action.Key);

                if(canSpeak)
				switch (action.Key.ToString())
				{
					case "Speak":
						m_activeController.StartCoroutine(HandleSpeak(action));
                            canSpeak = false;
						break;
					case "Disconnect":
                        m_activeController.StartCoroutine(newHandleDisconnect());
                        m_dialogController.AddDialogLine(string.Format("- {0} disconnects -", m_rpc.CharacterName));

                        _currentCoroutine = null;
                        Object.Destroy(_body.Body);
                        break;
					default:
						Debug.LogWarning("Unknown action: " + action.Key);
						break;
				}
			}

		
		}
     
		public IEnumerator HandleSpeak(IAction speakAction)
		{
            lastAction = speakAction;
            Name currentState = speakAction.Parameters[0];
            Name nextState = speakAction.Parameters[1];
            Name meaning = speakAction.Parameters[2];
            Name style = speakAction.Parameters[3];

            m_rpc.SaveToFile(m_rpc.CharacterName + "-output" + ".rpc");

            var dialog = m_iat.GetDialogueActions(currentState, nextState, meaning, style).FirstOrDefault();
			if (dialog == null)
			{
				Debug.LogWarning("Unknown dialog action.");
				m_dialogController.AddDialogLine("... (unkown dialogue) ...");
			}
			else
			{
			


                string subFolder = m_scenarioData.TTSFolder;
                if (subFolder != "<none>")
                {
                    var provider = (AssetManager.Instance.Bridge as AssetManagerBridge)._provider;
                    var path = string.Format("/TTS-Dialogs/{0}/{1}/{2}", subFolder, m_rpc.VoiceName, dialog.UtteranceId);

                    AudioClip clip = null; //Resources.Load<AudioClip>(path);
                    string xml = null; //Resources.Load<TextAsset>(path);

                    var xmlPath = path + ".xml";
                    if (provider.FileExists(xmlPath))
                    {
                        try
                        {
                            using (var xmlStream = provider.LoadFile(xmlPath, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new StreamReader(xmlStream))
                                {
                                    xml = reader.ReadToEnd();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }

                        if (!string.IsNullOrEmpty(xml))
                        {
                            var wavPath = path + ".wav";
                            if (provider.FileExists(wavPath))
                            {
                                try
                                {
                                    using (var wavStream = provider.LoadFile(wavPath, FileMode.Open, FileAccess.Read))
                                    {
                                        var wav = new WavStreamReader(wavStream);

                                        clip = AudioClip.Create("tmp", (int)wav.SamplesLength, wav.NumOfChannels, (int)wav.SampleRate, false);
                                        clip.SetData(wav.GetRawSamples(), 0);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                    if (clip != null)
                                    {
                                        clip.UnloadAudioData();
                                        clip = null;
                                    }
                                }
                            }
                        }
                    }

                    if (clip != null && xml != null)
                    {
                        yield return _body.PlaySpeech(clip, xml);
                        clip.UnloadAudioData();
                    }
                    else
                    {
                        Debug.LogWarning("Could not found speech assets for a dialog");
                        yield return new WaitForSeconds(2);
                    }
                }
                else
                    yield return nextframe;

				if (nextState.ToString() != "-") //todo: replace with a constant
					AddEvent(string.Format("Event(Property-change,Suspect,DialogueState(Player),{0})", nextState));
			}

			if (speakAction.Parameters[1].ToString() != "-") //todo: replace with a constant
			{
				var dialogueStateUpdateEvent = string.Format("Event(Property-Change, Suspect ,DialogueState({0}),{1})", speakAction.Target, speakAction.Parameters[1]);
				AddEvent(dialogueStateUpdateEvent);
			}
		    if (nextState.ToString() == "Disconnect")
		    {
               
		        this.End();
		    }
           
		    m_rpc.Perceive(new Name[] { EventHelper.ActionEnd(m_rpc.CharacterName.ToString(), speakAction.Name.ToString(), IATConsts.PLAYER) });

            yield return new WaitForSeconds(0.1f);

            m_dialogController.AddDialogLine(dialog.Utterance);
            reply = dialog;
            just_talked = true;
        }

		private IEnumerator HandleDisconnectAction(IAction actionRpc)
		{
			yield return null;
			m_rpc.Perceive(new Name[] { EventHelper.ActionEnd(m_rpc.CharacterName.ToString(), actionRpc.Name.ToString(), IATConsts.PLAYER) });
            AddEvent(EventHelper.PropertyChange(string.Format(IATConsts.DIALOGUE_STATE_PROPERTY,IATConsts.PLAYER),"Disconnected", "SELF").ToString());
			if(_body)
				_body.Hide();
			yield return new WaitForSeconds(0.1f);
		    GameObject.Destroy(GameObject.FindGameObjectWithTag("Score"));
            _finalScore.SetActive(true);
            GameObject.FindGameObjectWithTag("FinalScoreText").GetComponent<FinalScoreScript>().FinalScore(RPC.Mood);
        }


	    private IEnumerator newHandleDisconnect()
	    {
            RPC.SaveToFile("New/RPClog");
           Debug.Log(" JUST SAVED rpc ");
            if (_body)
                _body.Hide();
            yield return new WaitForSeconds(2);
            GameObject.Destroy(GameObject.FindGameObjectWithTag("Score"));
            _finalScore.SetActive(true);
            GameObject.FindGameObjectWithTag("FinalScoreText").GetComponent<FinalScoreScript>().FinalScore(RPC.Mood);
            m_dialogController.Clear();
        }

	    public void End()
	    {

            String myDocumentPath = Application.dataPath;

            var filteredTime = startingTime.Year + "-" + startingTime.Month + "-" + startingTime.Day + "-" +
                               startingTime.Hour + "-" + startingTime.Minute;

            m_rpc.SaveToFile(myDocumentPath + "/Logs" + "/" + filteredTime + " - " + m_rpc.CharacterName + "-output" + ".rpc");
            
            if (_body)
                _body.Hide();

            GameObject.Destroy(GameObject.FindGameObjectWithTag("Score"));
            _finalScore.SetActive(true);
            GameObject.FindGameObjectWithTag("FinalScoreText").GetComponent<FinalScoreScript>().FinalScore(RPC.Mood);
            m_dialogController.Clear();

        }

	    public void storeFinalScore(GameObject g)
	    {

	        _finalScore = g;
         
	    }
        public DialogueStateActionDTO getReply()
        {
            just_talked = false;
            return reply;
        }
        public bool getJustReplied()
        {
            return just_talked;
        }
	}
}