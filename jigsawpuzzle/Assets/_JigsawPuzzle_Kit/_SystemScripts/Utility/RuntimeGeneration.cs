//-----------------------------------------------------------------------------------------------------	
//  Simple demo script to help in puzzle-generation demonstration
//-----------------------------------------------------------------------------------------------------	
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class RuntimeGeneration : MonoBehaviour 
{
    public Texture2D image;                         // Will be used as main puzzle image
    public bool generateBackground = true;          // Automatically generate puzzle background from the source image
    public bool clearOldSaves = true;               // Clear existing Save data data during generation
    [TextArea]
    public string pathToImage;                      // pathToImage should starts from "http://"(for online image)  or  from "file://" (for local) 

    public PuzzleGenerator_Runtime puzzleGenerator;
	public GameController gameController;
	public Text rows;
	public Text cols;

    public Button shuffleButton;


    //============================================================================================================================================================
    public void GeneratePuzzle ()
	{
		if (puzzleGenerator == null || gameController == null) 
		{
			Debug.LogWarning ("Please assign <i>puzzleGenerator</i> and <i>gameController</i> to " + gameObject.name + " <b>RuntimeGenerator</b>", gameObject);
			return;
		}

        gameController.enabled = false;

        //Delete previously generated puzzle
        if (gameController.puzzle != null)
            Destroy(gameController.puzzle.gameObject);
        if (gameController.background != null)
            Destroy(gameController.background.gameObject);
        


        if (!image)
            puzzleGenerator.CreateFromExternalImage(pathToImage);               
        else
            gameController.puzzle = puzzleGenerator.CreatePuzzleFromImage(image);


        StartCoroutine(StartPuzzleWhenReady());
    }

    //-----------------------------------------------------------------------------------------------------
    IEnumerator StartPuzzleWhenReady()
    {
        while (puzzleGenerator.puzzle == null)
        {
            yield return null;
        }

        if (clearOldSaves)
        { 
           PlayerPrefs.DeleteKey(puzzleGenerator.puzzle.name);
           PlayerPrefs.DeleteKey(puzzleGenerator.puzzle.name + "_Positions");
        }

        gameController.puzzle = puzzleGenerator.puzzle;

        // Generate backround if needed
        if (generateBackground)
            gameController.background = puzzleGenerator.puzzle.GenerateBackground(puzzleGenerator.GetSourceImage(), true, (puzzleGenerator.anchoring == PuzzleAnchor.Center));

        gameController.enabled = true;

        if (shuffleButton)
            shuffleButton.GetComponent<Button>().onClick.AddListener(delegate { puzzleGenerator.puzzle.ShufflePuzzle(); });
    }

    //-----------------------------------------------------------------------------------------------------	
    public void SetRows (float _amount) 
	{
		if (puzzleGenerator != null)
			puzzleGenerator.rows = (int)_amount;

		if (rows != null)
			rows.text = ((int)_amount).ToString();		
	}

	//-----------------------------------------------------------------------------------------------------	
	public void SetCols (float _amount) 
	{
		if (puzzleGenerator != null)
			puzzleGenerator.cols = (int)_amount;

		if (cols != null)
			cols.text = ((int)_amount).ToString();
	}

	//-----------------------------------------------------------------------------------------------------	
}