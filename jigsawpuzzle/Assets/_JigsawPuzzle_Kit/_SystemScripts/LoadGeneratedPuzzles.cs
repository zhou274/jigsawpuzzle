//-----------------------------------------------------------------------------------------------------	
// Script allows to load differently generated puzzles and a simple UI for this
//-----------------------------------------------------------------------------------------------------	
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class LoadGeneratedPuzzles : MonoBehaviour {

	public RectTransform generationUI;
	public RuntimeGeneration generationProcessor;

	public Vector2 maxPuzzleSize = new Vector2(50, 50);  

	public RectTransform buttonTemplate;
	public ScrollRect scroll;
	public Sprite[] sourceImages;


	List<Vector2> loadDatas = new List<Vector2>();


	//-----------------------------------------------------------------------------------------------------
	void Start () 
	{
		//PlayerPrefs.DeleteAll();
		if (generationUI)
			generationUI.gameObject.SetActive(false);
		else
			Debug.LogWarning("Please assign generationUI", gameObject);

		if (!generationProcessor)
			Debug.LogWarning("Please assign runtimeGeneration", gameObject);

		CheckSaves();
	}

	//-----------------------------------------------------------------------------------------------------
	void CheckSaves () 
	{		
		string puzzleName;

		foreach (Sprite image in sourceImages)
			for (int x = 2; x < maxPuzzleSize.x; x++)
				for (int y = 2; y < maxPuzzleSize.y; y++)
				{
					puzzleName = "Puzzle_" + image.name + "_" + x.ToString() + "x" + y.ToString();

										if (PlayerPrefs.HasKey(puzzleName))			
						GenerateButton(image, PlayerPrefs.GetInt(puzzleName + "_X"), PlayerPrefs.GetInt(puzzleName + "_Y"));
				}
		
	}

	//-----------------------------------------------------------------------------------------------------
	public void GenerateButton(Sprite _image, int _puzzleSizeX = 0, int _puzzleSizeY = 0)
	{
		GameObject go = Instantiate(buttonTemplate.gameObject);
		go.name = loadDatas.Count.ToString();
		go.transform.SetParent(scroll.content.transform);

		Button button = go.GetComponent<UnityEngine.UI.Button>();
		button.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, - buttonTemplate.sizeDelta.y * (loadDatas.Count + 1.1f),0);
		button.image.sprite = _image;
		button.GetComponentInChildren<Text>().text = _image.name + " " + _puzzleSizeX.ToString() + "x" + _puzzleSizeY.ToString();

		button.onClick.AddListener(() => LoadGenerated(button.name));	
		loadDatas.Add(new Vector2(_puzzleSizeX, _puzzleSizeY));
	}

	//-----------------------------------------------------------------------------------------------------
	public void PrepareNewGeneration(Sprite _image)
	{
		Cleanup("Puzzle_" + _image.name);
	}

	//-----------------------------------------------------------------------------------------------------
	public void Cleanup(string puzzleName)
	{
		if (PlayerPrefs.HasKey(puzzleName))
		{
			PlayerPrefs.DeleteKey(puzzleName);
			PlayerPrefs.DeleteKey(puzzleName + "_Positions");
			PlayerPrefs.DeleteKey(puzzleName + "_X");
			PlayerPrefs.DeleteKey(puzzleName + "_Y");
		}
	}

	public void CleanupCurrent()
	{
		Cleanup(generationProcessor.image.name);
		Debug.Log(PlayerPrefs.HasKey(generationProcessor.image.name));
	}

	//-----------------------------------------------------------------------------------------------------
	public void LoadGenerated(string _name)
	{
		generationProcessor.puzzleGenerator.cols = (int)loadDatas[int.Parse(_name)].x;
		generationProcessor.puzzleGenerator.rows = (int)loadDatas[int.Parse(_name)].y;

		generationProcessor.GeneratePuzzle();

		generationUI.gameObject.SetActive(false);
		scroll.gameObject.SetActive(false);
	}

	//-----------------------------------------------------------------------------------------------------
}