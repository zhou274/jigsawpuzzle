//-----------------------------------------------------------------------------------------------------	
// Script controls whole gameplay, UI and all sounds
//-----------------------------------------------------------------------------------------------------	
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;

[AddComponentMenu("Scripts/Jigsaw Puzzle/Game Controller")]
public class GameController : MonoBehaviour 
{

	public Camera gameCamera;
	public PuzzleController puzzle;

    public bool findByTag = false;

    // Automatically align puzzle/camera with screen corners to fit puzzle with max filling
    public ScreenAnchor fitToScreenAnchor = ScreenAnchor.None;
    // Automatically align puzzle/background with camera center
    public bool alignWithCamera = false;             

    // Background (assembled puzzle preview)
    public Renderer background;
	/*public bool adjustBackground = true;*/
	public float backgroundTransparency = 0.1f;

	// Game UI
	public GameObject pauseUI;
	public GameObject winUI;
	public GameObject loseUI;
	public TextMeshProUGUI hintCounterUI;
	public TextMeshProUGUI timerCounterUI;
	public TextMeshProUGUI piecesLeftUI;
	public TextMeshProUGUI elapsedTimeUI;
	public Toggle musicToggleUI;
	public Toggle soundToggleUI;

	// Music-related
	public AudioSource musicPlayer; 
	public AudioClip musicMain;
	public AudioClip musicWin;
	public AudioClip musicLose;

	// Sound-related
	public AudioSource soundPlayer; 
	public AudioClip soundGrab;
	public AudioClip soundDrop;
	public AudioClip soundAssemble;

	// Game rules
	public float timer;					// Time limit for level
	public int hintLimit = -1;			// Hints limit for level
	public bool invertRules = false;	// Allows to invert basic rules - i.e. player should decompose  the images



	// Important internal variables - please don't change them blindly
	CameraController cameraScript;
	float timerTime = 20.0f;
	float remainingTime, elapsedTime;
	bool gameFinished = false;
    int remainingHints;
	Color backgroundColor;
	static Vector3 oldPointerPosition;

    public string clickid;
    private StarkAdManager starkAdManager;

    //=====================================================================================================
    // Initialize
    void OnEnable () 
	{
         // Prepare Camera
        if (!gameCamera) 
			gameCamera = Camera.main;
		
		gameCamera.orthographic = true;
		cameraScript = gameCamera.GetComponent<CameraController>();


		// Prepare AudioSources for soundPlayer and musicPlayer
		if (!soundPlayer   &&   (soundGrab  ||  soundDrop  ||  soundAssemble))
			soundPlayer = gameObject.AddComponent<AudioSource>();
		
		if (!musicPlayer   &&   (musicMain  ||  musicWin  ||  musicLose)) 
			musicPlayer = gameObject.AddComponent<AudioSource>();

        // Try to automatically find/assign puzzle and background by tags
        if (findByTag)
        {
            GameObject foundObject = GameObject.FindGameObjectWithTag("Puzzle_Main");
            if (foundObject)
                puzzle = foundObject.GetComponent<PuzzleController>();

            foundObject = GameObject.FindGameObjectWithTag("Puzzle_Background");
            if (foundObject)
                background = foundObject.GetComponent<Renderer>();
        }
        
        // Load saved data
        Load ();
		LoadAudioActivity();

        PlayMusic (musicMain, true); 

		// Prepare UI (disable all redudant at start)   
		if (winUI) 
			winUI.SetActive(false);
		
		if (loseUI)
			loseUI.SetActive(false);
		
		if (pauseUI) 
			pauseUI.SetActive(false);  
		
		if (timerCounterUI) 
			timerCounterUI.gameObject.SetActive(timer > 0);
		
		if (hintCounterUI) 
		{
            if (hintCounterUI.transform.parent)
                hintCounterUI.transform.parent.gameObject.SetActive(true);
           
            hintCounterUI.transform.gameObject.SetActive(remainingHints > 0);

            hintCounterUI.text = remainingHints.ToString();
        }


		// Init timer
		timerTime = Time.time + remainingTime;
		Time.timeScale = 1.0f;
		if (elapsedTimeUI)
			elapsedTimeUI.text = SecondsToTimeString(elapsedTime);


		Cursor.lockState = CursorLockMode.Confined;


        if (!puzzle)
        {
            this.enabled = false;
            return;
        }


        // Initiate puzzle and prepare background
        if (StartPuzzle(puzzle))
        {
            puzzle.SetPiecesActive(true);
            PrepareBackground(background);
        }


        // Align with Camera if needed
        if (alignWithCamera)
            puzzle.AlignWithCameraCenter(gameCamera, (puzzle.anchoring == PuzzleAnchor.Center), true);

        // Align with screen corners
        if (fitToScreenAnchor != ScreenAnchor.None)
        {
            puzzle.FitToScreen(gameCamera, fitToScreenAnchor);
            cameraScript.ReInit();
        }

    }

