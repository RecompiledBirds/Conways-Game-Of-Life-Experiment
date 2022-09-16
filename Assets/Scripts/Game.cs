using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour
{
    public Tile cell;
    public Tile empty;
    public Tilemap map;
    public int gameBoardSize;

    public int[,] gameBoard;
    public int[,] queue;
    // Start is called before the first frame update
    void Start()
    {
        gameBoard = new int[gameBoardSize,gameBoardSize];
        queue = new int[gameBoardSize,gameBoardSize];
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y = 0; y < gameBoardSize; y++)
            {
                gameBoard[x,y] = 0;
                map.SetTile(new Vector3Int(x, y), empty);
                map.RefreshTile(new Vector3Int(x, y));       
            }
        }
    }

    bool playing = false;
    float speed=100;
    float tick = 0;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            playing = !playing;
        if (playing)
        {
            if (tick >= 100)
            {
                tick = 0;
                PrepareQueue();
                UpdateBoard();
            }
            tick += speed;
        }

        //adjust simulation speed
        if (Input.GetKey(KeyCode.Equals))
        {
            Debug.Log(speed);
            speed += 0.5f;
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            Debug.Log(speed);
            speed -= 0.5f;
        }
        speed = Mathf.Clamp(speed, 1, 100);

        //reset board if needed
        if (Input.GetKey(KeyCode.R))
        {
            queue = new int[gameBoardSize, gameBoardSize];
            gameBoard = new int[gameBoardSize, gameBoardSize];
        }
        
        if (Input.GetKey(KeyCode.Mouse1))
        {
            Vector3 pos= Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int castedPost = new Vector3Int((int)pos.x, (int)pos.y, 0);
            if (castedPost.x > 0 && castedPost.y > 0 && castedPost.x < gameBoardSize && castedPost.y < gameBoardSize)
            {
                gameBoard[castedPost.x, castedPost.y] = 1;
                map.SetTile(castedPost, cell);
                map.RefreshTile(castedPost);
            }
        }
        
    }
    /// <summary>
    /// Calculate the new tiles.
    /// </summary>
    public void PrepareQueue()
    {
        
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y = 0; y < gameBoardSize; y++)
            {
                //Get neighbors
                int neighborCount = GetNeighbors(x, y);
                //Rules
                switch (gameBoard[x, y])
                {
                    //Death
                    case (1):
                        //underpopulated
                        if (neighborCount > 2)
                            queue[x, y] = -1;
                        //overpopulated
                        if (neighborCount > 3)
                        {
                            queue[x, y] = -1;
                        }
                        break;
                    //Reproduction
                    case 0:
                        if (neighborCount == 3)
                            queue[x, y] = 1;
                        break;


                }
            }
        }
    }

    /// <summary>
    /// Find all neighbors of a tile.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetNeighbors(int x, int y)
    {
        int amount=0;
        //Move along the board and get the amount of neighbors;
        //Debug.Log($"{x},{y}");
        for(int xPos = x - 1; xPos <= x+1; xPos++)
        {
            for(int yPos = y-1; yPos<=y+1; yPos++)
            {
               
                if (xPos >= 0 && yPos >= 0 && xPos < gameBoardSize && yPos < gameBoardSize)
                {
                    if (!(yPos == y && xPos == x))
                    {
                  //      Debug.Log($"Checking {xPos},{yPos}..");
                        amount += gameBoard[xPos, yPos];
                  //      Debug.Log($"Found: {gameBoard[xPos, yPos]}");
                    }
                    
                }
            }
        }

        return amount;
    }

    public List<Vector2Int> FindEmptyNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        for (int xPos = x - 1; xPos <= x + 1; xPos++)
        {
            for (int yPos = y - 1; yPos <= y + 1; yPos++)
            {

                if (xPos >= 0 && yPos >= 0 && xPos < gameBoardSize && yPos < gameBoardSize)
                {
                    if (!(yPos == y && xPos == x))
                    {
                        neighbors.Add(new Vector2Int(xPos, yPos));
                    }

                }
            }
        }
        return neighbors.ToList();
    }

    //Update the board with new tiles.
    public void UpdateBoard()
    {
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y =0; y < gameBoardSize; y++)
            {
                gameBoard[x, y] += queue[x, y];
                if (gameBoard[x, y] > 1)
                    gameBoard[x, y] = 1;
       //         Debug.Log($"Placing {queue[x, y]} at {x},{y}");
      //          Debug.Log($"Board at {x},{y}: {gameBoard[x, y]}");
                int item = gameBoard[x, y];

                switch (item)
                {
                    case 0:
                        map.SetTile(new Vector3Int(x, y), empty);
                        break;
                    case 1:
                        map.SetTile(new Vector3Int(x, y), cell);
                        break;
                }
                
            }
        }
        queue = new int[gameBoardSize, gameBoardSize];
    }
}
