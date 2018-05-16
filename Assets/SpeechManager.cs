using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SpeechManager : MonoBehaviour {

    private DictationRecognizer dictationRecognizer;
    private KeywordRecognizer kwr;
    private string grammarFileName;

    // Use this for initialization
    void Start () {
        /*Debug.Log("Started Speech Manager");
        List<string> phrases = new List<string>();
        phrases.Add("Peter Doe likes monkeys.");
        phrases.Add("I love dogs.");


        kwr = new KeywordRecognizer(phrases.ToArray());
        kwr.OnPhraseRecognized += OnPhraseRecognized;
        kwr.Start();

        Debug.Log("Started Recognition");
        Debug.Log(kwr.IsRunning);*/

        dictationRecognizer = new DictationRecognizer();
        
        dictationRecognizer.DictationResult += onDictationResult;
        dictationRecognizer.DictationHypothesis += onDictationHypothesis;
        dictationRecognizer.DictationComplete += onDictationComplete;
        dictationRecognizer.DictationError += onDictationError;

        dictationRecognizer.Start();
    }


    void onDictationResult(string text, ConfidenceLevel confidence)
    {
        // write your logic here
        Debug.LogFormat("Dictation result: " + text);
    }

    void onDictationHypothesis(string text)
    {
        // write your logic here
        Debug.LogFormat("Dictation hypothesis: {0}", text);
    }

    void onDictationComplete(DictationCompletionCause cause)
    {
        // write your logic here
        if (cause != DictationCompletionCause.Complete)
            Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", cause);
    }

    void onDictationError(string error, int hresult)
    {
        // write your logic here
        Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
    }

    public void ReplaceStringToRecognition(List<string> stringsToRecognize)
    {
        string grammarText = "";
        foreach(string toRec in stringsToRecognize)
        {
            grammarText += toRec + System.Environment.NewLine;
        }

        ChangeGrammarTempFileContent(grammarText);
    }


    private string CreateGrammarTempFile()
    {
        string filename ="";

        try
        {
            filename = Path.GetTempFileName();
            FileInfo finfo = new FileInfo(filename);
            finfo.Attributes = FileAttributes.Temporary;
        }catch(Exception ex)
        {
            Debug.LogError(ex);
        }

        return filename;
    }

    private void ChangeGrammarTempFileContent(string content)
    {
        try
        {
            // Write to the temp file.
            StreamWriter streamWriter = File.AppendText(grammarFileName);
            streamWriter.WriteLine(content);
            streamWriter.Flush();
            streamWriter.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing to TEMP file: " + ex.Message);
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log("Keyword: " + args.text + "; Confidence: " + args.confidence + "; Start Time: " + args.phraseStartTime + "; Duration: " + args.phraseDuration);
    }

    // Update is called once per frame
    void Update () {
        
    }
}