	//-----------------------------------------------------------------------------------------------------	
	// Main game cycle
	void Update () 
	{
		if (Input.GetKeyUp(KeyCode.Escape)) 
			Pause ();


		if (puzzle  &&  Time.timeScale > 0  &&  !gameFinished)
		{
			// Process puzzle and react on it state
            switch (puzzle.ProcessPuzzle (
                                            GetPointerPosition(gameCamera),
                                            Input.GetMouseButton(0)  &&  
											(!cameraScript || !cameraScript.IsCameraMoved())  
											&&  ((puzzle.GetCurrentPiece()==null 
											&& !EventSystem.current.IsPointerOverGameObject())  
											||  puzzle.GetCurrentPiece() != null),
                                            GetRotationDirection()
                                          ) )
			{
				case PuzzleState.None:
					;
					break;

				case PuzzleState.DragPiece:
					PlaySound(soundGrab);
					break;

				case PuzzleState.ReturnPiece:
					PlaySound(soundAssemble);
					break;

				case PuzzleState.DropPiece:
					PlaySound(soundDrop);
					break;

				// Hide all pieces and finish game - if whole puzzle Assembled 	
				case PuzzleState.PuzzleAssembled:
					if (background && !invertRules) 
						puzzle.SetPiecesActive(false); 
					
					if (winUI) 
						winUI.SetActive(true);
					
					PlayMusic(musicWin, false);
					gameFinished = true;
					break;	
			}


			ProcessTimer ();
			if (elapsedTimeUI) elapsedTimeUI.text = GetElapsedTime();
		}
		else  // Show background (assembled puzzle) if gameFinished
			if (gameFinished  &&  (!loseUI  ||  (loseUI && !loseUI.activeSelf)) ) 
				if(!invertRules) 
					ShowBackground();


        // Control Camera   
        if (cameraScript && puzzle)
           // if (puzzle.GetCurrentPiece() == null)  cameraScript.ManualUpdate();
            cameraScript.enabled = (puzzle.GetCurrentPiece() == null);


		if (piecesLeftUI) piecesLeftUI.text = puzzle.remainingPieces.ToString() + " / " + puzzle.pieces.Length.ToString();
	}

	//-----------------------------------------------------------------------------------------------------	 
	string GetElapsedTime()
	{
		elapsedTime = Mathf.Abs(timer - (timerTime - Time.time));

		return SecondsToTimeString(elapsedTime);
	}

