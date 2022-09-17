using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour
{
    //These are defined in the Unity Inspector tab
    public Tile empty;
    public Tilemap map;
    public int gameBoardSize;
    public Cell customCell;
    public Cell[,] gameBoard;
    public Cell[,] queue;
    [SerializeField]
    public List<Cell> cellTypes  = new List<Cell>();


    //Custom cell varibles
    string customUnderPop="2";
    string customOverPop = "3";
    string customReproducePop = "3";
    // Start is called before the first frame update
    void Start()
    {
    //Create and empty board and queue
        gameBoard = new Cell[gameBoardSize,gameBoardSize];
        queue = new Cell[gameBoardSize,gameBoardSize];
        //Paint the tilemap
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y = 0; y < gameBoardSize; y++)
            {
                gameBoard[x,y] = null;
                map.SetTile(new Vector3Int(x, y), empty);
                map.RefreshTile(new Vector3Int(x, y));       
            }
        }
        foreach(Cell cell in cellTypes)
        {
            cell.Setup();
        }
        customCell = (Cell)ScriptableObject.CreateInstance(typeof(Cell));
        customCell.id = "CustomCell";
        customCell.CellColor = new Color(16, 255, 0, 255);
        customCell.baseSprite = Cell.sprite;
        customCell.Setup();
        selectedCell = cellTypes[0];
    }
    public void Reset()
    {
        queue = new Cell[gameBoardSize, gameBoardSize];
        gameBoard = new Cell[gameBoardSize, gameBoardSize];
        UpdateBoard();
    }
    
    void OnGUI()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Rect rect = new Rect(new Vector2(0, 0), new Vector2(screenSize.x / 4, screenSize.y));
        if(GUILayout.Button("Reset (R)"))
        {
            Reset();
        }
        if(GUILayout.Button(playing?"Pause (Space)":"Unpause (Space)"))
        {
            playing = !playing;
        }
        if (GUILayout.Button("Save"))
        {
            Save();
        }
        if (GUILayout.Button("Load"))
        {
            Load();
        }
        GUILayout.TextArea($"Cell Count: {cellCount}");
        GUILayout.TextArea($"Sim speed: {speed}");
        foreach(Cell cell in cellTypes)
        {
            GUILayout.TextArea($"{cell.id} ({cell.hotKey}){(selectedCell==cell?"(Current)":"")}");
        }
        GUILayout.TextArea($"Custom cell (C): {(selectedCell == customCell ? "(Current)" : "")}");
        GUILayout.TextArea("Underpopulated at:");
        customUnderPop = GUILayout.TextField(customUnderPop);
        GUILayout.TextArea("Overpopulated at:");
        customOverPop = GUILayout.TextField(customOverPop);
        GUILayout.TextArea("Reproduces at:");
        customReproducePop = GUILayout.TextField(customReproducePop);
        if(GUILayout.Button($"Is Carnivore: {customCell.carnivoreCell}"))
        {
            customCell.carnivoreCell = !customCell.carnivoreCell;
        }
        if(customReproducePop!=""&&customReproducePop!=" "&&customReproducePop!=null)
            customCell.requiredNeighborsForReproduction = int.Parse(customReproducePop);
        if (customOverPop != "" && customOverPop != " " && customOverPop != null)
            customCell.requiredNeighborsForOverPopDeath = int.Parse(customOverPop);
        if (customUnderPop != "" && customUnderPop != " " && customUnderPop != null)
            customCell.requiredNeighborsForUnderPopDeath = int.Parse(customUnderPop);
    }

    //These are simply used to determine if and how fast the simulation is running.
    bool playing = false;
    float speed=100;
    float tick = 0;
    //Cell tracking
    int cellCount;
    public Cell selectedCell;
    // Update is called once per frame
    void Update()
    {
    //Inputs should become a switch case later.
    //Pause/unpause simulation
        if (Input.GetKeyDown(KeyCode.Space))
            playing = !playing;
        if (playing)
        {
        //Do simulation everye 100 ticks
            if (tick >= 100)
            {
                tick = 0;
                PrepareQueue();
                UpdateBoard();
            }
            //Tick increments by the speed varible
            tick += speed;
        }
        
        //adjust simulation speed
        if (Input.GetKey(KeyCode.Equals))
        {
            speed += 0.5f;
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            speed -= 0.5f;
        }
        //Clamp the speed so it never stops, and never goes too fast.
        speed = Mathf.Clamp(speed, 1, 100);

        //reset board if needed
        if (Input.GetKey(KeyCode.R))
        {
            Reset();
        }

        if (Input.GetKeyDown(KeyCode.C))
            selectedCell = customCell;

        foreach (Cell cell in cellTypes)
        {
            if (Input.GetKeyDown(cell.hotKey))
            {
                selectedCell = cell;
                break;
            }
        }
        //"Paint" cells with left mouse button
        if (Input.GetKey(KeyCode.Mouse1))
        {
            //Get the position of the mouse cursor in the game world
            Vector3 pos= Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //Convert it to a Vector3Int for use with our tilemap and 2D arrays
            Vector3Int castedPost = new Vector3Int((int)pos.x, (int)pos.y, 0);
            if (castedPost.x > 0 && castedPost.y > 0 && castedPost.x < gameBoardSize && castedPost.y < gameBoardSize)
            {
                //Set cell values at position
                gameBoard[castedPost.x, castedPost.y] =selectedCell;
                map.SetTile(castedPost, selectedCell.GetTile(0));
                map.RefreshTile(castedPost);
            }
        }
        
    }
    /// <summary>
    /// Calculate the new tiles.
    /// </summary>
    private void PrepareQueue()
    {
        
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y = 0; y < gameBoardSize; y++)
            {
                Cell cell = gameBoard[x, y];
                Vector2Int pos = new Vector2Int(x, y);
                Cell[] cells = new Cell[cellTypes.Count+1];
                cellTypes.CopyTo(cells);
                cells[cells.Length-1]=customCell;
                //Check if we should place a cell.
                foreach (Cell cType in cells)
                {
                    if (cType.ShouldBePlacedHere(gameBoard, pos, gameBoardSize,out bool eats))
                    {
                        queue[x, y] = cType;
                    }
                    if (eats)
                        continue;
                }

                if (cell == null)
                    continue;
               //Check if the cell should die.
                if(cell.Dies(gameBoard,pos, gameBoardSize))
                {
                    queue[x, y] = null;
                }
                else
                {
                    queue[x,y]= cell;
                }
            }
        }
    }

    public string SavePath
    {
        get
        {
            return $"{Application.persistentDataPath}/Save.xml";
        }
    }

    public void Save()
    {
        XmlDocument save = EncodedSave();
        save.Save(SavePath);
    }

    public void Load()
    {
        Reset();
        XmlDocument save = new XmlDocument();
        save.Load(SavePath);
        DecodeSave(save);
    //    UpdateBoard();
    }

    public void DecodeSave(XmlDocument document)
    {
        //Reset the board
        Reset();
        //Load the cells from each node
        foreach(XmlNode child in document.DocumentElement.ChildNodes)
        {
            string type = child.Name;
            Cell selected = cellTypes.Find(x => x.id == type);
            if (selected == null && type == "CustomCell")
                selected = customCell;
            string val = child.InnerText;
            //Remove ()
           
            //Get numbers out
            string[] values =val.Split(',');
            //Parse
            int x = int.Parse(values[0]);
            int y = int.Parse(values[1]);

            //Set val
            gameBoard[x, y] = selected;
            map.SetTile(new Vector3Int(x, y, 0), selected.GetTile(0));
        }
    }

    public XmlDocument EncodedSave()
    {
        //Root element
        string xml = "<Game>";
        XmlDocument document = new XmlDocument();
        for(int x=0; x<gameBoardSize; x++)
        {
            for(int y=0; y<gameBoardSize; y++)
            {
                //<CellName>Position<CellName>
                if(gameBoard[x, y] != null)
                    xml += $"<{gameBoard[x, y].id}>{x},{y}</{gameBoard[x, y].id}>";
            }
        }
        xml += "</Game>";
        Debug.Log(xml);
        //Load into doc
        document.LoadXml(xml);
        return document;
    }

    //Update the board with new tiles.
    private void UpdateBoard()
    {
        cellCount = 0;
    //Move through the entire board
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y =0; y < gameBoardSize; y++)
            {
                //As both of these are numbers, we can add them together.
                //This saves time setting values in queue, by only setting relevant values.
                gameBoard[x, y] = queue[x, y];
               
                //Get the cell
                Cell item = gameBoard[x, y];

                //Paint the map with the new cells
                if (item != null)
                {
                    map.SetTile(new Vector3Int(x,y,0),item.GetTile(0));
                    cellCount++;
                }
                else
                {
                    map.SetTile(new Vector3Int(x, y, 0), empty);
                }
                
            }
        }
        //Reset the queue
        queue = new Cell[gameBoardSize, gameBoardSize];
    }
}
