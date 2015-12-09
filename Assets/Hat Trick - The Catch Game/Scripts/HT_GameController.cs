using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HT_GameController : MonoBehaviour {
	
	public Camera cam;
	public GameObject[] balls;
	public float timeLeft;
	public GUIText timerText;
	public GameObject gameOverText;
	public GameObject restartButton;
	public GameObject splashScreen;
	public GameObject startButton;
	public HT_HatController hatController;
	
	private float maxWidth;
	private bool counting;
	
	public HT_Score ht_score;
	
	public GameObject inputSetObj;
	
	public InputField inputField;
	
	public GameObject scoreBoardListObj;
	public List<ListItem> scoreBoardList = new List<ListItem>();
	
	public string host = "http://localhost:3000";
	
	// Use this for initialization
	void Start () {
		if (cam == null) {
			cam = Camera.main;
		}
		Vector3 upperCorner = new Vector3 (Screen.width, Screen.height, 0.0f);
		Vector3 targetWidth = cam.ScreenToWorldPoint (upperCorner);
		float ballWidth = balls[0].GetComponent<Renderer>().bounds.extents.x;
		maxWidth = targetWidth.x - ballWidth;
		timerText.text = "TIME LEFT:\n" + Mathf.RoundToInt (timeLeft);
	}

	void FixedUpdate () {
		if (counting) {
			timeLeft -= Time.deltaTime;
			if (timeLeft < 0) {
				timeLeft = 0;
			}
			timerText.text = "TIME LEFT:\n" + Mathf.RoundToInt (timeLeft);
		}
	}

	public void StartGame () {
		splashScreen.SetActive (false);
		startButton.SetActive (false);
		scoreBoardListObj.SetActive(false);
		inputSetObj.SetActive(false);
		hatController.ToggleControl (true);
		StartCoroutine (Spawn ());
	}

	public IEnumerator Spawn () {
		yield return new WaitForSeconds (2.0f);
		counting = true;
		while (timeLeft > 0) {
			GameObject ball = balls [Random.Range (0, balls.Length)];
			Vector3 spawnPosition = new Vector3 (
				transform.position.x + Random.Range (-maxWidth, maxWidth), 
				transform.position.y, 
				0.0f
			);
			Quaternion spawnRotation = Quaternion.identity;
			Instantiate (ball, spawnPosition, spawnRotation);
			yield return new WaitForSeconds (Random.Range (1.0f, 2.0f));
		}
		yield return new WaitForSeconds (2.0f);
		gameOverText.SetActive (true);
		yield return new WaitForSeconds (2.0f);
		
		//아이디 보유 여부  확인.
		if(userID == 0) 
		{
			inputSetObj.SetActive(true);
			scoreBoardListObj.SetActive(false);
		}
		else 
		{
			inputSetObj.SetActive(false);
			//스코어 업로드.
			SendScoreProcess();
			
		}
		
	}
	
	void ViewRankAndRestartButton()
	{
		inputSetObj.SetActive(false);
		scoreBoardListObj.SetActive(true);
		restartButton.SetActive (true);
	}
	
	int userID
	{
		get 
		{
			return PlayerPrefs.GetInt("userID");
		}
		
		set
		{
			PlayerPrefs.SetInt("userID", value);
		}
	}
	
	
	public void AddUser()
	{
		string url = string.Format("{0}/users/add/{1}", host, inputField.text);
		WWWForm temp = new WWWForm();
		temp.AddField("temp", "1");
		StartCoroutine( RequestServer(url, temp, CallbackAddUser));
	}
	
	void CallbackAddUser(string result) 
	{
		var dict = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
		long resultCode = (long)dict["result"];
		switch(resultCode) 
		{
			case 0:
				//정상 등록 완료.
				var bodyDict = (Dictionary<string, object>)dict["body"];
				long id = (long)bodyDict["id"];
				Debug.Log(id);
				userID = System.Convert.ToInt32(id);
				
				SendScoreProcess();
				break;
			case 99:
				//이미 등록된 아이디.
				Debug.Log((string)dict["body"]);
				break;
		}
	}
	
	void SendScoreProcess()
	{
		//점수 보낸다.
		//TODO : Score send 기능 제작 후 작동 가능.
		SendScoreToServer(()=>{
			//TODO : ranker 로딩 기능 제작 후 작동 가능.
			// GetTopRanker(ViewRankAndRestartButton);
			ViewRankAndRestartButton();
		});
	}
	
	void SendScoreToServer(System.Action callback = null)
	{
		string url = string.Format("{0}/score/add/{1}/{2}", host, userID, ht_score.score);
		WWWForm temp = new WWWForm();
		temp.AddField("temp", "1");
		StartCoroutine( RequestServer(url, temp, (string result)=>{
			if(callback != null) {
				callback();
			}
		}));
	}
	
	public void GetTopRanker(System.Action callback = null)
	{
		string url = string.Format("{0}/score/ranker", host);
		StartCoroutine( RequestServer(url, null, (string result)=>{
			var dict = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
			List<object> itemDatas = (List<object>)dict["Rank"];
			
			for(int i=0;i<itemDatas.Count;++i) {
				var item = (Dictionary<string, object>)itemDatas[i];
				scoreBoardList[i].SetItem(
					System.Convert.ToInt32(item["Rank"]), 
					(string)item["username"], 
					System.Convert.ToInt32(item["score"]));
			}
			if(callback != null) {
				callback();
			}
		}));
	}
	
	IEnumerator RequestServer(string URL, WWWForm form = null, System.Action<string> callback = null) 
	{
		WWW www = (form == null) ? new WWW(URL) : new WWW(URL, form);
		
		yield return www;
		
		if(callback != null) {
			Debug.Log(www.text);
			callback(www.text);
		}
	}
	
	public void ResetUserID()
	{
		userID = 0;
	}
}