	//-----------------------------------------------------------------------------------------------------	 
	string SecondsToTimeString(float _seconds)
	{
		elapsedTime = Mathf.Abs(timer - (timerTime - Time.time));

		float minutes_tmp = (int)(elapsedTime / 60);
		float hours_tmp = (int)(minutes_tmp / 60);
		minutes_tmp = (int)(minutes_tmp % 60);
		float seconds_tmp = (int)(elapsedTime % 60);
		seconds_tmp = (seconds_tmp == 60) ? 0 : seconds_tmp;

		return hours_tmp.ToString() + ":" + minutes_tmp.ToString() + ":" + seconds_tmp.ToString("00");
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Get current pointer(mouse or single touch) position  
	static Vector3 GetPointerPosition (Camera _camera) 
	{
		Vector3 pointerPosition = oldPointerPosition;

        // For mobile/desktop
        if (Input.touchCount > 0)  
           pointerPosition = oldPointerPosition = _camera.ScreenToWorldPoint(Input.GetTouch(0).position);
          else
           pointerPosition = oldPointerPosition = _camera.ScreenToWorldPoint(Input.mousePosition);


        return pointerPosition;
	}

    //-----------------------------------------------------------------------------------------------------	 
    // Get current rotation basing on mouse or touches
    float GetRotationDirection () 
	{
        float rotation = 0;

         // For Desktop - just set rotation to "clockwise" (don't change the permanent speed)
		#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
			if (Input.GetMouseButton(1))
                rotation = 1;
        #else // For mobile - calculate angle changing between touches and use it.
        if(puzzle.gradualRotation)
		   foreach(Touch touch in Input.touches) 
		   {
			 if (touch.tapCount == 2) 
				  rotation = 1;	
		   } 
		  else
			if (Input.touchCount > 1)  
				{
						// If there are two touches on the device... Store both touches.
						Touch touchZero = Input.GetTouch (0);
						Touch touchOne 	= Input.GetTouch (1);

						// Find the angle between positions.
						float currentAngle = Vector2.SignedAngle(touchZero.position, touchOne.position); 
						float previousAngle = Vector2.SignedAngle(touchZero.position - touchZero.deltaPosition, touchOne.position - touchOne.deltaPosition);

						rotation = currentAngle - previousAngle;
				}
                 //Alternative (sign/direction based):  // rotation = (int)Mathf.Sign(Vector2.SignedAngle(Vector2.up, Input.GetTouch(1).position-Input.GetTouch(0).position));
        #endif

        return rotation;
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Switch puzzle and background to another
	public void SwitchPuzzle (PuzzleController _puzzle, Renderer _background = null)
	{
		if (_puzzle  &&  _puzzle != puzzle) 
			StartPuzzle (_puzzle);
		
		if (_background  &&  _background != background) 
			PrepareBackground (_background);
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Prepare puzzle and Decompose it if needed
	public bool StartPuzzle (PuzzleController _puzzle)
	{
		if (!_puzzle) 
			_puzzle = gameObject.GetComponent<PuzzleController>();
		
		if (!_puzzle) 
		{
			Debug.LogWarning("<b>PuzzleKit::GameController:</b> <i>PuzzleController</i> should be assigned to <i>puzzle</i> property - please check " + gameObject.name, gameObject);  
			return false;
		}


        if (puzzle.pieces == null || puzzle.pieces.Length == 0)
            puzzle.Prepare();


        if (puzzle  &&  puzzle != _puzzle) 
			puzzle.gameObject.SetActive(false);

        puzzle = _puzzle;
		puzzle.gameObject.SetActive(true); 


		if (!PlayerPrefs.HasKey (puzzle.name + "_Positions")  ||  !puzzle.enablePositionSaving)
			if (!invertRules) 
				puzzle.DecomposePuzzle (); 
			else
				puzzle.NonrandomPuzzle ();


		puzzle.invertedRules = invertRules;

		gameFinished = false;

		return true;
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Show background (assembled puzzle)
	void ShowBackground () 
	{
		if (background  &&  backgroundColor.a < 1) 
		{
			backgroundColor.a = Mathf.Lerp (backgroundColor.a, 1.0f, Time.deltaTime); 
			background.material.color = backgroundColor;
		}

	}

	//-----------------------------------------------------------------------------------------------------	 
	// Prepare background (assembled puzzle)
	void PrepareBackground (Renderer _background = null) 
	{
        // Try to get BG from first puzzle child        
        if (!_background && puzzle)
        {
            Transform tmp = puzzle.thisTransform.GetChild(0);
            if (!_background && puzzle && tmp.tag == "Puzzle_Background")
                _background = tmp.GetComponent<Renderer>();
        }

        
        // Adjust background
        if (_background)
        {
           if (background)
                background.gameObject.SetActive(false);

            background = _background;
            background.gameObject.SetActive(true);

            backgroundColor = background.material.color;

            if (backgroundTransparency < 1.0f)
            {
                backgroundColor.a = backgroundTransparency;
                background.material.color = backgroundColor;
            }

           /* if (adjustBackground)
                AdjustBackground(); */

        }
		else 
			background = null;

	}

    //-----------------------------------------------------------------------------------------------------	
    // Automatically align camera with background
    public void AlignCameraWithBackground(float _sizeTuner = 0)
    {
        if (!cameraScript)   return;


        cameraScript.enabled = false;

        if (gameCamera.aspect > 1)
            gameCamera.orthographicSize = background.bounds.size.x / (gameCamera.aspect * 2) + _sizeTuner;
        else
            gameCamera.orthographicSize = background.bounds.size.x - gameCamera.aspect + _sizeTuner;

        cameraScript.ReturnCamera();
        cameraScript.SetInitialZoom(gameCamera.orthographicSize);
        cameraScript.enabled = true;
    }

    //-----------------------------------------------------------------------------------------------------	
    // Adjust background to puzzle
    public void AdjustBackground () 
	{
		if (background  /*&&  background.transform.parent != puzzle.transform*/)  
		{
            background.transform.parent = puzzle.transform;
            background.transform.SetAsFirstSibling();
            background.transform.localPosition = new Vector3(0, 0, 0.2f);


            // Try to adjust background size according to puzzle bounds
            if (background as SpriteRenderer)
			{
				// Temporarily reset Puzzle rotation 
				Quaternion tmpRotation = puzzle.transform.rotation;
				puzzle.transform.localRotation = Quaternion.identity;

				// Reset background transform
				background.transform.localRotation = Quaternion.identity;	
				background.transform.localScale = Vector3.one;


                // Calculate background scale  to make it the same size as puzzle
                background.transform.localScale = new Vector3(puzzle.puzzleBounds.size.x/background.bounds.size.x, puzzle.puzzleBounds.size.y/background.bounds.size.y, background.transform.localScale.z);	
				
                // Aligned background position
				//background.transform.position = new Vector3(puzzle.puzzleBounds.min.x, puzzle.puzzleBounds.max.y, background.transform.position.z);

                // Shift background if it's origin not in LeftTop corner 		 			 	
                if (Mathf.Abs(background.bounds.min.x - puzzle.puzzleBounds.min.x) > 1  ||  Mathf.Abs(background.bounds.max.y - puzzle.puzzleBounds.max.y) > 1)
					background.transform.localPosition = new Vector3(background.transform.localPosition.x - background.bounds.extents.x,  background.transform.localPosition.y + background.bounds.extents.y,  background.transform.localPosition.z);
                               
                // Return proprer puzzle rotation
                puzzle.transform.localRotation = tmpRotation;
			}

		}

	}

    //-----------------------------------------------------------------------------------------------------	 
    // Show Hint and update remainingHints
    public void ShowHint () 
	{
        //if (gameFinished  ||  remainingHints == 0)  return;
        // else
        ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    puzzle.ReturnPiece(-1);


                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
       
		
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Process Timer
	void ProcessTimer () 
	{
		if (timer > 0 && gameFinished == false)
			if (timerTime < Time.time)
			{ // Lose game if time is out
				PlayMusic(musicLose, false);

				if (loseUI)
					loseUI.SetActive(true);

				gameFinished = true;
			}
			else
			{				
				if (timerCounterUI)
				{
					float minutes_tmp = (int)(Mathf.Abs(Time.time - timerTime) / 60);
					float seconds_tmp = (int)(Mathf.Abs(Time.time - timerTime) % 60);

					seconds_tmp = (seconds_tmp == 60) ? 0 : seconds_tmp;

					timerCounterUI.text = minutes_tmp.ToString() + ":" + seconds_tmp.ToString("00");

				}
			}

    }

	//-----------------------------------------------------------------------------------------------------	 
	// Pause game and show pauseUI
	public void Pause () 
	{
        if (Time.timeScale > 0 )
		{
			Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            if (pauseUI) 
				pauseUI.SetActive(true);
		}
		else  
		{
			Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Confined;
            if (pauseUI) 
				pauseUI.SetActive(false);
		}

	}


    //-----------------------------------------------------------------------------------------------------	 
    // Reset current puzzle
    public void ResetPuzzle()
    {
        if (puzzle == null)
            return;

        Time.timeScale = 0;

        puzzle.ResetProgress(puzzle.name);

        remainingHints = hintLimit;
        timerTime = Time.time + timer;

        PlayerPrefs.SetInt(puzzle.name + "_hints", hintLimit);
        PlayerPrefs.SetFloat(puzzle.name + "_timer", timer);

        if (hintCounterUI)
        {
            hintCounterUI.gameObject.SetActive(true);
            hintCounterUI.text = remainingHints.ToString();
        }

        puzzle.DecomposePuzzle();      

        Time.timeScale = 1.0f;
    }


	//-----------------------------------------------------------------------------------------------------	 
	// Restart current puzzle
	public void RestartPuzzle()
	{
		if (puzzle != null)
		{
			PlayerPrefs.SetString(puzzle.name, "");
			PlayerPrefs.DeleteKey(puzzle.name + "_Positions");
		}

		if (background && !invertRules)
		{
			puzzle.SetPiecesActive(true);

			if (backgroundTransparency < 1.0f)
			{
				backgroundColor.a = backgroundTransparency;
				background.material.color = backgroundColor;
			}
		}

		if (winUI)
			winUI.SetActive(false);

		PlayMusic(musicMain, true);
		gameFinished = false;


		ResetPuzzle();
	}

	//-----------------------------------------------------------------------------------------------------	 
	// Restart current level
	public void Restart () 
	{
		Time.timeScale = 1.0f;

		if (puzzle != null) 
		{
			PlayerPrefs.SetString (puzzle.name, "");
			PlayerPrefs.DeleteKey (puzzle.name + "_Positions");
			PlayerPrefs.SetInt (puzzle.name + "_hints", hintLimit);
			PlayerPrefs.SetFloat (puzzle.name + "_timer", timer);
		}

		SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex);

	}

	//-----------------------------------------------------------------------------------------------------	 
	// Load custom level
	public void LoadLevel (int _levelId) 
	{
		Time.timeScale = 1.0f;
		SceneManager.LoadScene (_levelId);

	}

	//-----------------------------------------------------------------------------------------------------	
	// Load MusicPlayer and SoundPlayer Activity 
	void LoadAudioActivity () 
	{
		if (PlayerPrefs.HasKey("MusicPlayer")  &&  musicPlayer)  
		{
			musicPlayer.enabled = PlayerPrefs.GetInt("MusicPlayer") > 0 ? true : false;
			if (musicToggleUI) 
				musicToggleUI.isOn = musicPlayer.enabled;
		}


		if (PlayerPrefs.HasKey("SoundPlayer")  &&  soundPlayer)  
		{
			soundPlayer.enabled = PlayerPrefs.GetInt("SoundPlayer") > 0 ? true : false;
			if (soundToggleUI) 
				soundToggleUI.isOn = soundPlayer.enabled;
		}

	}

	//-----------------------------------------------------------------------------------------------------	
	// Quit the Application
	public void QuitApplication()
	{
		Application.Quit();
	}

	//-----------------------------------------------------------------------------------------------------	
	// Enable/disable music 
	public void SetMusicActive (bool _enabled) 
	{
		if (musicPlayer) 
		{
			musicPlayer.enabled = _enabled;
			if (musicToggleUI)  
				musicToggleUI.isOn = _enabled;
			
			PlayerPrefs.SetInt("MusicPlayer", _enabled ? 1 : 0);
			PlayMusic (musicMain, true);
		}

	}

	//-----------------------------------------------------------------------------------------------------	 
	// Enable/disable sounds 
	public void SetSoundActive (bool _enabled) 
	{
		if (soundPlayer) 
		{
			soundPlayer.enabled = _enabled;
			if (soundToggleUI)
				soundToggleUI.isOn = _enabled;
			
			PlayerPrefs.SetInt("SoundPlayer", _enabled ? 1 : 0);
		}

	}

	//-----------------------------------------------------------------------------------------------------	 
	// Change and Play music clip
	public void PlayMusic (AudioClip _music, bool _loop) 
	{
		if (musicPlayer  &&  musicPlayer.enabled  &&  _music)
		{
			musicPlayer.loop = _loop;
			musicPlayer.clip = _music;
			musicPlayer.Play();
		}

	}

	public void PlayMusic (AudioClip _music) 
	{
		if (musicPlayer  &&  musicPlayer.enabled  &&  _music)
		{
			musicPlayer.clip = _music;
			musicPlayer.Play();
		}

	}

	//-----------------------------------------------------------------------------------------------------	 
	// Play sound clip once
	public void PlaySound (AudioClip _sound) 
	{
		if (soundPlayer  &&  soundPlayer.enabled  &&  _sound) 
			soundPlayer.PlayOneShot(_sound);

	}	  

	//-----------------------------------------------------------------------------------------------------	
	// Save progress (Assembled pieces)
	public void Save ()
	{
		if (puzzle != null) 
		{
			puzzle.SaveProgress (puzzle.name);
			PlayerPrefs.SetInt (puzzle.name + "_hints", remainingHints);
			PlayerPrefs.SetFloat (puzzle.name + "_timer", timer - elapsedTime);
			PlayerPrefs.SetFloat(puzzle.name + "_elapsedTime", elapsedTime);
		}

	}

	//-----------------------------------------------------------------------------------------------------	
	// Load puzzle (Assembled pieces)
	public void Load ()
	{
		if (!puzzle)
			return;
		else
			puzzle.LoadProgress(puzzle.name); 


		if (PlayerPrefs.HasKey (puzzle.name + "_hints"))
		{
			remainingHints = PlayerPrefs.GetInt (puzzle.name + "_hints");
			if (hintCounterUI)
				hintCounterUI.text = remainingHints.ToString ();
		} 
		else
			{
				Debug.Log ("No saved data found for: " + puzzle.name + "_hints", gameObject);
				remainingHints = hintLimit;  
			}


		if (PlayerPrefs.HasKey(puzzle.name + "_timer"))
		{
			remainingTime = PlayerPrefs.GetFloat(puzzle.name + "_timer");
		}
		else
		{
			Debug.Log("No saved data found for: " + puzzle.name + "_timer", gameObject);
			remainingTime = timer;
		}

		if (PlayerPrefs.HasKey(puzzle.name + "_elapsedTime"))
			elapsedTime = PlayerPrefs.GetFloat(puzzle.name + "_elapsedTime");

	}  

	//-----------------------------------------------------------------------------------------------------	
	// Save progress if player closes the application
	public void OnApplicationQuit() 
	{
		Save ();
		PlayerPrefs.Save();
	}
    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }


    /// <summary>
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="closeCallBack"></param>
    /// <param name="errorCallBack"></param>
    public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
        }
    }
    //-----------------------------------------------------------------------------------------------------	
}