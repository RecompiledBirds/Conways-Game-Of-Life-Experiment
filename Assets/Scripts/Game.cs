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
    //Create and empty board and queue
        gameBoard = new int[gameBoardSize,gameBoardSize];
        queue = new int[gameBoardSize,gameBoardSize];
        //Paint the tilemap
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
            Debug.Log(speed);
            speed += 0.5f;
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            Debug.Log(speed);
            speed -= 0.5f;
        }
        //Clamp the speed so it never stops, and never goes too fast.
        speed = Mathf.Clamp(speed, 1, 100);

        //reset board if needed
        if (Input.GetKey(KeyCode.R))
        {
            queue = new int[gameBoardSize, gameBoardSize];
            gameBoard = new int[gameBoardSize, gameBoardSize];
        }
        
        //"Paint" cells with left mouse button
        if (Input.GetKey(KeyCode.Mouse1))
        {
            //Get the position of the mouse cursor
            Vector3 pos= Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //Convert it to a Vector3Int for use with our tilemap and 2D arrays
            Vector3Int castedPost = new Vector3Int((int)pos.x, (int)pos.y, 0);
            if (castedPost.x > 0 && castedPost.y > 0 && castedPost.x < gameBoardSize && castedPost.y < gameBoardSize)
            {
                //Set needed values
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
        //Move in a 3x3 space around the center to find all possible neighbors
        for(int xPos = x - 1; xPos <= x+1; xPos++)
        {
            for(int yPos = y-1; yPos<=y+1; yPos++)
            {
               //Make sure it is not out of bounds
                if (xPos >= 0 && yPos >= 0 && xPos < gameBoardSize && yPos < gameBoardSize)
                {
                //Make sure it is not the center cell
                    if (!(yPos == y && xPos == x))
                    {
                        amount += gameBoard[xPos, yPos];
                    }
                    
                }
            }
        }

        return amount;
    }


    //Update the board with new tiles.
    public void UpdateBoard()
    {
    //Move through the entire board
        for(int x = 0; x < gameBoardSize; x++)
        {
            for(int y =0; y < gameBoardSize; y++)
            {
                //As both of these are numbers, we can add them together.
                //This saves time setting values in queue, by only setting relevant values.
                gameBoard[x, y] += queue[x, y];
                
                if (gameBoard[x, y] > 1)
                    gameBoard[x, y] = 1;
                    
                //Get the cell
                int item = gameBoard[x, y];
                
                //Paint the map with the new cells
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
        //Reset the queue
        queue = new int[gameBoardSize, gameBoardSize];
    }
}
