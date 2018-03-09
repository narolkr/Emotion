/**
* Copyright 2015 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;
using System.Collections.Generic;
using UnityEngine.UI;

using IBM.Watson.DeveloperCloud.Services.ToneAnalyzer.v3;
using IBM.Watson.DeveloperCloud.Connection;

using SimpleJSON;

public class Emotion : MonoBehaviour
{	
	private string _usernameSTT = "37233a24-d1df-4cb3-9b30-7782659087a5";
	private string _passwordSTT = "jrLSuoDAagg1";
	private string _urlSTT = "https://stream.watsonplatform.net/speech-to-text/api";

	public Text ResultsField;
	public Text topThree;

	private int _recordingRoutine = 0;
	private string _microphoneID = null;
	private AudioClip _recording = null;
	private int _recordingBufferSize = 1;
	private int _recordingHZ = 22050;

	private SpeechToText _speechToText;

	private string _usernameTA= "6e563bef-432e-4137-a31c-1d315ed1ecd3";
	private string _passwordTA = "pZVDVyc4J1Iy";
	private string _urlTA = "https://gateway.watsonplatform.net/tone-analyzer/api";

	private ToneAnalyzer _toneAnalyzer;
	private string _toneAnalyzerVersionDate = "2017-05-26";

	private bool _analyzeToneTested = false;

	public GameObject angry;
	public GameObject disgust;
	public GameObject fear;
	public GameObject joy;
	public GameObject sad;
	public GameObject analytic;
	public GameObject confident;
	public GameObject tentative;
	public GameObject finalEmotion;

	public Sprite[] emotionImages;

	Dictionary<string,float> dic = new Dictionary<string, float>();

	void Start()
	{
		angry.transform.localScale = new Vector3 (0.03f, 0.03f, 1f);
		disgust.transform.localScale = new Vector3 (0.03f, 0.03f, 1f);
		sad.transform.localScale = new Vector3 (0.03f, 0.03f, 1f);
		joy.transform.localScale = new Vector3 (0.03f, 0.03f, 1f);
		fear.transform.localScale = new Vector3 (0.03f, 0.03f, 1f);
		finalEmotion.transform.localScale = new Vector3 (0.05f, 0.05f, 1f);

		dic.Add ("Angry", 0f);
		dic.Add ("Disgust", 0f);
		dic.Add ("Joy", 0f);
		dic.Add ("Sad", 0f);
		dic.Add ("Fear", 0f);

		LogSystem.InstallDefaultReactors();

		//  Create credential and instantiate service
		Credentials credentialsSTT = new Credentials(_usernameSTT, _passwordSTT, _urlSTT);

		_speechToText = new SpeechToText(credentialsSTT);
		Active = true;

		Credentials credentialsTA = new Credentials(_usernameTA, _passwordTA, _urlTA);

		_toneAnalyzer = new ToneAnalyzer(credentialsTA);
		_toneAnalyzer.VersionDate = _toneAnalyzerVersionDate;

		StartRecording();
	}

	public bool Active
	{
		get { return _speechToText.IsListening; }
		set
		{
			if (value && !_speechToText.IsListening)
			{
				_speechToText.DetectSilence = true;
				_speechToText.EnableWordConfidence = true;
				_speechToText.EnableTimestamps = true;
				_speechToText.SilenceThreshold = 0.01f;
				_speechToText.MaxAlternatives = 0;
				_speechToText.EnableInterimResults = true;
				_speechToText.OnError = OnError;
				_speechToText.InactivityTimeout = -1;
				_speechToText.ProfanityFilter = false;
				_speechToText.SmartFormatting = true;
				_speechToText.SpeakerLabels = false;
				_speechToText.WordAlternativesThreshold = null;
				_speechToText.StartListening(OnRecognize, OnRecognizeSpeaker);
			}
			else if (!value && _speechToText.IsListening)
			{
				_speechToText.StopListening();
			}
		}
	}

	private void StartRecording()
	{
		if (_recordingRoutine == 0)
		{
			UnityObjectUtil.StartDestroyQueue();
			_recordingRoutine = Runnable.Run(RecordingHandler());
		}
	}

	private void StopRecording()
	{
		if (_recordingRoutine != 0)
		{
			Microphone.End(_microphoneID);
			Runnable.Stop(_recordingRoutine);
			_recordingRoutine = 0;
		}
	}

	private void OnError(string error)
	{
		Active = false;

		Log.Debug("ExampleStreaming.OnError()", "Error! {0}", error);
	}

	private IEnumerator RecordingHandler()
	{
		Log.Debug("ExampleStreaming.RecordingHandler()", "devices: {0}", Microphone.devices);
		_recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
		yield return null;      // let _recordingRoutine get set..

		if (_recording == null)
		{
			StopRecording();
			yield break;
		}

		bool bFirstBlock = true;
		int midPoint = _recording.samples / 2;
		float[] samples = null;

		while (_recordingRoutine != 0 && _recording != null)
		{
			int writePos = Microphone.GetPosition(_microphoneID);
			if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
			{
				Log.Error("ExampleStreaming.RecordingHandler()", "Microphone disconnected.");

				StopRecording();
				yield break;
			}

			if ((bFirstBlock && writePos >= midPoint)
				|| (!bFirstBlock && writePos < midPoint))
			{
				// front block is recorded, make a RecordClip and pass it onto our callback.
				samples = new float[midPoint];
				_recording.GetData(samples, bFirstBlock ? 0 : midPoint);

				AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
				record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
				record.Clip.SetData(samples, 0);

				_speechToText.OnListen(record);

				bFirstBlock = !bFirstBlock;
			}
			else
			{
				// calculate the number of samples remaining until we ready for a block of audio, 
				// and wait that amount of time it will take to record.
				int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
				float timeRemaining = (float)remaining / (float)_recordingHZ;

				yield return new WaitForSeconds(timeRemaining);
			}

		}

		yield break;
	}


	private void OnRecognize(SpeechRecognitionEvent result)
	{
		if (result != null && result.results.Length > 0)
		{
			foreach (var res in result.results)
			{
				foreach (var alt in res.alternatives)
				{
					string text = string.Format("{0} ({1}, {2:0.00})\n", alt.transcript, res.final ? "Final" : "Interim", alt.confidence);
					Log.Debug("ExampleStreaming.OnRecognize()", text);
					ResultsField.text = text;
					//tone
					Runnable.Run(Examples());
				}

				if (res.keywords_result != null && res.keywords_result.keyword != null)
				{
					foreach (var keyword in res.keywords_result.keyword)
					{
						Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
					}
				}

				if (res.word_alternatives != null)
				{
					foreach (var wordAlternative in res.word_alternatives)
					{
						Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
						foreach(var alternative in wordAlternative.alternatives)
							Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
					}
				}
			}
		}
	}

	private void OnRecognizeSpeaker(SpeakerRecognitionEvent result)
	{
		if (result != null)
		{
			foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
			{
				Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
			}
		}
	}

	//TONE

	private IEnumerator Examples()
	{		
		//_stringToTestTone = GetComponent<ExampleStreaming> ().ResultsField.text;
		//  Analyze tone
		if (!_toneAnalyzer.GetToneAnalyze(OnGetToneAnalyze, OnFail,ResultsField.text))
			Log.Debug("ExampleToneAnalyzer.Examples()", "Failed to analyze!");

		while (!_analyzeToneTested)
			yield return null;

		Log.Debug("ExampleToneAnalyzer.Examples()", "Tone analyzer examples complete.");
	}

	private void OnGetToneAnalyze(ToneAnalyzerResponse resp, Dictionary<string, object> customData)
	{
		
		var N = JSON.Parse (customData ["json"].ToString ());
		float angry_score= N["document_tone"]["tone_categories"][0]["tones"][0]["score"];
		float disgust_score= N["document_tone"]["tone_categories"][0]["tones"][1]["score"];
		float fear_score= N["document_tone"]["tone_categories"][0]["tones"][2]["score"];
		float joy_score= N["document_tone"]["tone_categories"][0]["tones"][3]["score"];
		float sad_score= N["document_tone"]["tone_categories"][0]["tones"][4]["score"];
		float analytic_score= N["document_tone"]["tone_categories"][1]["tones"][0]["score"];
		float confident_score= N["document_tone"]["tone_categories"][1]["tones"][1]["score"];
		float tentative_score= N["document_tone"]["tone_categories"][1]["tones"][2]["score"];

		Debug.Log(angry_score+" "+disgust_score+" "+fear_score+" "+joy_score+" "+sad_score+" "+analytic_score+" "+confident_score+" "+tentative_score);

		angry.transform.localScale = new Vector3 (Mathf.Lerp (0.03f, 0.05f, angry_score), Mathf.Lerp (0.03f, 0.05f, angry_score), 0f);
		disgust.transform.localScale = new Vector3 (Mathf.Lerp (0.03f, 0.05f, disgust_score), Mathf.Lerp (0.03f, 0.05f, disgust_score), 0f);
		joy.transform.localScale = new Vector3 (Mathf.Lerp (0.03f, 0.05f, joy_score), Mathf.Lerp (0.03f, 0.05f, joy_score), 0f);
		sad.transform.localScale = new Vector3 (Mathf.Lerp (0.03f, 0.05f, sad_score), Mathf.Lerp (0.03f, 0.05f, sad_score), 0f);
		fear.transform.localScale = new Vector3 (Mathf.Lerp (0.03f, 0.05f, fear_score), Mathf.Lerp (0.03f, 0.05f, fear_score), 0f);


		dic["Angry"] += Mathf.Lerp (0.03f, 0.05f, angry_score);
		dic["Disgust"] += Mathf.Lerp (0.03f, 0.05f, disgust_score);
		dic["Joy"] += Mathf.Lerp (0.03f, 0.05f, joy_score);
		dic["Sad"] += Mathf.Lerp (0.03f, 0.05f, sad_score);
		dic["Fear"] += Mathf.Lerp (0.03f, 0.05f, fear_score);

		float max1=0, max2=0, max3=0;
		string max1S=" ", max2S=" ", max3S=" ";

		foreach (float value in dic.Values) {
			if (value > max1) {
				max1 = value;
			}
			if (value > max2 && value < max1) {
				max2 = value;
			}
			if (value > max3 && value < max2) {
				max3 = value;
			}
		}

		if (max1== dic["Angry"]) 
		{
			finalEmotion.GetComponent<SpriteRenderer>().sprite =emotionImages[0];
			max1S = "Angry";
		}
		if (max1== dic["Disgust"]) 
		{
			finalEmotion.GetComponent<SpriteRenderer>().sprite =emotionImages[1];
			max1S = "Disgust";
		}
		if (max1== dic["Joy"]) 
		{
			finalEmotion.GetComponent<SpriteRenderer>().sprite =emotionImages[2];
			max1S = "Joy";
		}
		if (max1== dic["Sad"]) 
		{
			finalEmotion.GetComponent<SpriteRenderer>().sprite =emotionImages[3];
			max1S = "Sad";
		}
		if (max1== dic["Fear"]) 
		{
			finalEmotion.GetComponent<SpriteRenderer>().sprite =emotionImages[4];
			max1S = "Fear";
		}

		if (max2== dic["Angry"]) 
		{
			max2S = "Angry";
		}
		if (max2== dic["Disgust"]) 
		{
			max2S = "Disgust";
		}
		if (max2== dic["Joy"]) 
		{
			max2S = "Joy";
		}
		if (max2== dic["Sad"]) 
		{
			max2S = "Sad";
		}
		if (max2== dic["Fear"]) 
		{
			max2S = "Fear";
		}


		if (max3== dic["Angry"]) 
		{
			max3S = "Angry";
		}
		if (max3== dic["Disgust"]) 
		{
			max3S = "Disgust";
		}
		if (max3== dic["Joy"]) 
		{
			max3S = "Joy";
		}
		if (max3== dic["Sad"]) 
		{
			max3S = "Sad";
		}
		if (max3== dic["Fear"]) 
		{
			max3S = "Fear";
		}

		topThree.text = "Top 3 emotions: \n" + max1S + " : " + max1 + "\n" + max2S + " : " + max2 + "\n" + max3S + " : " + max3;
		_analyzeToneTested = true;
	}

	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
	{
		Log.Error("ExampleRetrieveAndRank.OnFail()", "Error received: {0}", error.ToString());
	}
}
